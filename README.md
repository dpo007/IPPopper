# IPPopper - IP Address System Tray Utility

<div align="center">
  <img src="Assets/IPPopperLogo.png" alt="IPPopper Logo" width="200"/>
</div>

## Features
- Windows system tray utility (WPF) that runs primarily from the tray
- Tray icon shows tooltip: `IPPopper (<COMPUTERNAME>) - <Primary IP>`
- Tray menu:
  - `Show` (opens the main window)
  - `Copy Name`
  - `Copy IP` (copies primary local IP)
  - `Quit`
- Main window shows detected IP information in a grid, including:
  - IP address
  - MAC address (for local interfaces)
  - Type (Local/LAN, Local/VPN, Local/Link-Local, Local/Public, External/Public)
  - Interface name
  - Primary indicator
- Displays the current primary local IP prominently
- Actions in the main window:
  - Copy computer name
  - Copy primary local IP
  - Copy a formatted report of all IPs
  - Refresh
  - Hide (keeps the app running in the system tray)

## Requirements
- Windows 7+ (tray icon support)
- **.NET 9 Desktop Runtime** must be installed on the target machine
- Download from: https://dotnet.microsoft.com/download/dotnet/9.0

## Development & Testing

### Development/Debug Mode
Run directly from Visual Studio or use:
```bash
dotnet run
```

### Release Build
To create the optimized Release executable:

```bash
dotnet build -c Release
```

**The output will be in:** `bin\Release\net9.0-windows\`

### Building the MSI Installer
The solution includes a WiX installer project. To build the MSI:
1. Install the WiX Toolset v4+ 
2. Build the solution in Release mode
3. Build the `IPPopper Installer` project

## Distribution

### Installer
IPPopper is distributed via an MSI installer.

### Framework-Dependent Deployment
The Release build creates a **framework-dependent deployment**:

- **IPPopper.exe** (main executable)
- **IPPopper.dll** (application logic)  
- **IPPopper.runtimeconfig.json** (runtime configuration)
- **IPPopper.deps.json** (dependencies)
- **Notifications.Wpf.Core.dll** (toast notification library)

### Installation (MSI)
1. Run the MSI installer.
2. Follow the setup wizard.

The MSI installs to:
- `C:\Program Files\IPPopper\`

The MSI configures:
- Start Menu shortcut: `Start Menu` → `IPPopper` → `IPPopper`
- Automatic startup for all users (via `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`)

## Usage

### Getting Started
After installation, IPPopper launches automatically when users log in. You can also start it manually from the Start Menu or by running `C:\Program Files\IPPopper\IPPopper.exe`.

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

### Special Features
- **Theme Toggle**: Ctrl+Alt+Click the Hide button to toggle between light and dark themes
- **Single Instance**: Only one copy of IPPopper can run at a time

## Uninstallation

Uninstall via Windows:
- Settings → Apps → Installed apps → `IPPopper` → Uninstall, or
- Control Panel → Programs and Features → `IPPopper` → Uninstall

## Technical Details
- **Framework**: .NET 9 with WPF for UI
- **Dependencies**: Notifications.Wpf.Core for toast notifications, System.Drawing.Common for icons
- **Network Detection**:
  - **IP Addresses**: Enumerated from local network interfaces
  - **MAC Addresses**: Physical addresses from network adapters
- **External/Public IP**: Retrieved by querying external services (e.g., `api.ipify.org`, `icanhazip.com`)
- **Deployment**: Framework-dependent (requires .NET 9 Desktop Runtime)
- **Installer**: WiX Toolset v4-based MSI installer

