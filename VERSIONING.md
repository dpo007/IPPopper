# IPPopper Version Management

This document explains how to update the version number for IPPopper releases.

## Current Version

**Version: 1.42.10.0**

## How to Update the Version

To release a new version of IPPopper, you need to update the version in **two files**:

### 1. Main Application Version

**File:** `IPPopper.csproj`

Update these properties in the `<PropertyGroup>`:

```xml
<Version>1.42.10</Version>
<AssemblyVersion>1.42.10.0</AssemblyVersion>
<FileVersion>1.42.10.0</FileVersion>
<InformationalVersion>1.42.10</InformationalVersion>
```

### 2. Installer Version

**File:** `IPPopper Installer/IPPopper Installer.wixproj`

Update the version constant in the `<PropertyGroup>`:

```xml
<DefineConstants>IPPopperVersion=1.42.10.0</DefineConstants>
```

## Version Format

IPPopper uses [Semantic Versioning](https://semver.org/) with the format:

```
Major.Minor.Patch.Build
```

For example: `1.42.10.0`

- **Major** (1) - Breaking changes or major new features
- **Minor** (42) - New features, backward compatible
- **Patch** (10) - Bug fixes, backward compatible
- **Build** (0) - Build number (usually 0 for releases)

## Build Process

After updating the version numbers:

1. Build the solution in **Release** configuration:
   ```bash
   dotnet build -c Release
   ```

2. Build the installer project to generate the MSI:
   - Build the `IPPopper Installer` project in Visual Studio, or
   - Use the WiX build tools

3. The installer MSI will be named: `IPPopper-1.42.10.0.msi` (or similar)

## Version Sync

The installer uses the variable `$(var.IPPopperVersion)` in `Package.wxs` to reference the version defined in the `.wixproj` file. This helps keep versions synchronized, but you still need to manually update both files.

**Important:** Always ensure both version numbers match to avoid confusion!

## Quick Checklist

- [ ] Update `<Version>`, `<AssemblyVersion>`, and `<FileVersion>` in `IPPopper.csproj`
- [ ] Update `IPPopperVersion` in `IPPopper Installer/IPPopper Installer.wixproj`
- [ ] Verify both versions match
- [ ] Build solution in Release mode
- [ ] Build installer project
- [ ] Test the installer
- [ ] Commit changes with version number in commit message
- [ ] Tag the release in Git: `git tag v1.42.10.0`
