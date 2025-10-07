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

        public static void SetStartup(bool enabled)
        {
            try
            {
                string exePath = GetExecutablePath();

                using (RegistryKey runKey = Registry.CurrentUser.CreateSubKey(RUN_REGISTRY_PATH, true))
                using (RegistryKey approvedKey = Registry.CurrentUser.CreateSubKey(STARTUP_APPROVED_PATH, true))
                {
                    if (enabled)
                    {
                        // Write/refresh the Run key value
                        runKey?.SetValue(APP_NAME, $"\"{exePath}\"", RegistryValueKind.String);

                        // Clear any disabled marker in StartupApproved so Windows will launch it
                        try
                        {
                            if (approvedKey?.GetValue(APP_NAME) != null)
                            {
                                approvedKey.DeleteValue(APP_NAME, false);
                            }
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

                        // Mark as disabled in StartupApproved so Windows reflects the state in Startup Apps UI
                        try
                        {
                            // 0x03 indicates disabled; remaining bytes are timestamp metadata (zeros are acceptable)
                            byte[] disabled = new byte[12];
                            disabled[0] = 0x03;
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

                return hasRunEntry && !isDisabledByApproval;
            }
            catch
            {
                return false;
            }
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
