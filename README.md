# IPPopper - IP Address System Tray Utility

## Features
- Runs in background with system tray icon
- Shows primary IP in tooltip when hovering over tray icon
- Right-click context menu with "Show" and "Quit" options
- Double-click system tray icon to show window
- Display window shows all IP addresses (local and external) **with MAC addresses**
- Primary LAN IP is highlighted in green and marked as primary
- Copy buttons to copy primary IP or all IP information (including MAC addresses)
- Refresh button to update IP addresses
- Window opens centered on current display

## Requirements
- **.NET 9 Runtime** must be installed on the target machine
- Download from: https://dotnet.microsoft.com/download/dotnet/9.0

## Development & Testing

### Quick Start
Use the `BuildAndRun.bat` file - this will build the Release version and run it.

### Development/Debug Mode
1. Use the provided `RunIPPopper.bat` file, or
2. Run directly: `bin\Debug\net9.0-windows\IPPopper.exe`

### Release Build
To create the optimized Release executable:

```bash
dotnet build -c Release
```

**The output will be in:** `bin\Release\net9.0-windows\`

## Distribution

### Creating Release Package
Use the PowerShell script to create a distributable package:

```powershell
.\CreateRelease.ps1
```

This creates **`IPPopper-Release.zip`** containing:
- **IPPopperInstallFiles** folder with application files
- **Install-IPPopper.ps1** - Professional installation script

### Framework-Dependent Deployment
The Release build creates a **framework-dependent deployment**:

- **IPPopper.exe** (~311KB - main executable)
- **IPPopper.dll** (~335KB - application logic)  
- **IPPopper.runtimeconfig.json** (runtime configuration)
- **IPPopper.deps.json** (dependencies)
- **Total size**: ~650KB

### Installation
1. **Extract** the ZIP file to any location
2. **Run as Administrator**: `.\Install-IPPopper.ps1`
3. **Features**:
   - Installs to `C:\IPPopper` (customizable)
   - Configures automatic startup for all users
   - Creates Start Menu shortcut for all users
   - Validates .NET 9 Runtime installation
   - Professional installation experience

## Usage
1. **Install**: Extract ZIP and run `Install-IPPopper.ps1` as Administrator
2. **Automatic Start**: IPPopper starts automatically for all users on login
3. **Manual Launch**: Use Start Menu shortcut or run `C:\IPPopper\IPPopper.exe`
4. **System Tray**: Look for the IPPopper icon near the clock
5. **View Network Info**: 
   - **Hover** over icon to see primary IP address
   - **Double-click** icon to open the network information window
   - **Right-click** for context menu ("Show" and "Quit" options)
6. **Network Information Window**:
   - View all detected IP addresses (local and external)
   - **MAC addresses** displayed for each network adapter
   - Primary IP highlighted in green and marked as "Primary"
   - **Copy Primary IP** - Copy just the primary IP to clipboard
   - **Copy All Network Info** - Copy formatted list with IPs and MAC addresses
   - **Refresh** - Update network information
   - **Close** - Close window (app continues in tray)

## Uninstallation
To completely remove IPPopper (run as Administrator):

```powershell
# Stop the application
# Right-click system tray icon → Quit

# Remove installation folder
Remove-Item "C:\IPPopper" -Recurse -Force

# Remove startup registry entry
Remove-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" -Name "IPPopper"

# Remove Start Menu shortcut
Remove-Item "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\IPPopper.lnk"
```

## Technical Details
- **Framework**: .NET 9 with WPF for UI
- **System Tray**: Windows Forms NotifyIcon component
- **Icon**: Fully embedded in executable (no separate icon file needed)
- **Network Detection**: 
  - **IP Addresses**: Primary IP detected via route to 8.8.8.8 (Google DNS)
  - **MAC Addresses**: Physical addresses from network adapters (formatted as AA:BB:CC:DD:EE:FF)
  - **External IP**: From multiple public services with fallback
  - **Network Classification**: Private/LAN, Link-Local, Public/Routable
- **Deployment**: Framework-dependent for optimal size and performance
- **Installation**: PowerShell-based with system-wide configuration