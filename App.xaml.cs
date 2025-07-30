using System;
using System.Windows;
using System.Windows.Automation;

namespace BearsAdaClock
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
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
    }
}
