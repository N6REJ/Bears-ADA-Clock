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
            base.OnStartup(e);
            
            // Simple settings initialization
            try
            {
                // Check if this is a fresh installation
                if (Settings.Default.UpgradeRequired)
                {
                    Settings.Default.Upgrade();
                    Settings.Default.UpgradeRequired = false;
                    Settings.Default.Save();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Settings initialization failed: {ex.Message}");
                // Reset to defaults if settings are corrupted
                Settings.Default.Reset();
                Settings.Default.Save();
            }
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
