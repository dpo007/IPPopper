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
    [string]$TargetPath = "C:\IPPopper",
    [switch]$SkipUninstall,
    [switch]$UninstallOnly
)

Write-Host "IPPopper Installation Script" -ForegroundColor Green
Write-Host "============================" -ForegroundColor Green

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host
    Write-Host "ERROR: Administrator privileges required!" -ForegroundColor Red
    Write-Host
    Write-Host "This installation script needs to run as Administrator because it:" -ForegroundColor Yellow
    Write-Host "  • Installs files to C:\IPPopper (or specified system directory)" -ForegroundColor Gray
    Write-Host "  • Configures startup for ALL USERS via registry (HKLM)" -ForegroundColor Gray
    Write-Host
    Write-Host "To run as Administrator:" -ForegroundColor Cyan
    Write-Host "  1. Right-click PowerShell" -ForegroundColor Gray
    Write-Host "  2. Select 'Run as Administrator'" -ForegroundColor Gray
    Write-Host "  3. Navigate to this directory and run: .\Install-IPPopper.ps1" -ForegroundColor Gray
    Write-Host
    Write-Host "Available switches:" -ForegroundColor Cyan
    Write-Host "  -SkipUninstall    : Skip automatic uninstall of existing version" -ForegroundColor Gray
    Write-Host "  -UninstallOnly    : Only uninstall existing version and exit" -ForegroundColor Gray
    Write-Host
    Write-Host "Installation aborted." -ForegroundColor Red
    exit 1
}

Write-Host "Running with Administrator privileges [OK]" -ForegroundColor Green

