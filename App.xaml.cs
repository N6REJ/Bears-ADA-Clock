using System;
using System.Windows;
using System.Windows.Automation;
using BearsAdaClock.Properties;

namespace BearsAdaClock
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Logger.Info("=== Application Starting ===");
            Logger.Info($"Command line args: {string.Join(" ", e.Args)}");
            
            base.OnStartup(e);
            
            // Simple settings initialization
            try
            {
                Logger.Info("Initializing settings...");
                
                // Check if this is a fresh installation
                if (Settings.Default.UpgradeRequired)
                {
                    Logger.Info("Upgrading settings from previous version");
                    Settings.Default.Upgrade();
                    Settings.Default.UpgradeRequired = false;
                    Settings.Default.Save();
                    Logger.Info("Settings upgraded successfully");
                }
                
                Logger.Info($"Logging enabled: {Settings.Default.EnableLogging}");
                Logger.Info($"Start with Windows: {Settings.Default.StartWithWindows}");
                Logger.Info($"Display mode: {Settings.Default.DisplayMode}");
                Logger.Info($"Window position: Left={Settings.Default.WindowLeft}, Top={Settings.Default.WindowTop}");
            }
            catch (Exception ex)
            {
                Logger.Error("Settings initialization failed", ex);
                System.Diagnostics.Debug.WriteLine($"Settings initialization failed: {ex.Message}");
                // Reset to defaults if settings are corrupted
                Settings.Default.Reset();
                Settings.Default.Save();
                Logger.Info("Settings reset to defaults");
            }
            
            Logger.Info("Application startup completed");
        }
        
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            
            // Ensure the application is accessible when activated
            if (this.MainWindow != null)
            {
                AutomationProperties.SetName(this.MainWindow, "Bears ADA Clock");
            }
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            // Ensure settings are saved when application exits
            Settings.Default.Save();
            base.OnExit(e);
        }
    }
}
