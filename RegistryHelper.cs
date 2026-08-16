using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

#nullable enable
namespace BearsAdaClock
{
    public static class RegistryHelper
    {
        private const string RUN_REGISTRY_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string STARTUP_APPROVED_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
        private const string STARTUP_APPROVED_STARTUP_FOLDER_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder";
        private const string APP_NAME = "BearsAdaClock";
        private const string AUTOSTART_ARG = "--autostart";
        private const string STARTUP_SHORTCUT_FILE = APP_NAME + ".lnk";
        private const string LAUNCHER_FILE = "BearsAdaClockLauncher.cmd";

        // The primary startup mechanism is HKCU\Run. To be reliable on all machines, the
        // Run entry points at a small launcher .cmd that uses `start` to launch the app.
        // (Launching the single-file exe directly from the Run key has been observed to
        // fail silently at logon on some systems, while a `start`-based launcher works.)
        // A Startup-folder shortcut is only used as a fallback if the registry write
        // cannot be verified.
        public static void SetStartup(bool enabled)
        {
            try
            {
                string exePath = GetExecutablePath();
                string shortcutPath = GetStartupShortcutPath();

                Log($"SetStartup(enabled={enabled}) exePath='{exePath}'");

                using (RegistryKey runKey = Registry.CurrentUser.CreateSubKey(RUN_REGISTRY_PATH, true))
                using (RegistryKey runApprovedKey = Registry.CurrentUser.CreateSubKey(STARTUP_APPROVED_PATH, true))
                using (RegistryKey folderApprovedKey = Registry.CurrentUser.CreateSubKey(STARTUP_APPROVED_STARTUP_FOLDER_PATH, true))
                {
                    if (enabled)
                    {
                        // ---- Primary: HKCU\Run via launcher .cmd ----
                        bool runEntryWritten = false;
                        string launcherPath = GetLauncherPath();
                        if (TryWriteLauncher(exePath, launcherPath))
                        {
                            Log("Launcher written to '" + launcherPath + "'");
                            string runValue = $"\"{launcherPath}\"";
                            try
                            {
                                runKey.SetValue(APP_NAME, runValue, RegistryValueKind.String);
                                runEntryWritten = VerifyRunEntry(exePath);
                                if (runEntryWritten)
                                    Log("Set HKCU Run entry to '" + runValue + "'");
                                else
                                    Log("HKCU Run entry was written but verification failed");
                            }
                            catch (Exception ex)
                            {
                                Log("Failed to set HKCU Run entry: " + ex.Message);
                            }
                        }
                        else
                        {
                            Log("Launcher write failed");
                        }

                        if (!runEntryWritten)
                        {
                            // ---- Fallback: direct exe entry ----
                            string runValue = $"\"{exePath}\" {AUTOSTART_ARG}";
                            try
                            {
                                runKey.SetValue(APP_NAME, runValue, RegistryValueKind.String);
                                runEntryWritten = VerifyRunEntry(exePath);
                                if (runEntryWritten)
                                    Log("Set HKCU Run entry (direct) to '" + runValue + "'");
                                else
                                    Log("HKCU Run entry (direct) was written but verification failed");
                            }
                            catch (Exception ex)
                            {
                                Log("Failed to set HKCU Run entry (direct): " + ex.Message);
                            }
                        }

                        if (runEntryWritten)
                        {
                            // Mark enabled in Startup Apps UI (0x02) so Task Manager shows it as enabled.
                            WriteApprovedMarker(runApprovedKey, APP_NAME, 0x02);
                            Log("Set StartupApproved\\Run enabled marker");

                            // Remove the legacy Startup-folder shortcut and its approval entry so the
                            // single reliable mechanism (HKCU\Run) is used and there is no double launch.
                            DeleteStartupShortcut(shortcutPath);
                            DeleteApprovedMarker(folderApprovedKey, STARTUP_SHORTCUT_FILE);
                            Log("Cleaned up legacy Startup folder shortcut");
                        }
                        else
                        {
                            // ---- Fallback: Startup-folder shortcut (older mechanism) ----
                            Log("HKCU Run approach failed; falling back to Startup folder shortcut");
                            bool shortcutCreated = TryCreateStartupShortcut(shortcutPath, exePath);
                            if (shortcutCreated)
                            {
                                WriteApprovedMarker(folderApprovedKey, STARTUP_SHORTCUT_FILE, 0x02);
                                Log("Created Startup folder shortcut fallback and marked it enabled");
                            }
                            else
                            {
                                Log("Both HKCU Run and Startup folder shortcut failed");
                            }
                        }
                    }
                    else
                    {
                        // Disable autostart: remove Run entry and shortcut, mark disabled for UI.
                        try
                        {
                            if (runKey.GetValue(APP_NAME) != null)
                            {
                                runKey.DeleteValue(APP_NAME, false);
                                Log("Deleted Run entry");
                            }
                        }
                        catch (Exception ex) { Log("Delete Run failed: " + ex.Message); }

                        DeleteStartupShortcut(shortcutPath);

                        WriteApprovedMarker(runApprovedKey, APP_NAME, 0x03);
                        WriteApprovedMarker(folderApprovedKey, STARTUP_SHORTCUT_FILE, 0x03);
                        Log("Wrote StartupApproved disabled markers");
                    }
                }
            }
            catch (Exception ex)
            {
                Log("SetStartup fatal error: " + ex);
                throw new Exception($"Failed to {(enabled ? "enable" : "disable")} startup: {ex.Message}");
            }
        }

