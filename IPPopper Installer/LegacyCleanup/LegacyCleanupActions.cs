using Microsoft.Win32;
using System.Diagnostics;

namespace IPPopper.LegacyCleanup;

public static class LegacyCleanupActions
{
    [CustomAction]
    public static ActionResult CleanupLegacyInstall(Session session)
    {
        if (session is null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        try
        {
            session.Log("[IPPopper] Legacy cleanup starting.");

            StopRunningProcesses(session);
            RemoveLegacyFolder(session);
            RemoveLegacyStartupRegistryValue(session);
            RemoveLegacyStartMenuShortcut(session);

            session.Log("[IPPopper] Legacy cleanup completed.");
            return ActionResult.Success;
        }
        catch (Exception ex)
        {
            session.Log("[IPPopper] Legacy cleanup failed: {0}", ex);
            return ActionResult.Failure;
        }
    }

    private static void StopRunningProcesses(Session session)
    {
        session.Log("[IPPopper] Checking for running IPPopper processes...");

        Process[] processes;
        try
        {
            processes = Process.GetProcessesByName("IPPopper");
        }
        catch (Exception ex)
        {
            session.Log("[IPPopper] Failed to enumerate processes: {0}", ex.Message);
            return;
        }

        if (processes.Length == 0)
        {
            session.Log("[IPPopper] No running IPPopper processes found.");
            return;
        }

        session.Log("[IPPopper] Found {0} IPPopper process(es). Attempting to stop...", processes.Length);

        foreach (Process process in processes)
        {
            try
            {
                if (process.HasExited)
                {
                    continue;
                }

                process.Kill();
            }
            catch (Exception ex)
            {
                session.Log("[IPPopper] Failed to kill process (Id={0}): {1}", process.Id, ex.Message);
            }
        }

        // Give Windows a moment to release file locks.
        Thread.Sleep(1500);
    }

    private static void RemoveLegacyFolder(Session session)
    {
        const string legacyPath = @"C:\IPPopper";

        if (!Directory.Exists(legacyPath))
        {
            session.Log("[IPPopper] Legacy folder not present: {0}", legacyPath);
            return;
        }

        session.Log("[IPPopper] Removing legacy folder: {0}", legacyPath);

        // Retry a couple times in case explorer/AV still holds locks.
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                Directory.Delete(legacyPath, recursive: true);
                session.Log("[IPPopper] Removed legacy folder.");
                return;
            }
            catch (Exception ex) when (attempt < 3)
            {
                session.Log("[IPPopper] Attempt {0} to remove legacy folder failed: {1}", attempt, ex.Message);
                Thread.Sleep(1000);
            }
        }

        // Final attempt: let exception bubble to fail install.
        Directory.Delete(legacyPath, recursive: true);
        session.Log("[IPPopper] Removed legacy folder.");
    }

    private static void RemoveLegacyStartupRegistryValue(Session session)
    {
        const string runKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        const string valueName = "IPPopper";

        session.Log("[IPPopper] Removing legacy startup registry value HKLM\\{0}\\{1}", runKeyPath, valueName);

        try
        {
            using RegistryKey? runKey = Registry.LocalMachine.OpenSubKey(runKeyPath, writable: true);
            if (runKey is null)
            {
                session.Log("[IPPopper] Run key not found.");
                return;
            }

            if (Array.IndexOf(runKey.GetValueNames(), valueName) < 0)
            {
                session.Log("[IPPopper] Startup value not present.");
                return;
            }

            runKey.DeleteValue(valueName, throwOnMissingValue: false);
            session.Log("[IPPopper] Startup value removed.");
        }
        catch (Exception ex)
        {
            session.Log("[IPPopper] Failed to remove startup value: {0}", ex.Message);
            throw;
        }
    }

    private static void RemoveLegacyStartMenuShortcut(Session session)
    {
        string commonStartMenu = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
        string shortcutPath = Path.Combine(commonStartMenu, "Programs", "IPPopper.lnk");

        if (!File.Exists(shortcutPath))
        {
            session.Log("[IPPopper] Legacy Start Menu shortcut not present: {0}", shortcutPath);
            return;
        }

        session.Log("[IPPopper] Removing legacy Start Menu shortcut: {0}", shortcutPath);

        try
        {
            File.Delete(shortcutPath);
            session.Log("[IPPopper] Legacy Start Menu shortcut removed.");
        }
        catch (Exception ex)
        {
            session.Log("[IPPopper] Failed to remove legacy Start Menu shortcut: {0}", ex.Message);
            throw;
        }
    }
}
