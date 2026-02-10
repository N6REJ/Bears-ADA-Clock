using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;

namespace BearsAdaClock
{
    /// <summary>
    /// Simple registry-based startup management, similar to WinKill's approach.
    /// Directly writes to HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
    /// </summary>
    public static class RegistryHelper
    {
        private const string RUN_REGISTRY_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string APP_NAME = "BearsAdaClock";

        /// <summary>
        /// Enable or disable startup with Windows by adding/removing registry entry.
        /// </summary>
        public static void SetStartup(bool enabled)
        {
            try
            {
                string exePath = GetExecutablePath();
                
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_REGISTRY_PATH, true))
                {
                    if (key == null)
                    {
                        Logger.Error("Failed to open registry key for startup configuration");
                        return;
                    }

                    if (enabled)
                    {
                        // Add to startup - use quoted path and add --autostart flag
                        key.SetValue(APP_NAME, $"\"{exePath}\" --autostart", RegistryValueKind.String);
                        Logger.Info($"Added to startup: {exePath} --autostart");
                    }
                    else
                    {
                        // Remove from startup
                        if (key.GetValue(APP_NAME) != null)
                        {
                            key.DeleteValue(APP_NAME, false);
                            Logger.Info("Removed from startup");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to {(enabled ? "enable" : "disable")} startup");
                throw new Exception($"Failed to {(enabled ? "enable" : "disable")} startup: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if the application is configured to start with Windows.
        /// </summary>
        public static bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_REGISTRY_PATH, false))
                {
                    if (key == null)
                        return false;

                    object value = key.GetValue(APP_NAME);
                    return value != null;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to check startup status");
                return false;
            }
        }

        /// <summary>
        /// Get the full path to the current executable.
        /// </summary>
        internal static string GetExecutablePath()
        {
            try
            {
                // Environment.ProcessPath is the most reliable way in .NET 6+ 
                // to get the path of the executable that started the process.
                string processPath = Environment.ProcessPath;
                
                if (!string.IsNullOrEmpty(processPath) && 
                    File.Exists(processPath) && 
                    processPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    return processPath;
                }

                // Fallback to MainModule for older .NET or specific scenarios
                processPath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(processPath) && 
                    File.Exists(processPath) && 
                    processPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    return processPath;
                }

                // Final fallback to base directory
                return Path.Combine(AppContext.BaseDirectory, "BearsAdaClock.exe");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get executable path");
                return Path.Combine(AppContext.BaseDirectory, "BearsAdaClock.exe");
            }
        }
    }
}
