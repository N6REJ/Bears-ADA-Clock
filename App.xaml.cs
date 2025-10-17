using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using BearsAdaClock.Properties;
using System.Diagnostics;
using System.Reflection;

namespace BearsAdaClock
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Attach global exception handlers early
            AppDomain.CurrentDomain.UnhandledException += (s, ex) => Logger.Error(ex.ExceptionObject as Exception ?? new Exception(ex.ExceptionObject?.ToString() ?? "Unknown"), "AppDomain.UnhandledException");
            this.DispatcherUnhandledException += (s, ex) => { Logger.Error(ex.Exception, "DispatcherUnhandledException"); ex.Handled = true; };
            TaskScheduler.UnobservedTaskException += (s, ex) => { Logger.Error(ex.Exception, "TaskScheduler.UnobservedTaskException"); ex.SetObserved(); };

            Logger.Info($"App.OnStartup - args='{string.Join(" ", e.Args ?? Array.Empty<string>())}', workingDir='{Environment.CurrentDirectory}', user='{Environment.UserName}'");
            Logger.Info($"Logger path in use: {Logger.LogFilePath}");

            // Log application version being used
            try
            {
                string version = null;
                string exe = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exe))
                {
                    var fvi = FileVersionInfo.GetVersionInfo(exe);
                    if (!string.IsNullOrWhiteSpace(fvi.FileVersion))
                        version = fvi.FileVersion;
                    else if (!string.IsNullOrWhiteSpace(fvi.ProductVersion))
                        version = fvi.ProductVersion;
                }
                if (string.IsNullOrWhiteSpace(version))
                {
                    var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                    version = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                              ?? asm.GetName().Version?.ToString();
                }
                Logger.Info($"Application version: {version ?? "unknown"}");
            }
            catch (Exception vex)
            {
                Logger.Error(vex, "Failed to resolve application version during startup");
            }

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
                    Logger.Info("Settings upgraded from previous version.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Settings initialization failed");
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
            Logger.Info("App.OnActivated");
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Info("App.OnExit - saving settings");
            // Ensure settings are saved when application exits
            Settings.Default.Save();
            base.OnExit(e);
        }
    }
}
