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
        private IPService? _ipService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize IP service
            _ipService = new IPService();

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
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"Failed to load embedded icon: {ex.Message}");
                _notifyIcon.Icon = SystemIcons.Information;
            }

            _notifyIcon.Text = "IPPopper - Loading...";
            _notifyIcon.Visible = true;

            // Add double-click event handler
            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

            // Create context menu
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show", null, ShowWindow_Click);
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
            if (_ipService != null && _notifyIcon != null)
            {
                string primaryIP = await _ipService.GetPrimaryLocalIPAsync();
                _notifyIcon.Text = $"IPPopper - {primaryIP}";
            }
        }

        private void ShowWindow_Click(object? sender, EventArgs e)
        {
            MainWindow mainWindow = new MainWindow(_ipService!);
            mainWindow.Show();
            mainWindow.Activate();
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