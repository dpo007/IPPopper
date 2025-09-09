@echo off
echo Building IPPopper...
dotnet build -c Release

echo.
echo Running IPPopper...
cd /d "%~dp0"
start "" "bin\Release\net9.0-windows\IPPopper.exe"