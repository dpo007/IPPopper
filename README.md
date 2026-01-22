# IPPopper - IP Address System Tray Utility

## Features
- WPF desktop UI for viewing local network IP information
- Displays all detected IP addresses in a grid with columns for:
  - IP address
  - MAC address
  - Type
  - Interface name
  - Primary indicator
- Highlights the primary IP row
- Shows the current primary IP in a dedicated panel
- Buttons to:
  - Copy computer name
  - Copy primary IP
  - Copy all IPs
  - Refresh
  - Hide the window (keeps the app running)

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

### Getting Started
After installation, IPPopper launches automatically when users log in. You can also start it manually from the Start Menu or by running `C:\IPPopper\IPPopper.exe`.

### Main Window
The main window displays a data grid showing all network interfaces detected on your computer:

- **IP Address** column shows IPv4 addresses from each interface
- **MAC Address** column displays the physical hardware address for each adapter
- **Type** column categorizes the address (e.g., Private/LAN, Link-Local, Public/Routable)
- **Interface** column shows the network adapter name
- **Primary** column indicates which IP is your primary local address

The primary IP row is highlighted and displayed prominently in a separate panel above the button row.

### Available Actions
- **Copy Name** - Copies your computer name to the clipboard
- **Copy Primary IP** - Copies only the primary local IP address
- **Copy All IPs** - Copies a formatted report of all network information including MAC addresses
- **Refresh** - Re-scans network interfaces and updates the display
- **Hide** - Minimizes the window to continue running in the background

## Uninstallation
To completely remove IPPopper (run as Administrator):

### Self-uninstall (recommended)
Run:

```powershell
C:\IPPopper\IPPopper.exe -uninstall
```

This will remove:
- Startup entry (all users)
- Start Menu shortcut (all users)
- The installation folder

### Manual uninstall
If needed (run as Administrator):

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
- **Network Detection**:
  - **IP Addresses**: Enumerated from local network interfaces
  - **MAC Addresses**: Physical addresses from network adapters
- **Deployment**: Framework-dependent for optimal size and performance
- **Installation**: PowerShell-based with system-wide configuration