        public static bool IsStartupEnabled()
        {
            try
            {
                bool hasShortcut = File.Exists(GetStartupShortcutPath());

                // Backward compatibility with existing Run key entries
                bool hasValidRunEntry = false;
                bool isDisabledByApproval = false;
                string? runRaw = null;
                string? runExe = null;

                using (RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(RUN_REGISTRY_PATH, false))
                {
                    object? val = runKey?.GetValue(APP_NAME);
                    runRaw = val as string;
                    runExe = ExtractExeFromRunValue(runRaw);
                    if (!string.IsNullOrWhiteSpace(runExe))
                    {
                        hasValidRunEntry = File.Exists(runExe);
                    }
                }

                using (RegistryKey? runApprovedKey = Registry.CurrentUser.OpenSubKey(STARTUP_APPROVED_PATH, false))
                {
                    var val = runApprovedKey?.GetValue(APP_NAME) as byte[];
                    if (val != null && val.Length > 0)
                    {
                        // First byte: 0x02 = enabled, 0x03 = disabled
                        isDisabledByApproval = val[0] == 0x03;
                    }
                }

                bool shortcutDisabledByApproval = false;
                using (RegistryKey? folderApprovedKey = Registry.CurrentUser.OpenSubKey(STARTUP_APPROVED_STARTUP_FOLDER_PATH, false))
                {
                    var val = folderApprovedKey?.GetValue(STARTUP_SHORTCUT_FILE) as byte[];
                    if (val != null && val.Length > 0)
                    {
                        shortcutDisabledByApproval = val[0] == 0x03;
                    }
                }

                bool enabled = (hasValidRunEntry && !isDisabledByApproval) || (hasShortcut && !shortcutDisabledByApproval);
                Log($"IsStartupEnabled => {enabled} hasShortcut={hasShortcut} hasValidRunEntry={hasValidRunEntry} runRaw='{runRaw}' runExe='{runExe}' disabledByApproval={isDisabledByApproval} shortcutDisabled={shortcutDisabledByApproval}");
                return enabled;
            }
            catch (Exception ex)
            {
                Log("IsStartupEnabled error: " + ex.Message);
                return false;
            }
        }

