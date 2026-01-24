using Notifications.Wpf.Core;
using System.Runtime.Versioning;
using System.Windows;
using WpfApp = System.Windows.Application;

namespace IPPopper;

/// <summary>
/// Provides helper methods for displaying toast notifications using the Notifications.Wpf.Core library.
/// Manages notification display with automatic expiration and proper threading for WPF applications.
/// </summary>
internal static class NotificationHelper
{
    /// <summary>
    /// Notification manager instance for displaying toast notifications.
    /// </summary>
    private static NotificationManager? _manager;

    [SupportedOSPlatform("windows7.0")]
    private static NotificationManager Manager => _manager ??= new NotificationManager();

    /// <summary>
    /// Invisible host window required by the notification library for proper rendering.
    /// </summary>
    private static Window? _hostWindow;

    /// <summary>
    /// Displays a notification indicating the primary IP address was copied to the clipboard.
    /// </summary>
    [SupportedOSPlatform("windows7.0")]
    public static void ShowCopiedPrimaryIP()
    {
        Show("IPPopper", "Copied Primary IP to clipboard.", NotificationType.Information);
    }

    /// <summary>
    /// Displays a notification indicating the full IP address report was copied to the clipboard.
    /// </summary>
    [SupportedOSPlatform("windows7.0")]
    public static void ShowCopiedAllIPs()
    {
        Show("IPPopper", "Copied IP address report to clipboard.", NotificationType.Information);
    }

    /// <summary>
    /// Displays a notification indicating the computer name was copied to the clipboard.
    /// </summary>
    [SupportedOSPlatform("windows7.0")]
    public static void ShowCopiedComputerName()
    {
        Show("IPPopper", "Copied computer name to clipboard.", NotificationType.Information);
    }

    /// <summary>
    /// Displays a toast notification with the specified title, message, and type.
    /// Automatically marshals to the UI thread if needed.
    /// </summary>
    /// <param name="title">The notification title.</param>
    /// <param name="message">The notification message content.</param>
    /// <param name="type">The notification type (Information, Success, Warning, or Error).</param>
    [SupportedOSPlatform("windows7.0")]
    public static void Show(string title, string message, NotificationType type)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        if (WpfApp.Current == null)
        {
            return;
        }

        BeginInvokeOnUiThread(() => ShowInternalAsync(title, message, type));
    }

    [SupportedOSPlatform("windows7.0")]
    public static void ShowBlocking(string title, string message, NotificationType type, TimeSpan expirationTime)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        if (WpfApp.Current == null)
        {
            return;
        }

        EnsureHostWindow();

        WpfApp.Current.Dispatcher.Invoke(() =>
        {
            Manager.ShowAsync(
                new NotificationContent
                {
                    Title = title,
                    Message = message,
                    Type = type,
                },
                expirationTime: expirationTime).GetAwaiter().GetResult();
        });
    }

    [SupportedOSPlatform("windows7.0")]
    private static void BeginInvokeOnUiThread(Func<Task> action)
    {
        if (!OperatingSystem.IsWindows() || WpfApp.Current is null)
        {
            return;
        }

        WpfApp.Current.Dispatcher.BeginInvoke(async () => await action());
    }

    /// <summary>
    /// Internal async method that displays the notification on the UI thread.
    /// Handles exceptions gracefully to prevent notification failures from crashing the application.
    /// </summary>
    /// <param name="title">The notification title.</param>
    /// <param name="message">The notification message content.</param>
    /// <param name="type">The notification type.</param>
    [SupportedOSPlatform("windows7.0")]
    private static async Task ShowInternalAsync(string title, string message, NotificationType type)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            EnsureHostWindow();

            await Manager.ShowAsync(
                new NotificationContent
                {
                    Title = title,
                    Message = message,
                    Type = type,
                },
                expirationTime: TimeSpan.FromSeconds(3)
                );
        }
        catch (ArgumentException
#if DEBUG
            ex
#endif
            )
        {
#if DEBUG
            // Log notification argument errors in debug builds only
            System.Diagnostics.Debug.WriteLine($"[NotificationHelper] ArgumentException: {ex}");
#endif
        }
        catch (InvalidOperationException
#if DEBUG
            ex
#endif
            )
        {
#if DEBUG
            // Log notification operation errors in debug builds only
            System.Diagnostics.Debug.WriteLine($"[NotificationHelper] InvalidOperationException: {ex}");
#endif
        }
    }

    /// <summary>
    /// Ensures an invisible host window exists for the notification library.
    /// The Notifications.Wpf.Core library requires an owner window that has been shown.
    /// Creates an off-screen, transparent window for tray-only applications.
    /// </summary>
    [SupportedOSPlatform("windows7.0")]
    private static void EnsureHostWindow()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        if (_hostWindow != null)
        {
            return;
        }

        // Create invisible host window for notification library
        // Required by Notifications.Wpf.Core even when no main window exists
        _hostWindow = new Window
        {
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = System.Windows.Media.Brushes.Transparent,
            ShowInTaskbar = false,
            Width = 1,
            Height = 1,
            Left = -10000,
            Top = -10000,
            Topmost = true
        };

        _hostWindow.Show();
        _hostWindow.Hide();
    }
}
