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
                Logger.Info($"RegistryHelper.SetStartup(enabled={enabled}) - exePath='{exePath}'");

                using (RegistryKey runKey = Registry.CurrentUser.CreateSubKey(RUN_REGISTRY_PATH, true))
                using (RegistryKey approvedKey = Registry.CurrentUser.CreateSubKey(STARTUP_APPROVED_PATH, true))
                {
                    if (enabled)
                    {
                        // Write/refresh the Run key value
                        runKey?.SetValue(APP_NAME, $"\"{exePath}\"", RegistryValueKind.String);
                        Logger.Info("Run key set for autostart.");

                        // Clear any disabled marker in StartupApproved so Windows will launch it
                        try
                        {
                            if (approvedKey?.GetValue(APP_NAME) != null)
                            {
                                approvedKey.DeleteValue(APP_NAME, false);
                                Logger.Info("Cleared StartupApproved disabled marker.");
                            }
                        }
                        catch (Exception exDel)
                        {
                            Logger.Warn($"Unable to clear StartupApproved value: {exDel.Message}");
                        }
                    }
                    else
                    {
                        // Remove the Run key value
                        try
                        {
                            if (runKey?.GetValue(APP_NAME) != null)
                            {
                                runKey.DeleteValue(APP_NAME, false);
                                Logger.Info("Removed Run key autostart value.");
                            }
                        }
                        catch (Exception exDel)
                        {
                            Logger.Warn($"Unable to remove Run key value: {exDel.Message}");
                        }

                        // Mark as disabled in StartupApproved so Windows reflects the state in Startup Apps UI
                        try
                        {
                            // 0x03 indicates disabled; remaining bytes are timestamp metadata (zeros are acceptable)
                            byte[] disabled = new byte[12];
                            disabled[0] = 0x03;
                            approvedKey?.SetValue(APP_NAME, disabled, RegistryValueKind.Binary);
                            Logger.Info("Set StartupApproved disabled marker.");
                        }
                        catch (Exception exSet)
                        {
                            Logger.Warn($"Unable to set StartupApproved disabled marker: {exSet.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"RegistryHelper.SetStartup({enabled}) failed");
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

                bool result = hasRunEntry && !isDisabledByApproval;
                Logger.Info($"RegistryHelper.IsStartupEnabled -> hasRunEntry={hasRunEntry}, disabledByApproval={isDisabledByApproval}, result={result}");
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "RegistryHelper.IsStartupEnabled failed");
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
                {
                    Logger.Info($"GetExecutablePath -> using Process.MainModule '{processPath}'");
                    return processPath;
                }
                
                // For single-file apps, use AppContext.BaseDirectory
                string baseDirectory = AppContext.BaseDirectory;
                string exePath = Path.Combine(baseDirectory, "BearsAdaClock.exe");
                if (File.Exists(exePath))
                {
                    Logger.Info($"GetExecutablePath -> using BaseDirectory exe '{exePath}'");
                    return exePath;
                }
                
                // Fallback to assembly location (for non-single-file deployments)
                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(assemblyPath))
                {
                    if (assemblyPath.EndsWith(".dll"))
                    {
                        string dllExePath = Path.ChangeExtension(assemblyPath, ".exe");
                        if (File.Exists(dllExePath)) { Logger.Info($"GetExecutablePath -> using dll exe '{dllExePath}'"); return dllExePath; }
                        string directory = Path.GetDirectoryName(assemblyPath);
                        string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
                        string potentialExePath = Path.Combine(directory, assemblyName + ".exe");
                        if (File.Exists(potentialExePath)) { Logger.Info($"GetExecutablePath -> using assembly exe '{potentialExePath}'"); return potentialExePath; }
                        string clockExePath = Path.Combine(directory, "BearsAdaClock.exe");
                        if (File.Exists(clockExePath)) { Logger.Info($"GetExecutablePath -> using clock exe '{clockExePath}'"); return clockExePath; }
                    }
                    Logger.Info($"GetExecutablePath -> using assembly path '{assemblyPath}'");
                    return assemblyPath;
                }
                
                // Final fallback - use the process path or base directory
                Logger.Warn($"GetExecutablePath -> fallback to '{processPath ?? baseDirectory}'");
                return processPath ?? baseDirectory;
            }
            catch (Exception ex)
            { 
                Logger.Error(ex, "GetExecutablePath failed");
                return AppContext.BaseDirectory; 
            }
        }
    }
}
