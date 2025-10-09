using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Globalization;
using System.Windows.Automation;
using Microsoft.Win32;
using BearsAdaClock.Properties;

namespace BearsAdaClock
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer;
        private SettingsWindow settingsWindow;
        public double DigitSize { get; set; } = 56;
        public double DateSize { get; set; } = 32;
        public Brush DigitColor { get; set; } = Brushes.Black;
        public Brush DateColor { get; set; } = Brushes.Black;
        public string DateFormat { get; set; } = "yyyy MMM dd";
        public string DisplayMode { get; set; } = "Both";
        public bool ShowSeconds { get; set; } = false;

        public MainWindow()
        {
            Logger.Info("MainWindow constructor started");
            
            try
            {
                InitializeComponent();
                Logger.Info("InitializeComponent completed");
                
                LoadSettings();
                Logger.Info("Settings loaded");
                
                InitializeTimer();
                Logger.Info("Timer initialized");
                
                UpdateDisplay();
                Logger.Info("Display updated");
                
                this.Loaded += MainWindow_Loaded;
                InitializeStartupSetting();
                Logger.Info("Startup setting initialized");
                
                Logger.Info("MainWindow constructor completed successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow constructor failed", ex);
                throw;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Logger.Info("MainWindow_Loaded event fired");
            
            try
            {
                PositionWindow();
                Logger.Info($"Window positioned at Left={this.Left}, Top={this.Top}");
                
                // Ensure timer is running after window is fully loaded
                if (timer != null && !timer.IsEnabled)
                {
                    timer.Start();
                    Logger.Info("Timer started");
                }
                else if (timer == null)
                {
                    Logger.Warning("Timer is null in MainWindow_Loaded");
                }
                else
                {
                    Logger.Info("Timer already running");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("MainWindow_Loaded failed", ex);
            }
        }

        private void LoadSettings()
        {
            try
            {
                DigitSize = Settings.Default.DigitSize;
                DateSize = Settings.Default.DateSize;
                DigitColor = GetBrushFromName(Settings.Default.DigitColor);
                DateColor = GetBrushFromName(Settings.Default.DateColor);
                DateFormat = Settings.Default.DateFormat;
                DisplayMode = Settings.Default.DisplayMode;
                ShowSeconds = Settings.Default.ShowSeconds;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load settings: {ex.Message}", "Settings Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        public void SaveSettings()
        {
            try
            {
                Settings.Default.DigitSize = DigitSize;
                Settings.Default.DateSize = DateSize;
                Settings.Default.DigitColor = GetColorName(DigitColor);
                Settings.Default.DateColor = GetColorName(DateColor);
                Settings.Default.DateFormat = DateFormat;
                Settings.Default.DisplayMode = DisplayMode;
                Settings.Default.ShowSeconds = ShowSeconds;
                Settings.Default.WindowLeft = this.Left;
                Settings.Default.WindowTop = this.Top;
                Settings.Default.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Settings Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private Brush GetBrushFromName(string colorName)
        {
            switch (colorName)
            {
                case "Black": return Brushes.Black;
                case "White": return Brushes.White;
                case "Red": return Brushes.Red;
                case "Blue": return Brushes.Blue;
                case "Green": return Brushes.Green;
                case "Yellow": return Brushes.Yellow;
                case "Orange": return Brushes.Orange;
                case "Purple": return Brushes.Purple;
                case "Gray": return Brushes.Gray;
                default: return Brushes.Black;
            }
        }

        private string GetColorName(Brush brush)
        {
            if (brush == Brushes.Black) return "Black";
            if (brush == Brushes.White) return "White";
            if (brush == Brushes.Red) return "Red";
            if (brush == Brushes.Blue) return "Blue";
            if (brush == Brushes.Green) return "Green";
            if (brush == Brushes.Yellow) return "Yellow";
            if (brush == Brushes.Orange) return "Orange";
            if (brush == Brushes.Purple) return "Purple";
            if (brush == Brushes.Gray) return "Gray";
            return "Black";
        }

        private void InitializeTimer()
        {
            try
            {
                Logger.Info("Creating DispatcherTimer");
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += Timer_Tick;
                timer.Start();
                Logger.Info("Timer started successfully");
            }
            catch (Exception ex)
            {
                Logger.Error("Timer initialization failed", ex);
                System.Diagnostics.Debug.WriteLine($"Timer initialization failed: {ex.Message}");
                
                // Try to reinitialize after a delay
                this.Dispatcher.BeginInvoke(new Action(() => {
                    try
                    {
                        Logger.Info("Attempting to reinitialize timer");
                        if (timer == null)
                        {
                            timer = new DispatcherTimer();
                            timer.Interval = TimeSpan.FromSeconds(1);
                            timer.Tick += Timer_Tick;
                        }
                        if (!timer.IsEnabled)
                        {
                            timer.Start();
                            Logger.Info("Timer reinitialized successfully");
                        }
                    }
                    catch (Exception retryEx)
                    {
                        Logger.Error("Timer reinitialization failed", retryEx);
                    }
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void PositionWindow()
        {
            try
            {
                Logger.Info("Positioning window");
                var workingArea = SystemParameters.WorkArea;
                Logger.Info($"Working area: Left={workingArea.Left}, Top={workingArea.Top}, Right={workingArea.Right}, Bottom={workingArea.Bottom}");
                Logger.Info($"Saved position: Left={Settings.Default.WindowLeft}, Top={Settings.Default.WindowTop}");
                Logger.Info($"Window actual size: Width={this.ActualWidth}, Height={this.ActualHeight}");
                
                if (Settings.Default.WindowLeft >= 0 && Settings.Default.WindowTop >= 0)
                {
                    if (Settings.Default.WindowLeft >= workingArea.Left &&
                        Settings.Default.WindowLeft < workingArea.Right - 100 &&
                        Settings.Default.WindowTop >= workingArea.Top &&
                        Settings.Default.WindowTop < workingArea.Bottom - 50)
                    {
                        this.Left = Settings.Default.WindowLeft;
                        this.Top = Settings.Default.WindowTop;
                        Logger.Info("Using saved window position");
                        return;
                    }
                    else
                    {
                        Logger.Warning("Saved position is outside working area, using default position");
                    }
                }
                else
                {
                    Logger.Info("No saved position found, using default position");
                }
                
                this.Left = workingArea.Right - this.ActualWidth - 64;
                this.Top = workingArea.Top + 64;
                Logger.Info($"Set default position: Left={this.Left}, Top={this.Top}");
                SaveSettings();
            }
            catch (Exception ex)
            {
                Logger.Error("Error positioning window, using center screen", ex);
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            var now = DateTime.Now;
            string timeFormat = ShowSeconds ? "HH:mm:ss" : "HH:mm";
            TimeDisplay.Text = now.ToString(timeFormat);
            TimeDisplay.FontSize = DigitSize;
            TimeDisplay.Foreground = DigitColor;
            DateDisplay.Text = FormatDate(now, DateFormat);
            DateDisplay.FontSize = DateSize;
            DateDisplay.Foreground = DateColor;
            UpdateDisplayMode();
            string timeAnnouncement = ShowSeconds ? now.ToString("h:mm:ss tt") : now.ToString("h:mm tt");
            if (DisplayMode != "DateOnly")
                AutomationProperties.SetName(TimeDisplay, $"Current time is {timeAnnouncement}");
            if (DisplayMode != "TimeOnly")
                AutomationProperties.SetName(DateDisplay, $"Current date is {FormatDate(now, DateFormat)}");
        }

        private void UpdateDisplayMode()
        {
            ClockPanel.Children.Clear();
            string safeDisplayMode = DisplayMode;
            if (string.IsNullOrEmpty(safeDisplayMode) ||
                (safeDisplayMode != "Both" && safeDisplayMode != "DateAbove" &&
                 safeDisplayMode != "TimeOnly" && safeDisplayMode != "DateOnly"))
            {
                safeDisplayMode = "Both";
                DisplayMode = "Both";
            }
            switch (safeDisplayMode)
            {
                case "Both":
                    ClockPanel.Children.Add(TimeDisplay);
                    ClockPanel.Children.Add(DateDisplay);
                    TimeDisplay.Visibility = Visibility.Visible;
                    DateDisplay.Visibility = Visibility.Visible;
                    TimeDisplay.Margin = new Thickness(0, 0, 0, 0);
                    DateDisplay.Margin = new Thickness(0, 5, 0, 0);
                    break;
                case "DateAbove":
                    ClockPanel.Children.Add(DateDisplay);
                    ClockPanel.Children.Add(TimeDisplay);
                    TimeDisplay.Visibility = Visibility.Visible;
                    DateDisplay.Visibility = Visibility.Visible;
                    DateDisplay.Margin = new Thickness(0, 0, 0, 0);
                    TimeDisplay.Margin = new Thickness(0, 5, 0, 0);
                    break;
                case "TimeOnly":
                    ClockPanel.Children.Add(TimeDisplay);
                    TimeDisplay.Visibility = Visibility.Visible;
                    DateDisplay.Visibility = Visibility.Collapsed;
                    TimeDisplay.Margin = new Thickness(0, 0, 0, 0);
                    break;
                case "DateOnly":
                    ClockPanel.Children.Add(DateDisplay);
                    TimeDisplay.Visibility = Visibility.Collapsed;
                    DateDisplay.Visibility = Visibility.Visible;
                    DateDisplay.Margin = new Thickness(0, 0, 0, 0);
                    break;
                default:
                    ClockPanel.Children.Add(TimeDisplay);
                    ClockPanel.Children.Add(DateDisplay);
                    TimeDisplay.Visibility = Visibility.Visible;
                    DateDisplay.Visibility = Visibility.Visible;
                    TimeDisplay.Margin = new Thickness(0, 0, 0, 0);
                    DateDisplay.Margin = new Thickness(0, 5, 0, 0);
                    DisplayMode = "Both";
                    break;
            }
        }

        private string FormatDate(DateTime date, string format)
        {
            switch (format)
            {
                case "MMM dd, yyyy": return date.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);
                case "dd MMM yyyy": return date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
                case "yyyy MMM dd":
                default: return date.ToString("yyyy MMM dd", CultureInfo.InvariantCulture);
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
                SaveSettings();
            }
        }

        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Logger.Info("Window right-click detected");
            try
            {
                ClockContextMenu.IsOpen = true;
                e.Handled = true;
                Logger.Info("Context menu opened from window");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to open context menu from window", ex);
            }
        }

        private void Grid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Logger.Info("Grid right-click detected");
            try
            {
                ClockContextMenu.IsOpen = true;
                e.Handled = true;
                Logger.Info("Context menu opened from grid");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to open context menu from grid", ex);
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            if (settingsWindow == null || !settingsWindow.IsLoaded)
                settingsWindow = new SettingsWindow(this);
            settingsWindow.Show();
            settingsWindow.Activate();
            settingsWindow.Focus();
        }

        private void ShowSettingsFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("Opening settings folder");
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string n6rejPath = Path.Combine(appData, "N6REJ");
                string[] dirs = Directory.GetDirectories(n6rejPath, "*BearsAdaClock*");
                if (dirs.Length > 0)
                {
                    System.Diagnostics.Process.Start("explorer.exe", dirs[0]);
                    Logger.Info($"Opened settings folder: {dirs[0]}");
                }
                else
                {
                    Logger.Warning("No settings folder found");
                    MessageBox.Show("No settings folder found in AppData.", "Show Settings Folder", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error opening settings folder", ex);
                MessageBox.Show($"Error opening settings folder: {ex.Message}", "Show Settings Folder", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowLogsFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Logger.Info("Opening logs folder");
                string logDirectory = Logger.GetLogDirectory();
                
                if (Directory.Exists(logDirectory))
                {
                    System.Diagnostics.Process.Start("explorer.exe", logDirectory);
                    Logger.Info($"Opened logs folder: {logDirectory}");
                }
                else
                {
                    // Create the directory if it doesn't exist
                    Directory.CreateDirectory(logDirectory);
                    System.Diagnostics.Process.Start("explorer.exe", logDirectory);
                    Logger.Info($"Created and opened logs folder: {logDirectory}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error opening logs folder", ex);
                MessageBox.Show($"Error opening logs folder: {ex.Message}\n\nLog path: {Logger.GetLogDirectory()}", 
                    "Show Logs Folder", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Logger.Info("Application exit requested by user");
            
            // Add confirmation to prevent accidental exits
            var result = MessageBox.Show(
                "Are you sure you want to exit Bears ADA Clock?",
                "Confirm Exit",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                Logger.Info("Exit confirmed by user");
                SaveSettings();
                Application.Current.Shutdown();
            }
            else
            {
                Logger.Info("Exit cancelled by user");
            }
        }

        public void ApplySettings()
        {
            UpdateDisplay();
            SaveSettings();
            this.Dispatcher.BeginInvoke(new Action(() => {
                var workingArea = SystemParameters.WorkArea;
                if (this.Left < workingArea.Left - this.ActualWidth + 50 ||
                    this.Left > workingArea.Right - 50 ||
                    this.Top < workingArea.Top - this.ActualHeight + 50 ||
                    this.Top > workingArea.Bottom - 50)
                {
                    this.Left = workingArea.Right - this.ActualWidth - 64;
                    this.Top = workingArea.Top + 64;
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        protected override void OnClosed(EventArgs e)
        {
            SaveSettings();
            base.OnClosed(e);
        }

        #region Startup Management
        private void InitializeStartupSetting()
        {
            try
            {
                bool isFirstRun = IsFirstRun();
                if (isFirstRun)
                {
                    RegistryHelper.SetStartup(true);
                    Settings.Default.StartWithWindows = true;
                    Settings.Default.Save();
                    MarkAsRun();
                }
                else
                {
                    SynchronizeStartupSetting();
                }
            }
            catch { }
        }
        
        private void SynchronizeStartupSetting()
        {
            try
            {
                bool registryStartupEnabled = RegistryHelper.IsStartupEnabled();
                bool settingsStartupEnabled = Settings.Default.StartWithWindows;
                if (registryStartupEnabled != settingsStartupEnabled)
                {
                    Settings.Default.StartWithWindows = registryStartupEnabled;
                    Settings.Default.Save();
                }
            }
            catch { }
        }
        
        private bool IsFirstRun()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\N6REJ\BearsAdaClock", false))
                {
                    return key?.GetValue("HasRun") == null;
                }
            }
            catch { return true; }
        }
        
        private void MarkAsRun()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\N6REJ\BearsAdaClock"))
                {
                    key?.SetValue("HasRun", "true");
                }
            }
            catch { }
        }
        #endregion
    }
}
