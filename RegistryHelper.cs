using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace BearsAdaClock
{
    public static class RegistryHelper
    {
        private const string RUN_REGISTRY_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string STARTUP_APPROVED_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
        private const string APP_NAME = "BearsAdaClock";

        // Prefer Startup folder shortcut like other working projects; keep registry fallback for compatibility
        public static void SetStartup(bool enabled)
        {
            try
            {
                string exePath = GetExecutablePath();
                string shortcutPath = GetStartupShortcutPath();

                Log($"SetStartup(enabled={enabled}) exePath='{exePath}' shortcutPath='{shortcutPath}'");

                using (RegistryKey runKey = Registry.CurrentUser.CreateSubKey(RUN_REGISTRY_PATH, true))
                using (RegistryKey approvedKey = Registry.CurrentUser.CreateSubKey(STARTUP_APPROVED_PATH, true))
                {
                    if (enabled)
                    {
                        // Try to create the Startup shortcut first
                        bool shortcutCreated = TryCreateStartupShortcut(shortcutPath, exePath);
                        Log($"TryCreateStartupShortcut => {shortcutCreated} existsAfter={File.Exists(shortcutPath)}");

                        if (shortcutCreated)
                        {
                            // Shortcut created successfully: clean any stale Run/StartupApproved entries
                            try { if (runKey?.GetValue(APP_NAME) != null) { runKey.DeleteValue(APP_NAME, false); Log("Deleted stale Run entry"); } } catch (Exception ex) { Log("Delete Run entry failed: " + ex.Message); }
                            try { if (approvedKey?.GetValue(APP_NAME) != null) { approvedKey.DeleteValue(APP_NAME, false); Log("Deleted stale StartupApproved entry"); } } catch (Exception ex) { Log("Delete StartupApproved failed: " + ex.Message); }
                        }
                        else
                        {
                            // Shortcut creation failed: ensure Run key is set as a reliable fallback
                            try
                            {
                                runKey?.SetValue(APP_NAME, $"\"{exePath}\"", RegistryValueKind.String);
                                Log("Set HKCU Run fallback to '" + exePath + "'");
                            }
                            catch (Exception ex)
                            {
                                Log("Failed to set Run fallback: " + ex.Message);
                            }

                            // Clear any disabled marker so Windows will launch it
                            try
                            {
                                if (approvedKey?.GetValue(APP_NAME) != null) { approvedKey.DeleteValue(APP_NAME, false); Log("Cleared StartupApproved disabled marker"); }
                            }
                            catch (Exception ex)
                            {
                                Log("Failed to clear StartupApproved: " + ex.Message);
                            }
                        }
                    }
                    else
                    {
                        // Disable autostart: remove shortcut and Run entry, and mark disabled for UI
                        DeleteStartupShortcut(shortcutPath);
                        Log("Deleted shortcut if existed");

                        try { if (runKey?.GetValue(APP_NAME) != null) { runKey.DeleteValue(APP_NAME, false); Log("Deleted Run entry"); } } catch (Exception ex) { Log("Delete Run failed: " + ex.Message); }

                        // Mark as disabled in StartupApproved so Windows reflects state in Startup Apps UI
                        try
                        {
                            byte[] disabled = new byte[12];
                            disabled[0] = 0x03; // 0x03 indicates disabled
                            approvedKey?.SetValue(APP_NAME, disabled, RegistryValueKind.Binary);
                            Log("Wrote StartupApproved disabled marker");
                        }
                        catch (Exception ex)
                        {
                            Log("Failed to write StartupApproved disabled marker: " + ex.Message);
                        }
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

                using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(RUN_REGISTRY_PATH, false))
                {
                    object? val = runKey?.GetValue(APP_NAME);
                    runRaw = val as string;
                    runExe = ExtractExeFromRunValue(runRaw);
                    if (!string.IsNullOrWhiteSpace(runExe))
                    {
                        hasValidRunEntry = File.Exists(runExe);
                    }
                }

                using (RegistryKey approvedKey = Registry.CurrentUser.OpenSubKey(STARTUP_APPROVED_PATH, false))
                {
                    var val = approvedKey?.GetValue(APP_NAME) as byte[];
                    if (val != null && val.Length > 0)
                    {
                        // First byte: 0x02 = enabled, 0x03 = disabled
                        isDisabledByApproval = val[0] == 0x03;
                    }
                }

                bool enabled = hasShortcut || (hasValidRunEntry && !isDisabledByApproval);
                Log($"IsStartupEnabled => {enabled} hasShortcut={hasShortcut} hasValidRunEntry={hasValidRunEntry} runRaw='{runRaw}' runExe='{runExe}' disabledByApproval={isDisabledByApproval}");
                return enabled;
            }
            catch (Exception ex)
            {
                Log("IsStartupEnabled error: " + ex.Message);
                return false;
            }
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
            return Path.Combine(startupFolder, APP_NAME + ".lnk");
        }

        private static bool TryCreateStartupShortcut(string shortcutPath, string exePath)
        {
            try { Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)!); } catch (Exception ex) { Log("CreateDirectory failed: " + ex.Message); }

            // Try robust ShellLink COM first (works even if Windows Script Host is disabled)
            try
            {
                var link = (IShellLinkW)new CShellLink();
                link.SetPath(exePath);
                link.SetWorkingDirectory(Path.GetDirectoryName(exePath));
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
                // First try to get the main module file name (works for both single-file and regular deployments)
                string processPath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(processPath) && File.Exists(processPath) && processPath.EndsWith(".exe"))
                    return processPath;
                
                // For single-file apps, use AppContext.BaseDirectory
                string baseDirectory = AppContext.BaseDirectory;
                string exePath = Path.Combine(baseDirectory, "BearsAdaClock.exe");
                if (File.Exists(exePath))
                    return exePath;
                
                // Fallback to assembly location (for non-single-file deployments)
                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(assemblyPath))
                {
                    if (assemblyPath.EndsWith(".dll"))
                    {
                        string dllExePath = Path.ChangeExtension(assemblyPath, ".exe");
                        if (File.Exists(dllExePath)) return dllExePath;
                        string directory = Path.GetDirectoryName(assemblyPath);
                        string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
                        string potentialExePath = Path.Combine(directory, assemblyName + ".exe");
                        if (File.Exists(potentialExePath)) return potentialExePath;
                        string clockExePath = Path.Combine(directory, "BearsAdaClock.exe");
                        if (File.Exists(clockExePath)) return clockExePath;
                    }
                    return assemblyPath;
                }
                
                // Final fallback - use the process path or base directory
                return processPath ?? baseDirectory;
            }
            catch (Exception ex)
            { 
                Log("GetExecutablePath error: " + ex.Message);
                return AppContext.BaseDirectory; 
            }
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