        private static bool VerifyRunEntry(string exePath)
        {
            try
            {
                using (RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(RUN_REGISTRY_PATH, false))
                {
                    string? raw = runKey?.GetValue(APP_NAME) as string;
                    if (string.IsNullOrWhiteSpace(raw)) return false;

                    string token = raw.Trim().Trim('"');

                    // Case 1: points directly at the app exe (legacy form).
                    if (string.Equals(token, exePath.Trim().Trim('"'), StringComparison.OrdinalIgnoreCase))
                        return File.Exists(exePath);

                    // Case 2: points at the launcher .cmd (current form).
                    if (string.Equals(token, GetLauncherPath(), StringComparison.OrdinalIgnoreCase))
                    {
                        string launcherPath = GetLauncherPath();
                        if (!File.Exists(launcherPath)) return false;
                        try
                        {
                            // The launcher references the app exe; a stale reference means
                            // the entry must be rewritten, so verify the path inside it.
                            string content = File.ReadAllText(launcherPath);
                            return content.Contains(exePath, StringComparison.OrdinalIgnoreCase);
                        }
                        catch { return false; }
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                Log("VerifyRunEntry error: " + ex.Message);
                return false;
            }
        }

        private static string GetLauncherPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(appData, "N6REJ", "BearsAdaClock", LAUNCHER_FILE);
        }

        private static bool TryWriteLauncher(string exePath, string launcherPath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(launcherPath)!);
                string content = "@echo off\r\nstart \"\" \"" + exePath + "\" " + AUTOSTART_ARG + "\r\n";
                File.WriteAllText(launcherPath, content);
                return File.Exists(launcherPath);
            }
            catch (Exception ex)
            {
                Log("Write launcher failed: " + ex.Message);
                return false;
            }
        }

        private static void WriteApprovedMarker(RegistryKey? key, string name, byte state)
        {
            try
            {
                byte[] marker = new byte[12];
                marker[0] = state;
                key?.SetValue(name, marker, RegistryValueKind.Binary);
            }
            catch (Exception ex)
            {
                Log($"WriteApprovedMarker({name}, {state}) failed: " + ex.Message);
            }
        }

        private static void DeleteApprovedMarker(RegistryKey? key, string name)
        {
            try
            {
                if (key?.GetValue(name) != null)
                {
                    key.DeleteValue(name, false);
                }
            }
            catch (Exception ex) { Log("DeleteApprovedMarker failed: " + ex.Message); }
        }

