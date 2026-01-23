using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;

namespace IPPopper;

/// <summary>
/// Handles the application self-uninstall process including registry cleanup,
/// file removal, and elevation when administrator privileges are required.
/// </summary>
internal static class Uninstaller
{
    /// <summary>
    /// Performs a complete uninstall of IPPopper, removing startup entries,
    /// shortcuts, and installation files. Prompts for elevation if needed.
    /// </summary>
    internal static void PerformSelfUninstall()
    {
        if (!OperatingSystem.IsWindows())
        {
            System.Windows.MessageBox.Show(
                "Uninstall is only supported on Windows.",
                "Uninstall IPPopper",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            return;
        }

        // Administrator privileges required for registry and Program Files access
        if (!IsRunningAsAdministrator())
        {
            System.Windows.MessageBoxResult result = System.Windows.MessageBox.Show(
                "IPPopper uninstall requires Administrator privileges.\n\nClick OK to relaunch as Administrator.",
                "IPPopper Uninstall",
                System.Windows.MessageBoxButton.OKCancel,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.OK)
            {
                TryRelaunchElevatedWithUninstallArg();
            }

            return;
        }

        System.Windows.MessageBoxResult confirm = System.Windows.MessageBox.Show(
            "This will uninstall IPPopper by removing:\n\n" +
            "• Startup entry (all users)\n" +
            "• Start Menu shortcut (all users)\n" +
            "• Installation folder\n\n" +
            "Continue?",
            "Uninstall IPPopper",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (confirm != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        // Determine installation directory from current process location
        string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
        if (string.IsNullOrWhiteSpace(exePath))
        {
            System.Windows.MessageBox.Show("Unable to determine executable path; uninstall aborted.", "Uninstall IPPopper", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }

        string? installDir = Path.GetDirectoryName(exePath);
        if (string.IsNullOrWhiteSpace(installDir))
        {
            System.Windows.MessageBox.Show("Unable to determine installation directory; uninstall aborted.", "Uninstall IPPopper", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }

        // Remove system integration points (continue on error to clean up as much as possible)
        try
        {
            RemoveStartupRegistryValue();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to remove startup registry entry: {ex.Message}", "Uninstall IPPopper", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }

        try
        {
            RemoveStartMenuShortcut();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to remove Start Menu shortcut: {ex.Message}", "Uninstall IPPopper", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }

        try
        {
            CreateAndLaunchCleanupScript(installDir);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to schedule file removal: {ex.Message}", "Uninstall IPPopper", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }

        System.Windows.MessageBox.Show("Uninstall has been scheduled. IPPopper will now exit.", "Uninstall IPPopper", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    /// <summary>
    /// Determines whether the current process is running with administrator privileges.
    /// </summary>
    /// <returns>True if running as administrator; otherwise, false.</returns>
    [SupportedOSPlatform("windows")]
    private static bool IsRunningAsAdministrator()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>
    /// Attempts to relaunch the application with elevated privileges and the -uninstall argument.
    /// Silently fails if the user cancels the UAC prompt.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static void TryRelaunchElevatedWithUninstallArg()
    {
        string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
        if (string.IsNullOrWhiteSpace(exePath))
        {
            return;
        }

        ProcessStartInfo psi = new()
        {
            FileName = exePath,
            Arguments = "-uninstall",
            UseShellExecute = true,
            Verb = "runas"
        };

        try
        {
            _ = Process.Start(psi);
        }
        catch
        {
            // User cancelled UAC prompt or elevation failed
        }
    }

    /// <summary>
    /// Removes the IPPopper startup entry from the system registry (HKLM\Run).
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static void RemoveStartupRegistryValue()
    {
        using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true);
        key?.DeleteValue("IPPopper", throwOnMissingValue: false);
    }

    /// <summary>
    /// Removes the IPPopper shortcut from the All Users Start Menu.
    /// </summary>
    [SupportedOSPlatform("windows")]
    private static void RemoveStartMenuShortcut()
    {
        string startMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
        string shortcutPath = Path.Combine(startMenuPath, "Programs", "IPPopper.lnk");

        if (File.Exists(shortcutPath))
        {
            File.Delete(shortcutPath);
        }
    }

    /// <summary>
    /// Creates and launches a PowerShell cleanup script that waits for the process to exit,
    /// then deletes the installation directory and itself.
    /// </summary>
    /// <param name="installDir">The installation directory to remove.</param>
    [SupportedOSPlatform("windows")]
    private static void CreateAndLaunchCleanupScript(string installDir)
    {
        string scriptPath = Path.Combine(Path.GetTempPath(), $"IPPopper_Uninstall_{Guid.NewGuid():N}.ps1");
        string scriptContent = BuildCleanupScriptContent(scriptPath, installDir);

        File.WriteAllText(scriptPath, scriptContent, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        ProcessStartInfo psi = new()
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        _ = Process.Start(psi);
    }

    /// <summary>
    /// Builds the PowerShell script content for delayed cleanup of installation files.
    /// Script waits for process exit, removes the installation directory, then self-deletes.
    /// </summary>
    /// <param name="scriptPath">Path where the cleanup script is saved.</param>
    /// <param name="installDir">Installation directory to remove.</param>
    /// <returns>PowerShell script content as a string.</returns>
    private static string BuildCleanupScriptContent(string scriptPath, string installDir)
    {
        // Escape single quotes for PowerShell string literals
        string escapedInstallDir = installDir.Replace("'", "''");
        string escapedScriptPath = scriptPath.Replace("'", "''");

        return $@"Start-Sleep -Seconds 3
Remove-Item -LiteralPath '{escapedInstallDir}' -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath '{escapedScriptPath}' -Force -ErrorAction SilentlyContinue
";
    }
}
