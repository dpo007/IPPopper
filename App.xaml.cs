using System.IO;
using System.Reflection;
using System.Windows;
using Application = System.Windows.Application;

namespace IPPopper
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private NotifyIcon? _notifyIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Apply system theme before showing any windows
            ThemeManager.ApplySystemTheme();

            // Create system tray icon
            CreateNotifyIcon();

            // Don't show main window on startup
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        private void CreateNotifyIcon()
        {
            _notifyIcon = new NotifyIcon();

            // Load embedded icon
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();

                // The embedded resource name follows the pattern: Namespace.FileName
                string iconResourceName = "IPPopper.IPPopper.ico";

                using Stream? iconStream = assembly.GetManifestResourceStream(iconResourceName);
                if (iconStream != null)
                {
                    _notifyIcon.Icon = new Icon(iconStream);
                }
                else
                {
                    // If the exact name doesn't work, try to find it dynamically
                    string[] resourceNames = assembly.GetManifestResourceNames();
                    string? iconResource = resourceNames.FirstOrDefault(name => name.EndsWith("IPPopper.ico"));

                    if (iconResource != null)
                    {
                        using Stream? stream = assembly.GetManifestResourceStream(iconResource);
                        if (stream != null)
                        {
                            _notifyIcon.Icon = new Icon(stream);
                        }
                        else
                        {
                            _notifyIcon.Icon = SystemIcons.Information;
                        }
                    }
                    else
                    {
                        _notifyIcon.Icon = SystemIcons.Information;
                    }
                }
            }
            catch (Exception ex)
            {
                _notifyIcon.Icon = SystemIcons.Information;
            }

            _notifyIcon.Text = "IPPopper - Loading...";
            _notifyIcon.Visible = true;

            // Add double-click event handler
            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            // Create context menu
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show", null, ShowWindow_Click);
            contextMenu.Items.Add("Copy Name", null, CopyName_Click);
            contextMenu.Items.Add("Copy IP", null, CopyIP_Click);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Quit", null, Quit_Click);
            _notifyIcon.ContextMenuStrip = contextMenu;

            // Update tooltip with primary IP
            UpdateTooltip();
        }

        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            ShowWindow_Click(sender, e);
        }

        private async void UpdateTooltip()
        {
            if (_notifyIcon != null)
            {
                string primaryIP = await IPService.GetPrimaryLocalIPAsync();
                _notifyIcon.Text = $"IPPopper ({Environment.MachineName}) - {primaryIP}";
            }
        }

        private void ShowWindow_Click(object? sender, EventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            mainWindow.Activate();
        }

        private async void CopyIP_Click(object? sender, EventArgs e)
        {
            string primaryIP = await IPService.GetPrimaryLocalIPAsync();
            if (string.IsNullOrWhiteSpace(primaryIP))
            {
                return;
            }

            System.Windows.Clipboard.SetText(primaryIP);

            NotificationHelper.ShowCopiedPrimaryIP();
        }

        private static void CopyName_Click(object? sender, EventArgs e)
        {
            string machineName = Environment.MachineName;
            if (string.IsNullOrWhiteSpace(machineName))
            {
                return;
            }

            System.Windows.Clipboard.SetText(machineName);
            NotificationHelper.Show("IPPopper", $"Copied computer name: {machineName}", Notifications.Wpf.Core.NotificationType.Information);
        }

        private void Quit_Click(object? sender, EventArgs e)
        {
            _notifyIcon?.Dispose();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon?.Dispose();
            base.OnExit(e);
        }
    }
}