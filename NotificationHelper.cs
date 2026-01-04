using Notifications.Wpf.Core;
using System.Windows;
using WpfApp = System.Windows.Application;

namespace IPPopper;

internal static class NotificationHelper
{
    private static readonly NotificationManager _manager = new();
    private static Window? _hostWindow;

    public static void ShowCopied()
    {
        Show("IPPopper", "Copied", NotificationType.Success);
    }

    public static void Show(string title, string message, NotificationType type)
    {
        if (WpfApp.Current == null)
        {
            return;
        }

        WpfApp.Current.Dispatcher.BeginInvoke(async () =>
        {
            await ShowInternalAsync(title, message, type);
        });
    }

    private static async Task ShowInternalAsync(string title, string message, NotificationType type)
    {
        try
        {
            EnsureHostWindow();

            await _manager.ShowAsync(
                new NotificationContent
                {
                    Title = title,
                    Message = message,
                    Type = type
                });
        }
        catch (ArgumentException ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[NotificationHelper] ArgumentException: {ex}");
#endif
        }
        catch (InvalidOperationException ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[NotificationHelper] InvalidOperationException: {ex}");
#endif
        }
    }

    private static void EnsureHostWindow()
    {
        if (_hostWindow != null)
        {
            return;
        }

        // Notifications.Wpf.Core expects an owner Window that has been shown.
        // In a tray-only app there may be no MainWindow, so we create an off-screen host.
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
