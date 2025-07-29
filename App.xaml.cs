using System.Windows;
using System.Windows.Automation;

namespace BearsAdaClock
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Ensure the application is accessible
            AutomationProperties.SetName(this.MainWindow, "Bears ADA Clock");
        }
    }
}
