using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using System.Windows;
using Application = System.Windows.Application;

namespace IPPopper
{
    /// <summary>
    /// Main application class for IPPopper.
    /// Manages system tray icon, theme application, and uninstall functionality.
    /// Runs without a main window by default, operating as a system tray application.
    /// </summary>
    [SupportedOSPlatform("windows6.1")]
    public partial class App : Application
    {
        /// <summary>
        /// System tray notification icon for the application.
        /// </summary>
        private NotifyIcon? _notifyIcon;

        private const string SingleInstanceMutexName = @"Local\IPPopper_SingleInstanceMutex";

        private Mutex? _singleInstanceMutex;

        /// <summary>
        /// Handles application startup, processes command-line arguments,
        /// applies system theme, and initializes the system tray icon.
        /// </summary>
        /// <param name="e">Startup event arguments containing command-line parameters.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            // Ensure WPF dispatcher is kept alive at startup even though the app is tray-only.
            // We later switch to OnExplicitShutdown after initialization.
            ShutdownMode = ShutdownMode.OnLastWindowClose;

            _singleInstanceMutex = new Mutex(initiallyOwned: true, name: SingleInstanceMutexName, createdNew: out bool createdNew);

            if (!createdNew)
            {
                Shutdown();
                return;
            }

            base.OnStartup(e);

            // Process -uninstall command-line switch for silent uninstallation
            if (e.Args.Length > 0 && (e.Args[0].Equals("-uninstall", StringComparison.OrdinalIgnoreCase) || e.Args[0].Equals("/uninstall", StringComparison.OrdinalIgnoreCase)))
            {
                Uninstaller.PerformSelfUninstall();
                Shutdown();
                return;
            }

            // Initialize theme before any UI is displayed
            ThemeManager.ApplySystemTheme();

            // Initialize system tray icon
            if (OperatingSystem.IsWindowsVersionAtLeast(7, 0))
            {
                CreateNotifyIcon();
            }

            // Run as tray-only application without main window
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
        }

        /// <summary>
        /// Creates and configures the system tray notification icon with context menu and event handlers.
        /// Loads the application icon from embedded resources.
        /// </summary>
        [SupportedOSPlatform("windows7.0")]
        private void CreateNotifyIcon()
        {
            _notifyIcon = new NotifyIcon();

            // Load icon from embedded resources with fallback to system icon
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();

                // Embedded resource naming: Namespace.FileName
                string iconResourceName = "IPPopper.IPPopper.ico";

                using Stream? iconStream = assembly.GetManifestResourceStream(iconResourceName);
                if (iconStream != null)
                {
                    _notifyIcon.Icon = new Icon(iconStream);
                }
                else
                {
                    // Fallback: dynamically search for icon resource by file extension
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
            catch (Exception)
            {
                // Resource load failure - use system default icon
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

        /// <summary>
        /// Handles double-click events on the system tray icon to show the main window.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            ShowWindow_Click(sender, e);
        }

        /// <summary>
        /// Updates the system tray icon tooltip with the computer name and primary IP address.
        /// </summary>
        private async void UpdateTooltip()
        {
            if (_notifyIcon != null)
            {
                string primaryIP = await IPService.GetPrimaryLocalIPAsync();
                _notifyIcon.Text = $"IPPopper ({Environment.MachineName}) - {primaryIP}";
            }
        }

        /// <summary>
        /// Shows and activates the main window when the user clicks "Show" in the context menu.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void ShowWindow_Click(object? sender, EventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            mainWindow.Activate();
        }

        /// <summary>
        /// Copies the primary IP address to the clipboard and shows a notification.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">Event arguments.</param>
        [SupportedOSPlatform("windows7.0")]
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

        /// <summary>
        /// Copies the computer name to the clipboard and shows a notification.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">Event arguments.</param>
        [SupportedOSPlatform("windows7.0")]
        private static void CopyName_Click(object? sender, EventArgs e)
        {
            string machineName = Environment.MachineName;
            if (string.IsNullOrWhiteSpace(machineName))
            {
                return;
            }

            System.Windows.Clipboard.SetText(machineName);
            NotificationHelper.ShowCopiedComputerName();
        }

        /// <summary>
        /// Disposes the system tray icon and shuts down the application.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void Quit_Click(object? sender, EventArgs e)
        {
            _notifyIcon?.Dispose();
            Shutdown();
        }

        /// <summary>
        /// Performs cleanup when the application exits, ensuring the system tray icon is disposed.
        /// </summary>
        /// <param name="e">Exit event arguments.</param>
        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon?.Dispose();

            if (_singleInstanceMutex != null)
            {
                try
                {
                    _singleInstanceMutex.ReleaseMutex();
                }
                catch (ApplicationException)
                {
                    // Not owned by this thread/process (or already released)
                }
                finally
                {
                    _singleInstanceMutex.Dispose();
                    _singleInstanceMutex = null;
                }
            }

            base.OnExit(e);
        }
    }
}