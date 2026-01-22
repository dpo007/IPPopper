using System.Text;
using System.Windows;
using System.Windows.Input;
using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;

namespace IPPopper
{
    /// <summary>
    /// Main window for displaying detailed IP address information.
    /// Shows local, LAN, VPN, and external IP addresses with interface details.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Cached list of IP addresses currently displayed in the UI.
        /// </summary>
        private List<IPInfo> _currentIPs = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// Sets up the window title with computer name and loads IP addresses.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Set title and header with computer name
            Title = $"IP Popper - IP Address Information - {Environment.MachineName}";
            HeaderTextBlock.Text = $"Current IP Addresses on {Environment.MachineName}";

            LoadIPAddresses();
        }

        /// <summary>
        /// Asynchronously loads all IP addresses and updates the UI data grid and primary IP display.
        /// </summary>
        private async void LoadIPAddresses()
        {
            try
            {
                // Show loading state
                IPDataGrid.ItemsSource = null;
                PrimaryIPTextBlock.Text = "Loading...";

                // Get IP addresses
                _currentIPs = await IPService.GetAllIPAddressesAsync();

                // Update UI
                IPDataGrid.ItemsSource = _currentIPs;

                // Update primary IP display
                IPInfo? primaryIP = _currentIPs.FirstOrDefault(ip => ip.IsPrimary);
                PrimaryIPTextBlock.Text = primaryIP?.Address ?? "No primary IP found";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading IP addresses: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                PrimaryIPTextBlock.Text = "Error loading IPs";
            }
        }

        /// <summary>
        /// Handles the Copy Primary IP button click event.
        /// Copies the primary IP address to the clipboard and shows a notification.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">Routed event arguments.</param>
        private void CopyPrimaryButton_Click(object sender, RoutedEventArgs e)
        {
            IPInfo? primaryIP = _currentIPs.FirstOrDefault(ip => ip.IsPrimary);
            if (primaryIP != null)
            {
                Clipboard.SetText(primaryIP.Address);
                NotificationHelper.ShowCopiedPrimaryIP();
            }
            else
            {
                MessageBox.Show("No primary IP address found.", "Information",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Copies the computer name to the clipboard and displays a notification.
        /// </summary>
        private static void CopyComputerNameToClipboard()
        {
            string machineName = Environment.MachineName;
            if (string.IsNullOrWhiteSpace(machineName))
            {
                MessageBox.Show("Computer name is not available.", "Information",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Clipboard.SetText(machineName);
            NotificationHelper.ShowCopiedComputerName();
        }

        /// <summary>
        /// Handles the Copy Computer Name button click event.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">Routed event arguments.</param>
        private void CopyNameButton_Click(object sender, RoutedEventArgs e)
        {
            CopyComputerNameToClipboard();
        }

        /// <summary>
        /// Handles the Copy All button click event.
        /// Generates a formatted report of all IP addresses and copies it to the clipboard.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">Routed event arguments.</param>
        private void CopyAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIPs.Count == 0)
            {
                MessageBox.Show("No IP addresses to copy.", "Information",
                               MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Build formatted report with header
            StringBuilder sb = new StringBuilder();
            string header = $"IP Address Information - {Environment.MachineName}";
            sb.AppendLine(header);
            sb.AppendLine(new string('=', header.Length));
            sb.AppendLine();

            // Group addresses by type for better readability
            List<IPInfo> localIPs = _currentIPs
                .Where(ip => ip.Type.StartsWith("Local", StringComparison.OrdinalIgnoreCase))
                .ToList();

            List<IPInfo> externalIPs = _currentIPs
                .Where(ip => ip.Type.StartsWith("External", StringComparison.OrdinalIgnoreCase) ||
                            ip.Type.StartsWith("Public", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (localIPs.Count > 0)
            {
                sb.AppendLine("Local/LAN IP Addresses:");
                sb.AppendLine("-----------------------");
                foreach (IPInfo? ip in localIPs)
                {
                    sb.AppendLine($"{ip.Address} - MAC: {ip.MacAddress} ({ip.Type}) - {ip.InterfaceName}{(ip.IsPrimary ? " [PRIMARY]" : "")}");
                }
                sb.AppendLine();
            }

            if (externalIPs.Count > 0)
            {
                sb.AppendLine("External/Public IP Addresses:");
                sb.AppendLine("-----------------------------");
                foreach (IPInfo? ip in externalIPs)
                {
                    sb.AppendLine($"{ip.Address} ({ip.Type})");
                }
            }

            Clipboard.SetText(sb.ToString());
            NotificationHelper.ShowCopiedAllIPs();
        }

        /// <summary>
        /// Handles the Refresh button click event.
        /// Reloads all IP address information from the system.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">Routed event arguments.</param>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadIPAddresses();
        }

        /// <summary>
        /// Handles the Hide button click event.
        /// Hides the window (keeps application running in system tray).
        /// Special behavior: Ctrl+Alt+Click toggles the application theme.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">Routed event arguments.</param>
        private void HideButton_Click(object sender, RoutedEventArgs e)
        {
            // Hidden feature: Ctrl+Alt+Click toggles theme
            if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) == (ModifierKeys.Control | ModifierKeys.Alt))
            {
                ThemeManager.ToggleTheme();
                return;
            }
            Hide();
        }

        /// <summary>
        /// Called when the window is closed.
        /// Overridden to prevent application shutdown when the main window closes.
        /// Application continues to run in the system tray.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected override void OnClosed(EventArgs e)
        {
            // Preserve tray-only mode - closing window doesn't exit application
            base.OnClosed(e);
        }
    }
}