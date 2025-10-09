![GitHub release (latest by date)](https://img.shields.io/github/v/release/N6REJ/Bears-ADA-Clock)
![GitHub top language](https://img.shields.io/github/languages/top/N6REJ/Bears-ADA-Clock)
![GitHub All Releases](https://img.shields.io/github/downloads/N6REJ/Bears-ADA-Clock/total)
![License: GPL v3](https://img.shields.io/badge/License-MIT-blue.svg)

# Bears ADA Clock

A desktop clock specifically designed for those with accessibility issues, featuring full screen reader support and comprehensive customization options.

## Features

### Accessibility Features
- **Full Screen Reader Support**: All UI elements are properly labeled and announced
- **Live Regions**: Time and date updates are announced to screen readers
- **Keyboard Navigation**: Complete keyboard accessibility throughout the application
- **High Contrast Support**: Compatible with Windows high contrast themes
- **Customizable Font Sizes**: Large, adjustable text for users with visual impairments

### Clock Features
- **Real-time Display**: Shows current time in HH:MM or HH:MM:SS format (configurable)
- **Date Display**: Configurable date format with multiple options
- **Custom Font**: Uses Roboto Mono TTF font for clear, readable digits
- **Draggable**: Click and drag to move the clock anywhere on your desktop
- **Right-Click Context Menu**: Access settings and exit options anywhere on the clock
- **Responsive Window**: Automatically sizes to content - no wasted space or oversized click areas
- **Normal Window Behavior**: Integrates naturally with other windows (not always on top)
- **Transparent Background**: Completely transparent with no visible borders or frames
- **Auto-Start**: Automatically starts with Windows (configurable)

### Customization Options
- **Font Size Control**: 
  - Separate sliders for time and date (24px to 128px in 8px increments)
  - Default sizes: 56px for time, 32px for date (optimized for accessibility)
  - Real-time preview of changes
- **Time Display Options**:
  - Toggle seconds display on/off (default: off)
  - Shows HH:MM when seconds are off, HH:MM:SS when on
- **Color Options**: 
  - Primary colors available for both time and date
  - Includes Black, White, Red, Blue, Green, Yellow, Orange, Purple, Gray
- **Date Formats**:
  - YYYY MMM DD (default)
  - MMM DD, YYYY
  - DD MMM YYYY
- **Display Modes**:
  - Time and Date (default)
  - Date Above Time
  - Time Only
  - Date Only
- **Startup Options**:
  - Start with Windows (enabled by default)
  - Automatic startup configuration
- **Responsive Window**: Automatically sizes to content for optimal right-click area
- **Positioning**: Default upper-right placement (64px from edges), maintains position after font changes

## System Requirements

- Windows 10 or later
- .NET 6.0 Runtime
- Visual Studio 2022 (for development)

## Building the Project

### For Development:

1. **Open in Visual Studio**:
   ```
   Open BearsAdaClock.sln in Visual Studio 2022
   ```

2. **Build the Solution**:
   ```
   Build > Build Solution (Ctrl+Shift+B)
   ```

3. **Run the Application**:
   ```
   Debug > Start Without Debugging (Ctrl+F5)
   ```

### For Distribution:

#### Professional MSI Installer (Recommended)
1. **Install Visual Studio Installer Projects Extension**:
   - Open Visual Studio 2022
   - Go to Extensions → Manage Extensions
   - Search for "Microsoft Visual Studio Installer Projects"
   - Install and restart Visual Studio

2. **Create Setup Project**:
   - See `SETUP_INSTRUCTIONS.md` for detailed steps
   - Add Setup Project to solution
   - Configure application files, shortcuts, and prerequisites
   - Build to generate professional MSI installer

3. **Distribute**:
   - Share the generated `BearsAdaClockSetup.msi` file
   - Professional Windows Installer with .NET prerequisite handling
   - Proper Add/Remove Programs integration
   - Upgrade support for future versions

## Installation

### For End Users:

1. **Download** the `BearsAdaClockInstaller.msi` file
2. **Double-click** the MSI file to start installation
3. **Follow** the installation wizard prompts
4. **Choose** whether to create a desktop shortcut
5. **Launch** from Start Menu or Desktop shortcut

### Installation Features:
- **Automatic .NET Detection**: Checks for .NET 6.0 Desktop Runtime
- **Smart Installation**: Downloads and installs .NET 6.0 if needed (~55MB)
- **Small Download**: Framework-dependent installer is only ~0.25MB
- **Start Menu Integration**: Creates program shortcuts
- **Desktop Shortcut**: Optional desktop icon
- **Add/Remove Programs**: Proper Windows integration for uninstallation
- **Clean Uninstall**: Complete removal of all files and registry entries
- **Shared Runtime**: Benefits from shared .NET runtime (faster startup, security updates)

## Logs

Where to find the log file(s):
- Primary: %LocalAppData%\BearsAdaClock\logs\ada-clock.log
- Fallback (if LocalAppData is unavailable): %TEMP%\ada-clock.log

Quick access:
- Right-click the clock and choose "Show Logs Folder" to open the exact folder the app is using.

Notes:
- On startup, the app writes an entry like "Logger path in use: C:\\Users\\<You>\\AppData\\Local\\BearsAdaClock\\logs\\ada-clock.log" near the top of the log. This confirms the active path after any reinstall or Windows restart.

What gets logged:
- App lifecycle: startup, activation, exit
- Window: construction, loaded, content rendered, positioning decisions
- Settings: load/save, upgrade, apply
- Context menu: open state, Start with Windows toggle
- Registry operations for autostart (Run and StartupApproved keys)
- Unhandled exceptions from AppDomain, Dispatcher, and TaskScheduler

Log rotation:
- The log file automatically rotates at ~1 MB into timestamped archives, keeping the current file small.

Troubleshooting startup visibility:
- If Windows shows the app in Startup Apps but no UI appears, check the log around "App.OnStartup" and "MainWindow constructor" entries for errors or immediate shutdown.

## Usage

### Basic Operation
1. **Launch**: Run the application - the clock will appear in the upper-right corner
2. **Move**: Click and drag the clock to reposition it anywhere on your desktop
3. **Settings**: Right-click on the clock to open the context menu
4. **Exit**: Right-click and select "Exit" to close the application

### Settings Menu
The settings window provides comprehensive customization:

1. **Time Display Settings**:
   - Adjust digit size with the slider (24-128px)
   - Select color from the dropdown menu
   - Toggle seconds display on/off with checkbox
   - See real-time preview of changes

2. **Date Display Settings**:
   - Adjust date size independently from time (24-128px)
   - Choose date color (can be different from time color)
   - Select preferred date format

3. **Display Mode Settings**:
   - Choose what to display: Time and Date, Date Above Time, Time Only, or Date Only
   - Perfect for different use cases and screen space requirements

4. **Startup Options**:
   - Toggle "Start with Windows" to automatically launch the clock at login
   - Enabled by default for convenience
   - Automatically configures Windows startup registry entries
   - You can also toggle this from the clock's right-click context menu (Start with Windows)

5. **Preview Section**:
   - See exactly how your settings will look
   - Updates in real-time as you make changes
   - Shows actual font sizes and colors

6. **Apply Changes**:
   - Click "Apply" to save and implement your settings
   - Window automatically resizes to fit new content
   - Position is maintained relative to screen edges
   - Click "Close" to exit without saving changes

### Accessibility Usage
- **Screen Readers**: All elements are properly announced
- **Keyboard Navigation**: Use Tab to navigate, Enter to activate
- **Live Updates**: Time changes are announced every minute
- **Settings Navigation**: Full keyboard support in settings window

## File Structure

```
Bears-ADA-Clock/
├── BearsAdaClock.sln          # Visual Studio solution file
├── BearsAdaClock.csproj       # Project file
├── App.xaml                   # Application resources
├── App.xaml.cs               # Application code-behind
├── MainWindow.xaml           # Main clock window UI
├── MainWindow.xaml.cs        # Main window logic
├── SettingsWindow.xaml       # Settings window UI
├── SettingsWindow.xaml.cs    # Settings window logic
├── Fonts/
│   └── RobotoMono-Regular.ttf # Custom TTF font
└── README.md                 # This file
```

## Technical Details

### Architecture
- **Framework**: WPF (.NET 6.0)
- **UI Pattern**: MVVM-inspired with code-behind
- **Accessibility**: Full AutomationProperties implementation
- **Font Handling**: Embedded TTF font resource

### Key Components
- **MainWindow**: Primary clock display with drag functionality
- **SettingsWindow**: Comprehensive settings interface
- **Timer**: DispatcherTimer for real-time updates
- **Context Menu**: Right-click settings access

### Accessibility Implementation
- AutomationProperties.Name on all interactive elements
- LiveSetting="Polite" for time/date updates
- Proper labeling relationships (Label.Target)
- Keyboard navigation support
- Screen reader announcements for setting changes

## Development Notes

### Adding New Features
1. Maintain accessibility standards for any new UI elements
2. Update AutomationProperties for screen reader support
3. Test with Windows Narrator and other screen readers
4. Follow WPF best practices for performance

### Font Customization
To use a different font:
1. Replace `RobotoMono-Regular.ttf` in the Fonts folder
2. Update the FontFamily reference in `App.xaml`
3. Rebuild the project

## Author

**Troy Hall (N6REJ)**
- Website: https://hallhome.us/software
- Repository: https://github.com/N6REJ/Bears-ADA-Clock
- Support: https://github.com/N6REJ/Bears-ADA-Clock/issues
- Manufacturer: N6REJ
- Copyright: © 2025 Troy Hall (N6REJ)

## License

This project is open source under the MIT License. See LICENSE file for details.

## Contributing

Contributions are welcome, especially those that improve accessibility features. Please ensure all new features maintain full screen reader compatibility.

For support and updates, visit: https://hallhome.us/software

## Exact log path explained

- %LocalAppData% is a Windows environment variable that expands to: C:\Users\<YourUser>\AppData\Local
- Therefore, the full log file path is: C:\Users\<YourUser>\AppData\Local\BearsAdaClock\logs\ada-clock.log
- Replace <YourUser> with your Windows account name. Example: C:\Users\Troy\AppData\Local\BearsAdaClock\logs\ada-clock.log
- Quick ways to open the folder:
  - Press Win+R, paste: %LocalAppData%\BearsAdaClock\logs and press Enter
  - In the app, right-click the clock and choose: Show Logs Folder
- On startup the app logs a confirmation line near the top: "Logger path in use: C:\\Users\\<YourUser>\\AppData\\Local\\BearsAdaClock\\logs\\ada-clock.log"
  - The same resolved path is exposed programmatically as: Logger.LogFilePath



## Installer notes for .NET 6 and dependency warnings

- When building the MSI via the Visual Studio Installer Projects extension, you may see warnings like:
  - WARNING: Unable to find dependency 'SYSTEM.CONFIGURATION.CONFIGURATIONMANAGER' ...
  - WARNING: Unable to find dependency 'MICROSOFT.WIN32.REGISTRY' ...
  - WARNING: Unable to find dependency 'SYSTEM.DIAGNOSTICS.PROCESS' ...
  - WARNING: Unable to find dependency 'SYSTEM.RUNTIME' ...
- These are expected for .NET 6/7 apps packaged as framework-dependent. Those assemblies are part of the shared .NET runtime and do not ship as separate files next to your app. The installer still packages the correct .NET 6 artifacts (BearsAdaClock.exe, BearsAdaClock.dll, BearsAdaClock.deps.json, BearsAdaClock.runtimeconfig.json, fonts, and assets). The MSI build will succeed and the app will run provided the .NET 6 Desktop Runtime is installed on the target machine.

What we changed
- The Installer project now sources application files from the publish output folder: bin\Release\net6.0-windows\win-x64\publish. This is the recommended input for MSI packaging.
- The legacy .NET Framework 4.7.2 launch condition was removed to avoid mixed prerequisites for a .NET 6 application.

Eliminating the warnings (optional)
- Best option: publish self-contained so all dependencies live next to the EXE. Then build the Installer (it already points at the publish folder) and the dependency warnings should disappear.

How to publish self-contained (win-x64)
- Visual Studio: Right-click project → Publish → New profile → Folder → Target runtime: win-x64 → Deployment mode: Self-contained → Finish → Publish.
- CLI:
  - dotnet publish -c Release -r win-x64 --self-contained true
  - Output will be in bin\Release\net6.0-windows\win-x64\publish

Notes
- If you keep framework-dependent publish, the warnings may still appear, but they are benign; ensure .NET 6 Desktop Runtime is present on target machines.
