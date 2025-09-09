# IPPopper Release Package Creator
# This script builds the release version and creates a distributable ZIP file

Write-Host "IPPopper Release Package Creator" -ForegroundColor Green
Write-Host "===================================" -ForegroundColor Green

# Clean and build release
Write-Host "`nCleaning previous builds..." -ForegroundColor Yellow
dotnet clean -c Release --verbosity quiet

Write-Host "Building Release version..." -ForegroundColor Yellow
$buildResult = dotnet build -c Release --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green

# Define paths
$releaseDir = "bin\Release\net9.0-windows"
$tempDir = "temp_release"
$zipName = "IPPopper-Release.zip"

# Clean up any previous temp directory and zip file
if (Test-Path $tempDir) {
    Remove-Item $tempDir -Recurse -Force
}
if (Test-Path $zipName) {
    Remove-Item $zipName -Force
}

# Create temp directory structure
Write-Host "`nPreparing release files..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path "$tempDir\IPPopperInstallFiles" -Force | Out-Null

# Copy only the essential files
$filesToCopy = @(
    "IPPopper.exe",
    "IPPopper.dll", 
    "IPPopper.runtimeconfig.json",
    "IPPopper.deps.json"
)

foreach ($file in $filesToCopy) {
    $sourcePath = Join-Path $releaseDir $file
    $destPath = Join-Path "$tempDir\IPPopperInstallFiles" $file
    
    if (Test-Path $sourcePath) {
        Copy-Item $sourcePath $destPath
        Write-Host "  ✓ Copied $file" -ForegroundColor Gray
    } else {
        Write-Host "  ✗ Warning: $file not found" -ForegroundColor Red
    }
}

# Create installation script
Write-Host "  ✓ Creating installation script..." -ForegroundColor Gray
$installScript = @'
# IPPopper Installation Script
# This script installs IPPopper to C:\IPPopper and configures it to start with Windows for all users

param(
    [string]$TargetPath = "C:\IPPopper"
)

Write-Host "IPPopper Installation Script" -ForegroundColor Green
Write-Host "============================" -ForegroundColor Green

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host ""
    Write-Host "ERROR: Administrator privileges required!" -ForegroundColor Red
    Write-Host ""
    Write-Host "This installation script needs to run as Administrator because it:" -ForegroundColor Yellow
    Write-Host "  • Installs files to C:\IPPopper (or specified system directory)" -ForegroundColor Gray
    Write-Host "  • Configures startup for ALL USERS via registry (HKLM)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "To run as Administrator:" -ForegroundColor Cyan
    Write-Host "  1. Right-click PowerShell" -ForegroundColor Gray
    Write-Host "  2. Select 'Run as Administrator'" -ForegroundColor Gray
    Write-Host "  3. Navigate to this directory and run: .\Install-IPPopper.ps1" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Installation aborted." -ForegroundColor Red
    exit 1
}

Write-Host "Running with Administrator privileges [OK]" -ForegroundColor Green

# Validate .NET 9 Runtime
Write-Host "`nChecking .NET 9 Runtime..." -ForegroundColor Yellow
try {
    $dotnetInfo = dotnet --list-runtimes | Where-Object { $_ -match "Microsoft\.WindowsDesktop\.App 9\." }
    if ($dotnetInfo) {
        Write-Host "[OK] .NET 9 Runtime found" -ForegroundColor Green
    } else {
        Write-Host "WARNING: .NET 9 Runtime not detected!" -ForegroundColor Red
        Write-Host "IPPopper requires .NET 9 Runtime to run." -ForegroundColor Yellow
        Write-Host "Download from: https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Cyan
        Write-Host ""
        $continue = Read-Host "Continue installation anyway? (y/N)"
        if ($continue -ne "y" -and $continue -ne "Y") {
            Write-Host "Installation cancelled." -ForegroundColor Yellow
            exit 0
        }
    }
} catch {
    Write-Host "Could not check .NET Runtime (dotnet command not found)" -ForegroundColor Yellow
}

# Create target directory
Write-Host "`nInstalling to: $TargetPath" -ForegroundColor Yellow
if (Test-Path $TargetPath) {
    Write-Host "Target directory exists, removing old files..." -ForegroundColor Gray
    Remove-Item "$TargetPath\*" -Force -ErrorAction SilentlyContinue
} else {
    New-Item -ItemType Directory -Path $TargetPath -Force | Out-Null
}

# Copy files
Write-Host "Copying IPPopper files..." -ForegroundColor Yellow
$sourceFiles = Get-ChildItem "IPPopperInstallFiles" -File
foreach ($file in $sourceFiles) {
    Copy-Item $file.FullName $TargetPath -Force
    Write-Host "  [OK] $($file.Name)" -ForegroundColor Gray
}

