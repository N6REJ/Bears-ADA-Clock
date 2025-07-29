# Visual Studio Setup Project Instructions

## Installing the Visual Studio Installer Projects Extension

### Step 1: Install the Extension
1. Open Visual Studio 2022
2. Go to **Extensions** → **Manage Extensions**
3. Search for "Microsoft Visual Studio Installer Projects"
4. Install the extension by Microsoft
5. Restart Visual Studio when prompted

### Step 2: Add Setup Project to Solution
1. Right-click on the solution in Solution Explorer
2. Select **Add** → **New Project**
3. Search for "Setup Project" 
4. Select **Setup Project** template
5. Name it "BearsAdaClockSetup"
6. Click **Create**

### Step 3: Configure the Setup Project

#### Add Application Files:
1. Right-click on **Application Folder** in the setup project
2. Select **Add** → **Project Output**
3. Choose "BearsAdaClock" project
4. Select "Primary Output"
5. Click **OK**

#### Add Custom Font:
1. Right-click on **Application Folder**
2. Select **Add** ��� **Folder**
3. Name it "Fonts"
4. Right-click on the Fonts folder
5. Select **Add** → **File**
6. Browse to and select `Fonts/RobotoMono-Regular.ttf`

#### Configure Project Properties:
1. Select the setup project in Solution Explorer
2. In Properties window, set:
   - **Author**: Troy Hall (N6REJ)
   - **Description**: Accessible Desktop Clock
   - **Manufacturer**: N6REJ
   - **ManufacturerUrl**: https://hallhome.us/software
   - **ProductName**: Bears ADA Clock
   - **SupportUrl**: https://github.com/N6REJ/Bears-ADA-Clock/issues
   - **Title**: Bears ADA Clock Setup
   - **Version**: 1.0.0

#### Create Desktop Shortcut:
1. Right-click on **User's Desktop** folder
2. Select **Create Shortcut to User's Desktop**
3. In the dialog, select **Application Folder**
4. Select "Primary output from BearsAdaClock"
5. Name the shortcut "Bears ADA Clock"

#### Create Start Menu Shortcut:
1. Right-click on **User's Programs Menu**
2. Select **Add** → **Folder**
3. Name it "Bears ADA Clock"
4. Right-click on the new folder
5. Select **Create Shortcut**
6. Select **Application Folder** → "Primary output from BearsAdaClock"
7. Name it "Bears ADA Clock"

#### Add Uninstall Shortcut:
1. In the "Bears ADA Clock" Start Menu folder
2. Right-click and select **Create Shortcut**
3. In Target, enter: `msiexec.exe /x {ProductCode}`
4. Name it "Uninstall Bears ADA Clock"

### Step 4: Configure Prerequisites (.NET 6.0)
1. Right-click on the setup project
2. Select **Properties**
3. Go to **Prerequisites**
4. Check "Create setup program to install prerequisite components"
5. Check "Microsoft .NET Desktop Runtime 6.0"
6. Select "Download prerequisites from the component vendor's web site"

### Step 5: Build the MSI
1. Set solution configuration to **Release**
2. Right-click on the setup project
3. Select **Build**
4. The MSI will be created in the setup project's output folder

## Advantages of This Approach

### Professional Features:
- ✅ **True MSI format** (not self-extracting executable)
- ✅ **Automatic .NET detection** and installation prompts
- ✅ **Proper Windows integration** (Add/Remove Programs)
- ✅ **Upgrade handling** for future versions
- ✅ **Digital signing support** for trusted installations
- ✅ **Custom actions** if needed
- ✅ **Localization support**

### Size Benefits:
- **Framework-dependent**: ~2-5MB (requires .NET 6.0)
- **Self-contained**: Can be configured if needed
- **Professional compression** built into MSI format

### Maintenance:
- ✅ **Visual designer** - no scripting required
- ✅ **Microsoft supported** - regular updates
- ✅ **Industry standard** - familiar to IT departments
- ✅ **Documentation** - extensive Microsoft docs available

## Alternative: WiX Toolset (Advanced)

For even more control, consider WiX Toolset v4:
- XML-based installer definition
- Complete customization control
- Command-line buildable
- Used by Microsoft for their own products

## Recommendation

**Use Visual Studio Setup Projects** for this project because:
1. **Simplicity**: GUI-based, easy to maintain
2. **Professional**: Creates proper MSI files
3. **Integration**: Works seamlessly with Visual Studio
4. **Support**: Microsoft-backed solution
5. **Accessibility**: Better for your target audience (easier installation)

The custom PowerShell scripts we created were a good interim solution, but Visual Studio Setup Projects will provide a much more professional and maintainable installer.