        private static string? ExtractExeFromRunValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string s = value.Trim();
            try
            {
                if (s.StartsWith("\""))
                {
                    int end = s.IndexOf('"', 1);
                    if (end > 1)
                        return s.Substring(1, end - 1);
                }
                int space = s.IndexOf(' ');
                return space > 0 ? s.Substring(0, space) : s;
            }
            catch { return null; }
        }

        private static string GetStartupShortcutPath()
        {
            string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            return Path.Combine(startupFolder, STARTUP_SHORTCUT_FILE);
        }

        private static bool TryCreateStartupShortcut(string shortcutPath, string exePath)
        {
            // Legacy fallback mechanism (used only if the HKCU\Run entry cannot be verified).
            try { Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)!); } catch (Exception ex) { Log("CreateDirectory failed: " + ex.Message); }

            // Try robust ShellLink COM first (works even if Windows Script Host is disabled)
            try
            {
                var link = (IShellLinkW)new CShellLink();
                link.SetPath(exePath);
                link.SetWorkingDirectory(Path.GetDirectoryName(exePath) ?? string.Empty);
                link.SetShowCmd(1); // SW_SHOWNORMAL
                link.SetDescription("Bears ADA Clock");
                link.SetIconLocation(exePath, 0);

                var pf = (IPersistFile)link;
                pf.Save(shortcutPath, true);

                bool exists = File.Exists(shortcutPath);
                Log("ShellLink save => exists=" + exists);
                return exists;
            }
            catch (Exception ex)
            {
                Log("ShellLink creation failed: " + ex.Message);
            }

            // Fallback to WScript.Shell if ShellLink failed
            try
            {
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                {
                    Log("WScript.Shell not available");
                }
                else
                {
                    dynamic shell = Activator.CreateInstance(shellType)!;
                    dynamic lnk = shell.CreateShortcut(shortcutPath);
                    lnk.TargetPath = exePath;
                    lnk.WorkingDirectory = Path.GetDirectoryName(exePath);
                    lnk.WindowStyle = 1;
                    lnk.Description = "Bears ADA Clock";
                    lnk.IconLocation = exePath + ",0";
                    lnk.Save();
                    bool exists = File.Exists(shortcutPath);
                    Log("WScript.Shell save => exists=" + exists);
                    return exists;
                }
            }
            catch (Exception ex)
            {
                Log("WScript.Shell creation failed: " + ex.Message);
            }

            return false;
        }

        private static void DeleteStartupShortcut(string shortcutPath)
        {
            try
            {
                if (File.Exists(shortcutPath))
                {
                    File.Delete(shortcutPath);
                }
            }
            catch (Exception ex) { Log("Delete shortcut failed: " + ex.Message); }
        }

        private static string GetExecutablePath()
        {
            try
            {
                // Prefer Environment.ProcessPath - it returns the real apphost/bundle
                // executable path even for single-file (self-contained single exe) builds.
                string? processPath = Environment.ProcessPath;
                if (IsValidExe(processPath))
                    return processPath!;

                // Fall back to the main module file name.
                try
                {
                    string? mainModule = Process.GetCurrentProcess().MainModule?.FileName;
                    if (IsValidExe(mainModule))
                        return mainModule!;
                }
                catch { }

                // Last resort: build the path from AppContext.BaseDirectory.
                // NOTE: for single-file apps this is the temp extraction dir which is
                // deleted on reboot - reject candidates that live under the temp path.
                string baseDirectory = AppContext.BaseDirectory;
                string tempPath = Path.GetTempPath();
                if (!string.IsNullOrEmpty(baseDirectory) &&
                    !baseDirectory.StartsWith(tempPath, StringComparison.OrdinalIgnoreCase))
                {
                    string candidate = Path.Combine(baseDirectory, "BearsAdaClock.exe");
                    if (File.Exists(candidate))
                        return candidate;
                }

                return processPath ?? baseDirectory;
            }
            catch (Exception ex)
            {
                Log("GetExecutablePath error: " + ex.Message);
                return AppContext.BaseDirectory;
            }
        }

        private static bool IsValidExe(string? path)
        {
            return !string.IsNullOrEmpty(path)
                && path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                && File.Exists(path);
        }

        private static void Log(string message)
        {
            try
            {
                string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "N6REJ", "BearsAdaClock", "logs");
                Directory.CreateDirectory(root);
                string path = Path.Combine(root, "autostart.log");
                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message + Environment.NewLine;
                File.AppendAllText(path, line);
            }
            catch { }
        }

        // COM Interop for Shell Link (legacy Startup-folder shortcut fallback)
        #region COM Interop for Shell Link
        [ComImport]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellLinkW
        {
            int GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszFile, int cchMaxPath, IntPtr pfd, int fFlags);
            int GetIDList(out IntPtr ppidl);
            int SetIDList(IntPtr pidl);
            int GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszName, int cchMaxName);
            int SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            int GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszDir, int cchMaxPath);
            int SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            int GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszArgs, int cchMaxPath);
            int SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            int GetHotkey(out short wHotkey);
            int SetHotkey(short wHotkey);
            int GetShowCmd(out int iShowCmd);
            int SetShowCmd(int iShowCmd);
            int GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszIconPath, int cchIconPath, out int iIcon);
            int SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            int SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
            int Resolve(IntPtr hwnd, int fFlags);
            int SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport]
        [Guid("0000010B-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPersistFile
        {
            int GetClassID(out Guid pClassID);
            int IsDirty();
            int Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
            int Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, bool fRemember);
            int SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
            int GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
        }

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        private class CShellLink { }
        #endregion
    }
}
