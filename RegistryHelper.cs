using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace BearsAdaClock
{
    public static class RegistryHelper
    {
        private const string RUN_REGISTRY_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string STARTUP_APPROVED_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
        private const string STARTUP_APPROVED_STARTUPFOLDER_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder";
        private const string APP_NAME = "BearsAdaClock"; // Keep stable value name for Run and StartupApproved
        private const string APP_SHORTCUT_NAME = "BearsAdaClock.lnk";

        public static void SetStartup(bool enabled)
        {
            try
            {
                string exePath = GetExecutablePath();

                using (RegistryKey runKey = Registry.CurrentUser.CreateSubKey(RUN_REGISTRY_PATH, true))
                using (RegistryKey approvedRunKey = Registry.CurrentUser.CreateSubKey(STARTUP_APPROVED_PATH, true))
                using (RegistryKey approvedStartupFolderKey = Registry.CurrentUser.CreateSubKey(STARTUP_APPROVED_STARTUPFOLDER_PATH, true))
                {
                    if (enabled)
                    {
                        // Write/refresh the Run key value
                        runKey?.SetValue(APP_NAME, $"\"{exePath}\"", RegistryValueKind.String);

                        // Create/refresh Startup folder shortcut
                        try { CreateStartupShortcut(exePath); } catch { }

                        // Also create/update a per-user Scheduled Task as a reliable fallback with a short delay
                        try { TryCreateScheduledTask(exePath, 10); } catch { }

                        // Mark as enabled in StartupApproved (Run)
                        try
                        {
                            byte[] enabledValue = new byte[12];
                            enabledValue[0] = 0x02; // enabled
                            approvedRunKey?.SetValue(APP_NAME, enabledValue, RegistryValueKind.Binary);
                        }
                        catch { }

                        // Mark as enabled in StartupApproved (StartupFolder)
                        try
                        {
                            byte[] enabledValue = new byte[12];
                            enabledValue[0] = 0x02; // enabled
                            approvedStartupFolderKey?.SetValue(APP_SHORTCUT_NAME, enabledValue, RegistryValueKind.Binary);
                        }
                        catch { }
                    }
                    else
                    {
                        // Remove the Run key value
                        try
                        {
                            if (runKey?.GetValue(APP_NAME) != null)
                            {
                                runKey.DeleteValue(APP_NAME, false);
                            }
                        }
                        catch { }

                        // Delete Startup folder shortcut
                        try { DeleteStartupShortcut(); } catch { }

                        // Delete per-user scheduled task if present
                        try { TryDeleteScheduledTask(); } catch { }

                        // Mark as disabled in StartupApproved (Run)
                        try
                        {
                            byte[] disabled = new byte[12];
                            disabled[0] = 0x03; // disabled
                            approvedRunKey?.SetValue(APP_NAME, disabled, RegistryValueKind.Binary);
                        }
                        catch { }

                        // Mark as disabled in StartupApproved (StartupFolder)
                        try
                        {
                            byte[] disabled = new byte[12];
                            disabled[0] = 0x03; // disabled
                            approvedStartupFolderKey?.SetValue(APP_SHORTCUT_NAME, disabled, RegistryValueKind.Binary);
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to {(enabled ? "enable" : "disable")} startup: {ex.Message}");
            }
        }

        public static bool IsStartupEnabled()
        {
            try
            {
                bool hasRunEntry = false;
                bool runDisabledByApproval = false;
                bool hasStartupShortcut = false;
                bool folderDisabledByApproval = false;

                using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(RUN_REGISTRY_PATH, false))
                {
                    hasRunEntry = runKey?.GetValue(APP_NAME) != null;
                }

                using (RegistryKey approvedRun = Registry.CurrentUser.OpenSubKey(STARTUP_APPROVED_PATH, false))
                {
                    var val = approvedRun?.GetValue(APP_NAME) as byte[];
                    if (val != null && val.Length > 0)
                    {
                        // First byte: 0x02 = enabled, 0x03 = disabled
                        runDisabledByApproval = val[0] == 0x03;
                    }
                }

                // Startup folder
                string shortcut = GetStartupShortcutPath();
                hasStartupShortcut = !string.IsNullOrEmpty(shortcut) && File.Exists(shortcut);
                using (RegistryKey approvedFolder = Registry.CurrentUser.OpenSubKey(STARTUP_APPROVED_STARTUPFOLDER_PATH, false))
                {
                    var val = approvedFolder?.GetValue(APP_SHORTCUT_NAME) as byte[];
                    if (val != null && val.Length > 0)
                    {
                        folderDisabledByApproval = val[0] == 0x03;
                    }
                }

                // Check scheduled task presence as an additional reliable mechanism
                bool hasSchedTask = false;
                try { hasSchedTask = IsScheduledTaskPresentAndEnabled(); } catch { }

                return (hasRunEntry && !runDisabledByApproval)
                       || (hasStartupShortcut && !folderDisabledByApproval)
                       || hasSchedTask;
            }
            catch
            {
                return false;
            }
        }

        private static string GetStartupShortcutPath()
        {
            try
            {
                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                if (string.IsNullOrEmpty(startupFolder)) return null;
                return Path.Combine(startupFolder, APP_SHORTCUT_NAME);
            }
            catch { return null; }
        }

        private static void CreateStartupShortcut(string exePath)
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath)) return;
            string shortcutPath = GetStartupShortcutPath();
            if (string.IsNullOrEmpty(shortcutPath)) return;

            try
            {
                var shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = exePath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
                shortcut.IconLocation = exePath;
                shortcut.Description = "Bears ADA Clock";
                shortcut.Save();
            }
            catch { }
        }

        private static void DeleteStartupShortcut()
        {
            try
            {
                string shortcutPath = GetStartupShortcutPath();
                if (!string.IsNullOrEmpty(shortcutPath) && File.Exists(shortcutPath))
                {
                    File.Delete(shortcutPath);
                }
            }
            catch { }
        }

        private static string GetExecutablePath()
        {
            try
            {
                // Prefer the actual running process executable
                string processPath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(processPath) && File.Exists(processPath) && processPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    return processPath;

                // Try common locations relative to base directory
                string baseDirectory = AppContext.BaseDirectory;
                if (!string.IsNullOrEmpty(baseDirectory))
                {
                    // Try BearsAdaClock.exe by convention
                    string exePath = Path.Combine(baseDirectory, "BearsAdaClock.exe");
                    if (File.Exists(exePath))
                        return exePath;

                    // Try using entry assembly name
                    var entry = Assembly.GetEntryAssembly();
                    if (entry != null)
                    {
                        string name = entry.GetName().Name + ".exe";
                        string candidate = Path.Combine(baseDirectory, name);
                        if (File.Exists(candidate)) return candidate;
                    }
                }


                // As last resort, return the base directory (unlikely to work) only if processPath is null
                return !string.IsNullOrEmpty(processPath) ? processPath : baseDirectory;
            }
            catch
            {
                // Final fallback to process path if available
                try
                {
                    string p = Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrEmpty(p) && File.Exists(p)) return p;
                }
                catch { }
                return AppContext.BaseDirectory;
            }
        }
    }
}


        private static void TryCreateScheduledTask(string exePath, int delaySeconds)
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath)) return;
            try
            {
                // schtasks per-user creation without password; delay helps avoid logon race conditions
                string taskName = APP_NAME; // keep consistent branding
                int d = Math.Max(0, Math.Min(delaySeconds, 300));
                string delay = $"{(d/60):D2}:{(d%60):D2}"; // mm:ss

                // If task exists, delete first to ensure it updates cleanly
                TryDeleteScheduledTask();

                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/Create /TN \"{taskName}\" /SC ONLOGON /RL LIMITED /DELAY 0000:{delay} /TR \"\"{exePath}\"\" /F",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                using (var p = Process.Start(psi))
                {
                    p.WaitForExit(5000);
                }
            }
            catch { }
        }

        private static void TryDeleteScheduledTask()
        {
            try
            {
                string taskName = APP_NAME;
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/Delete /TN \"{taskName}\" /F",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                using (var p = Process.Start(psi))
                {
                    p.WaitForExit(4000);
                }
            }
            catch { }
        }

        private static bool IsScheduledTaskPresentAndEnabled()
        {
            try
            {
                string taskName = APP_NAME;
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/Query /TN \"{taskName}\" /FO LIST /V",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                using (var p = Process.Start(psi))
                {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(4000);
                    if (p.ExitCode != 0) return false;
                    if (string.IsNullOrEmpty(output)) return true; // exists but no detail; assume present
                    // Look for Enabled: Yes or State: Ready/Queued
                    bool enabled = output.IndexOf("Enabled:", StringComparison.OrdinalIgnoreCase) < 0 ||
                                   output.IndexOf("Enabled:             Yes", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   output.IndexOf("Enabled: Yes", StringComparison.OrdinalIgnoreCase) >= 0;
                    bool notDisabledState = output.IndexOf("State:", StringComparison.OrdinalIgnoreCase) < 0 ||
                                            output.IndexOf("State:             Ready", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            output.IndexOf("State: Ready", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            output.IndexOf("State: Running", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            output.IndexOf("State: Queued", StringComparison.OrdinalIgnoreCase) >= 0;
                    return enabled && notDisabledState;
                }
            }
            catch { return false; }
        }