# Uninstall Function
function Uninstall-IPPopper {
    Write-Host "`nChecking for existing IPPopper installation..." -ForegroundColor Yellow

    $regPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"
    $startMenuPath = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::CommonStartMenu)
    $shortcutPath = Join-Path (Join-Path $startMenuPath "Programs") "IPPopper.lnk"
    $defaultPath = "C:\IPPopper"
    $installPath = $null
    $foundExisting = $false

    # Step 1: Check registry for startup entry
    try {
        $regValue = Get-ItemProperty -Path $regPath -Name "IPPopper" -ErrorAction SilentlyContinue
        if ($regValue -and $regValue.IPPopper) {
            # Extract path from registry value (remove quotes if present)
            $exePath = $regValue.IPPopper -replace '^"(.*)"$', '$1'
            $installPath = Split-Path $exePath -Parent
            Write-Host "[FOUND] Registry startup entry pointing to: $installPath" -ForegroundColor Yellow
            $foundExisting = $true

            # Remove registry entry
            try {
                Remove-ItemProperty -Path $regPath -Name "IPPopper" -Force
                Write-Host "[REMOVED] Registry startup entry" -ForegroundColor Green
            } catch {
                Write-Host "[ERROR] Failed to remove registry entry: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    } catch {
        Write-Host "[INFO] No registry startup entry found" -ForegroundColor Gray
    }

    # Step 2: If no registry entry, check Start Menu shortcut
    if (-not $foundExisting -and (Test-Path $shortcutPath)) {
        try {
            $shell = New-Object -ComObject WScript.Shell
            $shortcut = $shell.CreateShortcut($shortcutPath)
            $installPath = $shortcut.WorkingDirectory
            if ($installPath) {
                Write-Host "[FOUND] Start Menu shortcut pointing to: $installPath" -ForegroundColor Yellow
                $foundExisting = $true
            }
        } catch {
            Write-Host "[WARNING] Could not read Start Menu shortcut: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }

    # Step 3: Remove Start Menu shortcut if it exists
    if (Test-Path $shortcutPath) {
        try {
            Remove-Item $shortcutPath -Force
            Write-Host "[REMOVED] Start Menu shortcut" -ForegroundColor Green
        } catch {
            Write-Host "[ERROR] Failed to remove Start Menu shortcut: $($_.Exception.Message)" -ForegroundColor Red
        }
    }

    # Step 4: Try to stop running IPPopper processes
    Write-Host "Checking for running IPPopper processes..." -ForegroundColor Yellow
    $processes = Get-Process -Name "IPPopper" -ErrorAction SilentlyContinue
    if ($processes) {
        Write-Host "Stopping $($processes.Count) IPPopper process(es)..." -ForegroundColor Yellow
        try {
            $processes | Stop-Process -Force
            Start-Sleep -Seconds 2  # Give processes time to fully terminate
            Write-Host "[STOPPED] IPPopper processes" -ForegroundColor Green
        } catch {
            Write-Host "[ERROR] Failed to stop some IPPopper processes: $($_.Exception.Message)" -ForegroundColor Red
            Write-Host "You may need to manually stop IPPopper before installation" -ForegroundColor Yellow
        }
    }

    # Step 5: Remove installation folder
    # Priority: discovered path -> default path
    $pathsToCheck = @()
    if ($installPath -and (Test-Path $installPath)) {
        $pathsToCheck += $installPath
    }
    if ($installPath -ne $defaultPath -and (Test-Path $defaultPath)) {
        $pathsToCheck += $defaultPath
    }

    foreach ($pathToRemove in $pathsToCheck) {
        if (Test-Path $pathToRemove) {
            Write-Host "Removing installation folder: $pathToRemove" -ForegroundColor Yellow
            try {
                # Try to remove individual files first (better error handling)
                $files = Get-ChildItem $pathToRemove -File -ErrorAction SilentlyContinue
                foreach ($file in $files) {
                    try {
                        Remove-Item $file.FullName -Force
                    } catch {
                        Write-Host "[WARNING] Could not remove file: $($file.Name)" -ForegroundColor Yellow
                    }
                }

                # Then remove the directory
                Remove-Item $pathToRemove -Recurse -Force -ErrorAction SilentlyContinue

                if (-not (Test-Path $pathToRemove)) {
                    Write-Host "[REMOVED] Installation folder: $pathToRemove" -ForegroundColor Green
                    $foundExisting = $true
                } else {
                    Write-Host "[WARNING] Installation folder may not be completely removed: $pathToRemove" -ForegroundColor Yellow
                }
            } catch {
                Write-Host "[ERROR] Failed to remove installation folder: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }

    if ($foundExisting) {
        Write-Host "`n[SUCCESS] Previous IPPopper installation has been uninstalled" -ForegroundColor Green
    } else {
        Write-Host "`n[INFO] No existing IPPopper installation found" -ForegroundColor Gray
    }

    return $foundExisting
}

# Run uninstall if not skipped
if (-not $SkipUninstall) {
    $null = Uninstall-IPPopper
}

# Exit if UninstallOnly switch is used
if ($UninstallOnly) {
    Write-Host "`nUninstall completed. Exiting as requested by -UninstallOnly switch." -ForegroundColor Cyan
    exit 0
}

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
        Write-Host
        $continue = Read-Host "Continue installation anyway? (y/N)"
        if ($continue -ne "y" -and $continue -ne "Y") {
            Write-Host "Installation cancelled." -ForegroundColor Yellow
            exit 0
        }
    }
} catch {
    Write-Host "Could not check .NET Runtime (dotnet command not found)" -ForegroundColor Yellow
}

# Copy files (and handle target directory cleanup here only — no duplicate cleanup block)
Write-Host "`nInstalling to: $TargetPath" -ForegroundColor Yellow
Write-Host "Copying IPPopper files..." -ForegroundColor Yellow

# Resolve source relative to the script location
$sourceDir = Join-Path $PSScriptRoot "IPPopperInstallFiles"

if (-not (Test-Path $sourceDir)) {
    Write-Host "[ERROR] Source folder not found: $sourceDir" -ForegroundColor Red
    Write-Host "Make sure 'IPPopperInstallFiles' is in the same folder as Install-IPPopper.ps1" -ForegroundColor Yellow
    exit 1
}

# Enumerate contents (fail fast if empty)
$itemsToCopy = Get-ChildItem -Path $sourceDir -Recurse -Force -ErrorAction Stop
if (-not $itemsToCopy -or $itemsToCopy.Count -eq 0) {
    Write-Host "[ERROR] Source folder exists but contains no files: $sourceDir" -ForegroundColor Red
    exit 1
}

Write-Host "Source: $sourceDir" -ForegroundColor Gray
Write-Host "Target: $TargetPath" -ForegroundColor Gray

# Ensure target exists and is clean
if (Test-Path $TargetPath) {
    Write-Host "Target directory exists, removing old files..." -ForegroundColor Gray
    Remove-Item (Join-Path $TargetPath "*") -Recurse -Force -ErrorAction SilentlyContinue
} else {
    New-Item -ItemType Directory -Path $TargetPath -Force | Out-Null
}

# Copy everything (files + subfolders)
try {
    Copy-Item -Path (Join-Path $sourceDir "*") -Destination $TargetPath -Recurse -Force -ErrorAction Stop
    Write-Host "[OK] Copied $($itemsToCopy.Count) item(s) into $TargetPath" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Copy failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Configure startup for all users
Write-Host "`nConfiguring startup for all users..." -ForegroundColor Yellow
$exePath = Join-Path $TargetPath "IPPopper.exe"
$regPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"

if (-not (Test-Path $exePath)) {
    Write-Host "[ERROR] Expected executable not found after copy: $exePath" -ForegroundColor Red
    Write-Host "Check that IPPopper.exe exists inside: $sourceDir" -ForegroundColor Yellow
    exit 1
}

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
Write-Host
Write-Host "If you need to start it manually (as the signed-in user), run:" -ForegroundColor Cyan
Write-Host "  `"$exePath`"" -ForegroundColor Cyan

Write-Host
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "           MANUAL UNINSTALL INFO         " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host
Write-Host "This script includes automatic uninstall functionality." -ForegroundColor Yellow
Write-Host "To uninstall IPPopper, run this script with:" -ForegroundColor White
Write-Host "  .\Install-IPPopper.ps1 -UninstallOnly" -ForegroundColor Cyan
Write-Host
Write-Host "Or to manually remove IPPopper:" -ForegroundColor Yellow
Write-Host
Write-Host "1. STOP the application:" -ForegroundColor White
Write-Host "   • Right-click IPPopper system tray icon and select 'Quit'" -ForegroundColor Gray
Write-Host "   • Or use Task Manager to end IPPopper.exe process" -ForegroundColor Gray
Write-Host
Write-Host "2. DELETE installation folder:" -ForegroundColor White
Write-Host "   Remove-Item `"$TargetPath`" -Recurse -Force" -ForegroundColor Gray
Write-Host
Write-Host "3. REMOVE startup registry entry:" -ForegroundColor White
Write-Host "   Remove-ItemProperty -Path `"HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`" -Name `"IPPopper`"" -ForegroundColor Gray
Write-Host
Write-Host "4. DELETE Start Menu shortcut:" -ForegroundColor White
$startMenuPath = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::CommonStartMenu)
$shortcutPath = Join-Path (Join-Path $startMenuPath "Programs") "IPPopper.lnk"
Write-Host "   Remove-Item `"$shortcutPath`"" -ForegroundColor Gray
Write-Host
Write-Host "NOTE: You must run these commands as Administrator" -ForegroundColor Yellow
Write-Host
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
Write-Host "  • Available switches: -SkipUninstall, -UninstallOnly, -RunAfterInstall" -ForegroundColor Gray

Write-Host "`nDone!" -ForegroundColor Green

Write-Host "Press any key to exit..."
$x = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
exit 0