# Configure startup for all users
Write-Host "`nConfiguring startup for all users..." -ForegroundColor Yellow
$exePath = Join-Path $TargetPath "IPPopper.exe"
$regPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"

try {
    Set-ItemProperty -Path $regPath -Name "IPPopper" -Value "`"$exePath`"" -Force
    Write-Host "[OK] Added to system startup (all users)" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Failed to configure startup: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "You may need to manually add IPPopper to startup" -ForegroundColor Yellow
}

# Create Start Menu shortcut for all users
Write-Host "`nCreating Start Menu shortcut..." -ForegroundColor Yellow
try {
    $startMenuPath = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::CommonStartMenu)
    $shortcutDir = Join-Path $startMenuPath "Programs"
    $shortcutPath = Join-Path $shortcutDir "IPPopper.lnk"
    
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $exePath
    $shortcut.WorkingDirectory = $TargetPath
    $shortcut.Description = "IPPopper - IP Address System Tray Utility"
    $shortcut.Save()
    
    Write-Host "[OK] Start Menu shortcut created for all users" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Failed to create Start Menu shortcut: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "You can manually create a shortcut if needed" -ForegroundColor Yellow
}

Write-Host "`nInstallation completed!" -ForegroundColor Green
Write-Host "IPPopper has been installed to: $TargetPath" -ForegroundColor White
Write-Host "It will start automatically for all users on next login." -ForegroundColor Gray
Write-Host ""
Write-Host "To start IPPopper now, run: `"$exePath`"" -ForegroundColor Cyan

$startNow = Read-Host "`nStart IPPopper now? (Y/n)"
if ($startNow -ne "n" -and $startNow -ne "N") {
    Write-Host "Starting IPPopper..." -ForegroundColor Yellow
    Start-Process $exePath
    Write-Host "IPPopper started! Look for the icon in your system tray." -ForegroundColor Green
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "           UNINSTALL INSTRUCTIONS        " -ForegroundColor Cyan  
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "To completely remove IPPopper from this system:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. STOP the application:" -ForegroundColor White
Write-Host "   • Right-click IPPopper system tray icon and select 'Quit'" -ForegroundColor Gray
Write-Host "   • Or use Task Manager to end IPPopper.exe process" -ForegroundColor Gray
Write-Host ""
Write-Host "2. DELETE installation folder:" -ForegroundColor White
Write-Host "   Remove-Item `"$TargetPath`" -Recurse -Force" -ForegroundColor Gray
Write-Host ""
Write-Host "3. REMOVE startup registry entry:" -ForegroundColor White
Write-Host "   Remove-ItemProperty -Path `"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`" -Name `"IPPopper`"" -ForegroundColor Gray
Write-Host ""
Write-Host "4. DELETE Start Menu shortcut:" -ForegroundColor White
$startMenuPath = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::CommonStartMenu)
$shortcutPath = Join-Path (Join-Path $startMenuPath "Programs") "IPPopper.lnk"
Write-Host "   Remove-Item `"$shortcutPath`"" -ForegroundColor Gray
Write-Host ""
Write-Host "NOTE: You must run these commands as Administrator" -ForegroundColor Yellow
Write-Host ""
'@

$installScriptPath = Join-Path "$tempDir" "Install-IPPopper.ps1"
Set-Content -Path $installScriptPath -Value $installScript -Encoding UTF8

# Create the ZIP file
Write-Host "`nCreating ZIP package..." -ForegroundColor Yellow
Compress-Archive -Path "$tempDir\*" -DestinationPath $zipName -Force

# Clean up temp directory
Remove-Item $tempDir -Recurse -Force

# Get file size for display
$zipSize = [math]::Round((Get-Item $zipName).Length / 1KB, 1)

Write-Host "`nPackage created successfully!" -ForegroundColor Green
Write-Host "File: $zipName ($zipSize KB)" -ForegroundColor White
Write-Host "`nExtraction info:" -ForegroundColor Yellow
Write-Host "  • When extracted, creates 'IPPopperInstallFiles' folder and 'Install-IPPopper.ps1'" -ForegroundColor Gray
Write-Host "  • Run Install-IPPopper.ps1 as Administrator to install" -ForegroundColor Gray
Write-Host "  • Installs to C:\IPPopper by default (customizable)" -ForegroundColor Gray
Write-Host "  • Configures automatic startup for all users" -ForegroundColor Gray
Write-Host "  • Requires .NET 9 Runtime on target machine" -ForegroundColor Gray

Write-Host "`nDone!" -ForegroundColor Green