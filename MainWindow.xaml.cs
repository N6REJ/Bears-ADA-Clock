using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Globalization;
using System.Windows.Automation;

namespace BearsAdaClock
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer;
        private SettingsWindow settingsWindow;
        
        // Settings properties
        public double DigitSize { get; set; } = 24;
        public double DateSize { get; set; } = 24;
        public Brush DigitColor { get; set; } = Brushes.Black;
        public Brush DateColor { get; set; } = Brushes.Black;
        public string DateFormat { get; set; } = "yyyy MMM dd";
        public string DisplayMode { get; set; } = "Both";
        public bool ShowSeconds { get; set; } = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeTimer();
            PositionWindow();
            UpdateDisplay();
        }

        private void InitializeTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void PositionWindow()
        {
            // Position window 64px from top and right edges
            var workingArea = SystemParameters.WorkArea;
            this.Left = workingArea.Right - this.Width - 64;
            this.Top = workingArea.Top + 64;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            var now = DateTime.Now;
            
            // Update time display with or without seconds
            string timeFormat = ShowSeconds ? "HH:mm:ss" : "HH:mm";
            TimeDisplay.Text = now.ToString(timeFormat);
            TimeDisplay.FontSize = DigitSize;
            TimeDisplay.Foreground = DigitColor;
            
            // Update date display
            DateDisplay.Text = FormatDate(now, DateFormat);
            DateDisplay.FontSize = DateSize;
            DateDisplay.Foreground = DateColor;
            
            // Update display mode and visibility
            UpdateDisplayMode();
            
            // Update automation properties for screen readers
            string timeAnnouncement = ShowSeconds ? now.ToString("h:mm:ss tt") : now.ToString("h:mm tt");
            
            if (DisplayMode != "DateOnly")
            {
                AutomationProperties.SetName(TimeDisplay, $"Current time is {timeAnnouncement}");
            }
            
            if (DisplayMode != "TimeOnly")
            {
                AutomationProperties.SetName(DateDisplay, $"Current date is {FormatDate(now, DateFormat)}");
            }
        }

        /// <summary>
        /// Updates the display mode ensuring at least one element (time or date) is always visible.
        /// This prevents the clock from being completely empty.
        /// </summary>
        private void UpdateDisplayMode()
        {
            // Clear the panel and re-add elements in the correct order
            ClockPanel.Children.Clear();

            // Safety check: Ensure we never have an empty display
            string safeDisplayMode = DisplayMode;
            if (string.IsNullOrEmpty(safeDisplayMode) || 
                (safeDisplayMode != "Both" && safeDisplayMode != "DateAbove" && 
                 safeDisplayMode != "TimeOnly" && safeDisplayMode != "DateOnly"))
            {
                safeDisplayMode = "Both"; // Safe default that always shows something
                DisplayMode = "Both"; // Update the property to the safe value
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
                    // Additional safety fallback (should never reach here due to validation above)
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
                case "MMM dd, yyyy":
                    return date.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);
                case "dd MMM yyyy":
                    return date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
                case "yyyy MMM dd":
                default:
                    return date.ToString("yyyy MMM dd", CultureInfo.InvariantCulture);
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Allow dragging the window
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            if (settingsWindow == null || !settingsWindow.IsLoaded)
            {
                settingsWindow = new SettingsWindow(this);
            }
            
            settingsWindow.Show();
            settingsWindow.Activate();
            settingsWindow.Focus();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        public void ApplySettings()
        {
            UpdateDisplay();
        }
    }
}
