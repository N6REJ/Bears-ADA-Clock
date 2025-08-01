using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Automation;
using Microsoft.Win32;
using BearsAdaClock.Properties;

namespace BearsAdaClock
{
    public partial class SettingsWindow : Window
    {
        private MainWindow mainWindow;

        public SettingsWindow(MainWindow parent)
        {
            InitializeComponent();
            mainWindow = parent;
            LoadCurrentSettings();
            UpdatePreview();
        }

        private void LoadCurrentSettings()
        {
            // Load current settings from main window
            DigitSizeSlider.Value = mainWindow.DigitSize;
            DateSizeSlider.Value = mainWindow.DateSize;
            ShowSecondsCheckBox.IsChecked = mainWindow.ShowSeconds;
            
            // Set color selections
            SetColorComboSelection(DigitColorCombo, mainWindow.DigitColor);
            SetColorComboSelection(DateColorCombo, mainWindow.DateColor);
            
            // Set date format selection
            SetDateFormatSelection(mainWindow.DateFormat);
            
            // Set display mode selection
            SetDisplayModeSelection(mainWindow.DisplayMode);
            
            // Load startup setting
            StartWithWindowsCheckBox.IsChecked = IsStartupEnabled();
            
            // Update value displays
            DigitSizeValue.Text = $"{mainWindow.DigitSize:F0}px";
            DateSizeValue.Text = $"{mainWindow.DateSize:F0}px";
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
                    return true;
                default:
                    return false; // Unknown or invalid mode
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

        private void DateFormatCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePreview();
            
            // Announce change to screen readers
            if (DateFormatCombo.SelectedItem is ComboBoxItem item)
            {
                AutomationProperties.SetName(DateFormatCombo, $"Date Format Selection - Current selection {item.Content}");
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
            if (PreviewTime == null || PreviewDate == null || PreviewPanel == null) return;

            // Update preview time with or without seconds
            bool showSeconds = ShowSecondsCheckBox.IsChecked == true;
            PreviewTime.Text = showSeconds ? "12:34:56" : "12:34";
            PreviewTime.FontSize = DigitSizeSlider.Value;
            if (DigitColorCombo.SelectedItem is ComboBoxItem digitColorItem)
            {
                PreviewTime.Foreground = GetBrushFromName(digitColorItem.Tag.ToString());
            }

            // Update preview date
            PreviewDate.FontSize = DateSizeSlider.Value;
            if (DateColorCombo.SelectedItem is ComboBoxItem dateColorItem)
            {
                PreviewDate.Foreground = GetBrushFromName(dateColorItem.Tag.ToString());
            }

            // Update date format
            if (DateFormatCombo.SelectedItem is ComboBoxItem formatItem)
            {
                var sampleDate = new DateTime(2024, 1, 15);
                PreviewDate.Text = FormatDate(sampleDate, formatItem.Tag.ToString());
            }

            // Update display mode - ensures at least one element is always visible
            if (DisplayModeCombo.SelectedItem is ComboBoxItem modeItem)
            {
                string mode = modeItem.Tag.ToString();
                PreviewPanel.Children.Clear();

                switch (mode)
                {
                    case "Both":
                        PreviewPanel.Children.Add(PreviewTime);
                        PreviewPanel.Children.Add(PreviewDate);
                        PreviewTime.Visibility = Visibility.Visible;
                        PreviewDate.Visibility = Visibility.Visible;
                        PreviewTime.Margin = new Thickness(0, 0, 0, 0);
                        PreviewDate.Margin = new Thickness(0, 5, 0, 0);
                        break;
                    case "DateAbove":
                        PreviewPanel.Children.Add(PreviewDate);
                        PreviewPanel.Children.Add(PreviewTime);
                        PreviewTime.Visibility = Visibility.Visible;
                        PreviewDate.Visibility = Visibility.Visible;
                        PreviewDate.Margin = new Thickness(0, 0, 0, 0);
                        PreviewTime.Margin = new Thickness(0, 5, 0, 0);
                        break;
                    case "TimeOnly":
                        PreviewPanel.Children.Add(PreviewTime);
                        PreviewTime.Visibility = Visibility.Visible;
                        PreviewDate.Visibility = Visibility.Collapsed;
                        PreviewTime.Margin = new Thickness(0, 0, 0, 0);
                        break;
                    case "DateOnly":
                        PreviewPanel.Children.Add(PreviewDate);
                        PreviewTime.Visibility = Visibility.Collapsed;
                        PreviewDate.Visibility = Visibility.Visible;
                        PreviewDate.Margin = new Thickness(0, 0, 0, 0);
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
            if (DisplayModeCombo.SelectedItem is ComboBoxItem validationModeItem)
            {
                string selectedMode = validationModeItem.Tag.ToString();
                
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
            mainWindow.ShowSeconds = ShowSecondsCheckBox.IsChecked == true;

            if (DigitColorCombo.SelectedItem is ComboBoxItem digitColorItem)
            {
                mainWindow.DigitColor = GetBrushFromName(digitColorItem.Tag.ToString());
            }

            if (DateColorCombo.SelectedItem is ComboBoxItem dateColorItem)
            {
                mainWindow.DateColor = GetBrushFromName(dateColorItem.Tag.ToString());
            }

            if (DateFormatCombo.SelectedItem is ComboBoxItem formatItem)
            {
                mainWindow.DateFormat = formatItem.Tag.ToString();
            }

            if (DisplayModeCombo.SelectedItem is ComboBoxItem applyModeItem)
            {
                mainWindow.DisplayMode = applyModeItem.Tag.ToString();
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
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    return key?.GetValue("BearsAdaClock") != null;
                }
            }
            catch
            {
                return false;
            }
        }

        private void EnableStartup()
        {
            try
            {
                string executablePath = GetExecutablePath();

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    key?.SetValue("BearsAdaClock", $"\"{executablePath}\"");
                }
                
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
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key?.GetValue("BearsAdaClock") != null)
                    {
                        key.DeleteValue("BearsAdaClock");
                    }
                }
                
                // Save startup setting
                Settings.Default.StartWithWindows = false;
                Settings.Default.Save();
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to remove from startup: {ex.Message}");
            }
        }

        private string GetExecutablePath()
        {
            try
            {
                // First try to get the main module file name
                string processPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                
                if (!string.IsNullOrEmpty(processPath) && File.Exists(processPath) && processPath.EndsWith(".exe"))
                {
                    return processPath;
                }
                
                // Fallback to assembly location
                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                
                // If it's a .dll (development/self-contained), try to find the .exe
                if (assemblyPath.EndsWith(".dll"))
                {
                    string exePath = Path.ChangeExtension(assemblyPath, ".exe");
                    if (File.Exists(exePath))
                    {
                        return exePath;
                    }
                    
                    // Try looking in the same directory for an exe with the assembly name
                    string directory = Path.GetDirectoryName(assemblyPath);
                    string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
                    string potentialExePath = Path.Combine(directory, assemblyName + ".exe");
                    if (File.Exists(potentialExePath))
                    {
                        return potentialExePath;
                    }
                    
                    // Look for BearsAdaClock.exe specifically
                    string clockExePath = Path.Combine(directory, "BearsAdaClock.exe");
                    if (File.Exists(clockExePath))
                    {
                        return clockExePath;
                    }
                }
                
                // Return the assembly path as last resort
                return assemblyPath;
            }
            catch
            {
                // Final fallback - use assembly location
                return Assembly.GetExecutingAssembly().Location;
            }
        }

        #endregion
    }
}
