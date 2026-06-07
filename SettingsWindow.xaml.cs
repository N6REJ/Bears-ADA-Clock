using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Automation;
using Microsoft.Win32;
using BearsAdaClock.Properties;

#nullable enable
namespace BearsAdaClock
{
    public partial class SettingsWindow : Window
    {
        private MainWindow mainWindow;

        public SettingsWindow(MainWindow parent)
        {
            InitializeComponent();
            mainWindow = parent;

            // Set version text from assembly information
            try
            {
                // Use the version from the AssemblyName, which is what we updated in the .csproj
                var assembly = Assembly.GetExecutingAssembly();
                string version = assembly.GetName().Version?.ToString() ?? "0.0.0.0";
                
                // Clean up trailing .0 if it's the 4th component and not needed
                if (version.EndsWith(".0") && version.Split('.').Length == 4)
                {
                    version = version.Substring(0, version.Length - 2);
                }

                if (!string.IsNullOrWhiteSpace(version))
                {
                    VersionText.Text = $"Version {version}";
                }
            }
            catch { }

            LoadCurrentSettings();
            UpdatePreview();
        }

        private void LoadCurrentSettings()
        {
            // Load current settings from main window
            DigitSizeSlider.Value = mainWindow.DigitSize;
            DateSizeSlider.Value = mainWindow.DateSize;
            DayOfWeekSizeSlider.Value = mainWindow.DayOfWeekSize;
            ShowSecondsCheckBox.IsChecked = mainWindow.ShowSeconds;
            
            // Set color selections
            SetColorComboSelection(DigitColorCombo, mainWindow.DigitColor);
            SetColorComboSelection(DateColorCombo, mainWindow.DateColor);
            SetColorComboSelection(DayOfWeekColorCombo, mainWindow.DayOfWeekColor);
            
            // Set date format selection
            SetDateFormatSelection(mainWindow.DateFormat);
            
            // Set day of week format selection
            SetDayOfWeekFormatSelection(mainWindow.DayOfWeekFormat);
            
            // Set display mode selection
            SetDisplayModeSelection(mainWindow.DisplayMode);
            
            // Load startup setting
            StartWithWindowsCheckBox.IsChecked = IsStartupEnabled();
            
            // Load logging setting
            EnableLoggingCheckBox.IsChecked = Settings.Default.EnableLogging;
            
            // Update value displays
            DigitSizeValue.Text = $"{mainWindow.DigitSize:F0}px";
            DateSizeValue.Text = $"{mainWindow.DateSize:F0}px";
            DayOfWeekSizeValue.Text = $"{mainWindow.DayOfWeekSize:F0}px";
        }

        private void SetColorComboSelection(ComboBox combo, Brush brush)
        {
            string colorName = GetColorName(brush);
            foreach (ComboBoxItem item in combo.Items)
            {
                if (item.Tag.ToString() == colorName)
                {
                    combo.SelectedItem = item;
                    break;
                }
            }
        }

        private void SetDateFormatSelection(string format)
        {
            foreach (ComboBoxItem item in DateFormatCombo.Items)
            {
                if (item.Tag.ToString() == format)
                {
                    DateFormatCombo.SelectedItem = item;
                    break;
                }
            }
        }

        private void SetDayOfWeekFormatSelection(string format)
        {
            foreach (ComboBoxItem item in DayOfWeekFormatCombo.Items)
            {
                if (item.Tag.ToString() == format)
                {
                    DayOfWeekFormatCombo.SelectedItem = item;
                    break;
                }
            }
        }

        private void SetDisplayModeSelection(string displayMode)
        {
            foreach (ComboBoxItem item in DisplayModeCombo.Items)
            {
                if (item.Tag.ToString() == displayMode)
                {
                    DisplayModeCombo.SelectedItem = item;
                    break;
                }
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

        /// <summary>
        /// Validates that the selected display mode shows at least one element (time or date).
        /// This prevents the clock from being completely empty.
        /// </summary>
        /// <param name="displayMode">The display mode to validate</param>
        /// <returns>True if the mode is valid (shows at least one element), false otherwise</returns>
        private bool IsValidDisplayMode(string displayMode)
        {
            // All current display modes show at least one element:
            // - "Both": shows time and date
            // - "DateAbove": shows time and date (different order)
            // - "TimeOnly": shows time only
            // - "DateOnly": shows date only
            // 
            // This validation prevents future modifications from accidentally
            // creating a mode that hides both time and date.
            
            switch (displayMode)
            {
                case "Both":
                case "DateAbove":
                case "TimeOnly":
                case "DateOnly":
                case "DayOnly":
                    return true;
                default:
                    return false; // Unknown or invalid mode
            }
        }

        private void DayOfWeekSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DayOfWeekSizeValue != null)
            {
                DayOfWeekSizeValue.Text = $"{e.NewValue:F0}px";
                UpdatePreview();
                
                // Announce change to screen readers
                AutomationProperties.SetName(DayOfWeekSizeSlider, $"Day of Week Size Slider - Current value {e.NewValue:F0} pixels");
            }
        }

