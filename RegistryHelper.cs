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
        private const string APP_NAME = "BearsAdaClock";

        // Prefer Startup folder shortcut like other working projects; keep registry cleanup for compatibility
        public static void SetStartup(bool enabled)
        {
            try
            {
                string exePath = GetExecutablePath();
                string shortcutPath = GetStartupShortcutPath();

                using (RegistryKey runKey = Registry.CurrentUser.CreateSubKey(RUN_REGISTRY_PATH, true))
                using (RegistryKey approvedKey = Registry.CurrentUser.CreateSubKey(STARTUP_APPROVED_PATH, true))
                {
                    if (enabled)
                    {
                        // Create Startup shortcut (preferred and reliable at user logon)
                        CreateStartupShortcut(shortcutPath, exePath);

                        // Clean any stale Run/StartupApproved entries to avoid duplicates/conflicts
                        try { if (runKey?.GetValue(APP_NAME) != null) runKey.DeleteValue(APP_NAME, false); } catch { }
                        try { if (approvedKey?.GetValue(APP_NAME) != null) approvedKey.DeleteValue(APP_NAME, false); } catch { }
                    }
                    else
                    {
                        // Remove Startup shortcut
                        DeleteStartupShortcut(shortcutPath);

                        // Remove old Run entry
                        try { if (runKey?.GetValue(APP_NAME) != null) runKey.DeleteValue(APP_NAME, false); } catch { }

                        // Mark as disabled in StartupApproved so Windows reflects state in Startup Apps UI
                        try
                        {
                            byte[] disabled = new byte[12];
                            disabled[0] = 0x03; // 0x03 indicates disabled
                            approvedKey?.SetValue(APP_NAME, disabled, RegistryValueKind.Binary);
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
                bool hasShortcut = File.Exists(GetStartupShortcutPath());

                // Backward compatibility with existing Run key entries
                bool hasRunEntry = false;
                bool isDisabledByApproval = false;

                using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(RUN_REGISTRY_PATH, false))
                {
                    hasRunEntry = runKey?.GetValue(APP_NAME) != null;
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

                return hasShortcut || (hasRunEntry && !isDisabledByApproval);
            }
            catch
            {
                return false;
            }
        }

        private static string GetStartupShortcutPath()
        {
            string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            return Path.Combine(startupFolder, APP_NAME + ".lnk");
        }

        private static void CreateStartupShortcut(string shortcutPath, string exePath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)!);

                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                    throw new InvalidOperationException("WScript.Shell COM not available");

                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic lnk = shell.CreateShortcut(shortcutPath);
                lnk.TargetPath = exePath;
                lnk.WorkingDirectory = Path.GetDirectoryName(exePath);
                lnk.WindowStyle = 1;
                lnk.Description = "Bears ADA Clock";
                lnk.IconLocation = exePath + ",0";
                lnk.Save();
            }
            catch (Exception)
            {
                // As a fallback, if shortcut creation fails for any reason, write/refresh the Run key
                try
                {
                    using (RegistryKey runKey = Registry.CurrentUser.CreateSubKey(RUN_REGISTRY_PATH, true))
                    {
                        runKey?.SetValue(APP_NAME, $"\"{GetExecutablePath()}\"", RegistryValueKind.String);
                    }

                    // Clear any disabled marker so Windows will launch it
                    using (RegistryKey approvedKey = Registry.CurrentUser.CreateSubKey(STARTUP_APPROVED_PATH, true))
                    {
                        try { if (approvedKey?.GetValue(APP_NAME) != null) approvedKey.DeleteValue(APP_NAME, false); } catch { }
                    }
                }
                catch { }
            }
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
            catch { }
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
            catch 
            { 
                return AppContext.BaseDirectory; 
            }
        }
    }
}
