# IPPopper - IP Address System Tray Utility

## Features
- Runs in background with system tray icon
- Shows primary IP in tooltip when hovering over tray icon
- Right-click context menu with "Show" and "Quit" options
- Double-click system tray icon to show window
- Display window shows all IP addresses (local and external)
- Primary LAN IP is highlighted in green and marked as primary
- Copy buttons to copy primary IP or all IP information
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

### Framework-Dependent Deployment
The Release build creates a **framework-dependent deployment**:

- **IPPopper.exe** (~311KB - main executable)
- **IPPopper.dll** (~335KB - application logic)  
- **IPPopper.runtimeconfig.json** (runtime configuration)
- **IPPopper.deps.json** (dependencies)
- **Total size**: ~650KB

### Advantages
✅ **Tiny footprint** - Less than 1MB vs 175MB+ self-contained  
✅ **No native DLLs** - Clean deployment with just .NET files  
✅ **Automatic updates** - .NET runtime updates handled by system  
✅ **Better performance** - Shared runtime optimizations  
✅ **Professional standard** - How enterprise applications are distributed  

## Usage
1. **Prerequisites**: Ensure .NET 9 Runtime is installed
2. **Launch**: Run IPPopper.exe from the Release build folder
3. **System Tray**: Look for the IPPopper icon near the clock
4. **View IPs**: 
   - **Hover** over icon to see primary IP address
   - **Double-click** icon to open the IP information window
   - **Right-click** for context menu ("Show" and "Quit" options)
5. **IP Window Features**:
   - View all detected IP addresses (local and external)
   - Primary IP highlighted in green and marked as "Primary"
   - **Copy Primary IP** - Copy just the primary IP to clipboard
   - **Copy All IPs** - Copy formatted IP list to clipboard
   - **Refresh** - Update IP addresses
   - **Close** - Close window (app continues in tray)

## Technical Details
- **Framework**: .NET 9 with WPF for UI
- **System Tray**: Windows Forms NotifyIcon component
- **Icon**: Fully embedded in executable (no separate icon file needed)
- **IP Detection**: 
  - Primary IP detected via route to 8.8.8.8 (Google DNS)
  - External IP from multiple public services with fallback
  - Network classification: Private/LAN, Link-Local, Public/Routable
- **Deployment**: Framework-dependent for optimal size and performance