        private void DigitSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DigitSizeValue != null)
            {
                DigitSizeValue.Text = $"{e.NewValue:F0}px";
                UpdatePreview();
                
                // Announce change to screen readers
                AutomationProperties.SetName(DigitSizeSlider, $"Digit Size Slider - Current value {e.NewValue:F0} pixels");
            }
        }

        private void DateSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DateSizeValue != null)
            {
                DateSizeValue.Text = $"{e.NewValue:F0}px";
                UpdatePreview();
                
                // Announce change to screen readers
                AutomationProperties.SetName(DateSizeSlider, $"Date Size Slider - Current value {e.NewValue:F0} pixels");
            }
        }

        private void DigitColorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePreview();
            
            // Announce change to screen readers
            if (DigitColorCombo.SelectedItem is ComboBoxItem item)
            {
                AutomationProperties.SetName(DigitColorCombo, $"Digit Color Selection - Current selection {item.Content}");
            }
        }

        private void DateColorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePreview();
            
            // Announce change to screen readers
            if (DateColorCombo.SelectedItem is ComboBoxItem item)
            {
                AutomationProperties.SetName(DateColorCombo, $"Date Color Selection - Current selection {item.Content}");
            }
        }

        private void DayOfWeekColorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePreview();
            
            // Announce change to screen readers
            if (DayOfWeekColorCombo.SelectedItem is ComboBoxItem item)
            {
                AutomationProperties.SetName(DayOfWeekColorCombo, $"Day of Week Color Selection - Current selection {item.Content}");
            }
        }

        private void DateFormatCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePreview();
            
            // Announce change to screen readers
            if (DateFormatCombo.SelectedItem is ComboBoxItem item)
            {
                AutomationProperties.SetName(DateFormatCombo, $"Date Format Selection - Current selection {item.Content}");
            }
        }

        private void DayOfWeekFormatCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePreview();
            
            // Announce change to screen readers
            if (DayOfWeekFormatCombo.SelectedItem is ComboBoxItem item)
            {
                AutomationProperties.SetName(DayOfWeekFormatCombo, $"Day of Week Format Selection - Current selection {item.Content}");
            }
        }

        private void DisplayModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePreview();
            
            // Announce change to screen readers
            if (DisplayModeCombo.SelectedItem is ComboBoxItem item)
            {
                AutomationProperties.SetName(DisplayModeCombo, $"Display Mode Selection - Current selection {item.Content}");
            }
        }

        private void ShowSecondsCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdatePreview();
            
            // Announce change to screen readers
            bool isChecked = ShowSecondsCheckBox.IsChecked == true;
            AutomationProperties.SetName(ShowSecondsCheckBox, $"Show Seconds Checkbox - Currently {(isChecked ? "checked" : "unchecked")}");
        }

        private void UpdatePreview()
        {
            if (PreviewTime == null || PreviewDate == null || PreviewDayOfWeek == null || PreviewPanel == null) return;

            // Update preview time with or without seconds
            bool showSeconds = ShowSecondsCheckBox.IsChecked == true;
            PreviewTime.Text = showSeconds ? "12:34:56" : "12:34";
            PreviewTime.FontSize = DigitSizeSlider.Value;
            if (DigitColorCombo.SelectedItem is ComboBoxItem digitColorItem && digitColorItem.Tag != null)
            {
                PreviewTime.Foreground = GetBrushFromName(digitColorItem.Tag.ToString() ?? "Black");
            }

            // Update preview date
            PreviewDate.FontSize = DateSizeSlider.Value;
            if (DateColorCombo.SelectedItem is ComboBoxItem dateColorItem && dateColorItem.Tag != null)
            {
                PreviewDate.Foreground = GetBrushFromName(dateColorItem.Tag.ToString() ?? "Black");
            }

            // Update date format
            if (DateFormatCombo.SelectedItem is ComboBoxItem formatItem && formatItem.Tag != null)
            {
                var sampleDate = new DateTime(2024, 1, 15);
                PreviewDate.Text = FormatDate(sampleDate, formatItem.Tag.ToString() ?? "yyyy MMM dd");
            }

            // Update preview day of week
            PreviewDayOfWeek.FontSize = DayOfWeekSizeSlider.Value;
            if (DayOfWeekColorCombo.SelectedItem is ComboBoxItem dayColorItem && dayColorItem.Tag != null)
            {
                PreviewDayOfWeek.Foreground = GetBrushFromName(dayColorItem.Tag.ToString() ?? "Black");
            }

            // Update day of week format
            if (DayOfWeekFormatCombo.SelectedItem is ComboBoxItem dayFormatItem && dayFormatItem.Tag != null)
            {
                var sampleDate = new DateTime(2024, 1, 15); // Monday
                PreviewDayOfWeek.Text = sampleDate.ToString(dayFormatItem.Tag.ToString() ?? "dddd");
            }

            // Update display mode - ensures at least one element is always visible
            if (DisplayModeCombo.SelectedItem is ComboBoxItem modeItem && modeItem.Tag != null)
            {
                string mode = modeItem.Tag.ToString() ?? "Both";
                PreviewPanel.Children.Clear();

                switch (mode)
                {
                    case "Both":
                        PreviewPanel.Children.Add(PreviewTime);
                        PreviewPanel.Children.Add(PreviewDayOfWeek);
                        PreviewPanel.Children.Add(PreviewDate);
                        PreviewTime.Visibility = Visibility.Visible;
                        PreviewDate.Visibility = Visibility.Visible;
                        PreviewDayOfWeek.Visibility = Visibility.Visible;
                        PreviewTime.Margin = new Thickness(0, 0, 0, 0);
                        PreviewDayOfWeek.Margin = new Thickness(0, 3, 0, 0);
                        PreviewDate.Margin = new Thickness(0, 3, 0, 0);
                        break;
                    case "DateAbove":
                        PreviewPanel.Children.Add(PreviewDayOfWeek);
                        PreviewPanel.Children.Add(PreviewDate);
                        PreviewPanel.Children.Add(PreviewTime);
                        PreviewTime.Visibility = Visibility.Visible;
                        PreviewDate.Visibility = Visibility.Visible;
                        PreviewDayOfWeek.Visibility = Visibility.Visible;
                        PreviewDayOfWeek.Margin = new Thickness(0, 0, 0, 0);
                        PreviewDate.Margin = new Thickness(0, 3, 0, 0);
                        PreviewTime.Margin = new Thickness(0, 3, 0, 0);
                        break;
                    case "TimeOnly":
                        PreviewPanel.Children.Add(PreviewTime);
                        PreviewTime.Visibility = Visibility.Visible;
                        PreviewDate.Visibility = Visibility.Collapsed;
                        PreviewDayOfWeek.Visibility = Visibility.Collapsed;
                        PreviewTime.Margin = new Thickness(0, 0, 0, 0);
                        break;
                    case "DateOnly":
                        PreviewPanel.Children.Add(PreviewDate);
                        PreviewTime.Visibility = Visibility.Collapsed;
                        PreviewDate.Visibility = Visibility.Visible;
                        PreviewDayOfWeek.Visibility = Visibility.Collapsed;
                        PreviewDate.Margin = new Thickness(0, 0, 0, 0);
                        break;
                    case "DayOnly":
                        PreviewPanel.Children.Add(PreviewDayOfWeek);
                        PreviewTime.Visibility = Visibility.Collapsed;
                        PreviewDate.Visibility = Visibility.Collapsed;
                        PreviewDayOfWeek.Visibility = Visibility.Visible;
                        PreviewDayOfWeek.Margin = new Thickness(0, 0, 0, 0);
                        break;
                }
            }
        }

        private string FormatDate(DateTime date, string format)
        {
            switch (format)
            {
                case "MMM dd, yyyy":
                    return date.ToString("MMM dd, yyyy", System.Globalization.CultureInfo.InvariantCulture);
                case "dd MMM yyyy":
                    return date.ToString("dd MMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
                case "yyyy MMM dd":
                default:
                    return date.ToString("yyyy MMM dd", System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            // Validate display mode to ensure at least one element is visible
            if (DisplayModeCombo.SelectedItem is ComboBoxItem validationModeItem && validationModeItem.Tag != null)
            {
                string selectedMode = validationModeItem.Tag.ToString() ?? "Both";
                
                if (!IsValidDisplayMode(selectedMode))
                {
                    MessageBox.Show("Invalid display mode selected. Please choose a valid option that displays at least the time or date.", 
                                  "Invalid Display Mode", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                MessageBox.Show("Please select a display mode.", "Missing Selection", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Apply settings to main window
            mainWindow.DigitSize = DigitSizeSlider.Value;
            mainWindow.DateSize = DateSizeSlider.Value;
            mainWindow.DayOfWeekSize = DayOfWeekSizeSlider.Value;
            mainWindow.ShowSeconds = ShowSecondsCheckBox.IsChecked == true;

            if (DigitColorCombo.SelectedItem is ComboBoxItem digitColorItem && digitColorItem.Tag != null)
            {
                mainWindow.DigitColor = GetBrushFromName(digitColorItem.Tag.ToString() ?? "Black");
            }

            if (DateColorCombo.SelectedItem is ComboBoxItem dateColorItem && dateColorItem.Tag != null)
            {
                mainWindow.DateColor = GetBrushFromName(dateColorItem.Tag.ToString() ?? "Black");
            }

            if (DayOfWeekColorCombo.SelectedItem is ComboBoxItem dayColorItem && dayColorItem.Tag != null)
            {
                mainWindow.DayOfWeekColor = GetBrushFromName(dayColorItem.Tag.ToString() ?? "Black");
            }

            if (DateFormatCombo.SelectedItem is ComboBoxItem formatItem && formatItem.Tag != null)
            {
                mainWindow.DateFormat = formatItem.Tag.ToString() ?? "yyyy MMM dd";
            }

            if (DayOfWeekFormatCombo.SelectedItem is ComboBoxItem dayFormatItem && dayFormatItem.Tag != null)
            {
                mainWindow.DayOfWeekFormat = dayFormatItem.Tag.ToString() ?? "dddd";
            }

            if (DisplayModeCombo.SelectedItem is ComboBoxItem applyModeItem && applyModeItem.Tag != null)
            {
                mainWindow.DisplayMode = applyModeItem.Tag.ToString() ?? "Both";
            }

            mainWindow.ApplySettings();
            
            // Announce to screen readers
            MessageBox.Show("Settings applied successfully!", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            // Ensure the settings window can be reopened
            mainWindow.Focus();
        }

        #region Startup Management

        private void StartWithWindowsCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            bool isChecked = StartWithWindowsCheckBox.IsChecked == true;
            
            try
            {
                if (isChecked)
                {
                    EnableStartup();
                }
                else
                {
                    DisableStartup();
                }
                
                // Announce change to screen readers
                AutomationProperties.SetName(StartWithWindowsCheckBox, 
                    $"Start with Windows Checkbox - Currently {(isChecked ? "enabled" : "disabled")}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update startup setting: {ex.Message}", 
                              "Startup Setting Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                
                // Revert checkbox state on error
                StartWithWindowsCheckBox.IsChecked = !isChecked;
            }
        }

        private bool IsStartupEnabled()
        {
            return RegistryHelper.IsStartupEnabled();
        }

        private void EnableStartup()
        {
            try
            {
                RegistryHelper.SetStartup(true);
                
                // Save startup setting
                Settings.Default.StartWithWindows = true;
                Settings.Default.Save();
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to add to startup: {ex.Message}");
            }
        }

        private void DisableStartup()
        {
            try
            {
                RegistryHelper.SetStartup(false);
                
                // Save startup setting
                Settings.Default.StartWithWindows = false;
                Settings.Default.Save();
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to remove from startup: {ex.Message}");
            }
        }

        #endregion

        #region Logging Management

        private void EnableLoggingCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            bool isChecked = EnableLoggingCheckBox.IsChecked == true;
            
            try
            {
                // Save logging setting
                Settings.Default.EnableLogging = isChecked;
                Settings.Default.Save();
                
                // Log the change
                if (isChecked)
                {
                    Logger.Info("Logging has been enabled");
                }
                else
                {
                    Logger.Info("Logging has been disabled");
                }
                
                // Announce change to screen readers
                AutomationProperties.SetName(EnableLoggingCheckBox, 
                    $"Enable Logging Checkbox - Currently {(isChecked ? "enabled" : "disabled")}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update logging setting: {ex.Message}", 
                              "Logging Setting Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                
                // Revert checkbox state on error
                EnableLoggingCheckBox.IsChecked = !isChecked;
            }
        }

        private void OpenLogFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string logDirectory = Logger.GetLogDirectory();
                
                // Create directory if it doesn't exist
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                
                // Open the log folder in Windows Explorer
                System.Diagnostics.Process.Start("explorer.exe", logDirectory);
                
                Logger.Info("Log folder opened from settings");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open log folder: {ex.Message}", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Error("Failed to open log folder", ex);
            }
        }

        #endregion
    }
}
