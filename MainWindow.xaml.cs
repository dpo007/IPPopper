using System.Text;
using System.Windows;
using System.Windows.Input;
using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;

namespace IPPopper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<IPInfo> _currentIPs = [];

        public MainWindow()
        {
            InitializeComponent();

            // Set title and header with computer name
            Title = $"IP Popper - IP Address Information - {Environment.MachineName}";
            HeaderTextBlock.Text = $"Current IP Addresses on {Environment.MachineName}";

            LoadIPAddresses();
        }

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
            NotificationHelper.Show("IPPopper", $"Copied computer name: {machineName}", Notifications.Wpf.Core.NotificationType.Information);
        }

        private void CopyNameButton_Click(object sender, RoutedEventArgs e)
        {
            CopyComputerNameToClipboard();
        }

        private void CopyAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIPs.Count == 0)
            {
                MessageBox.Show("No IP addresses to copy.", "Information",
                               MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            StringBuilder sb = new StringBuilder();
            string header = $"IP Address Information - {Environment.MachineName}";
            sb.AppendLine(header);
            sb.AppendLine(new string('=', header.Length));
            sb.AppendLine();

            // Group by type
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

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadIPAddresses();
        }

        private void HideButton_Click(object sender, RoutedEventArgs e)
        {
            // Check for Ctrl+Alt+Click to toggle theme
            if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) == (ModifierKeys.Control | ModifierKeys.Alt))
            {
                ThemeManager.ToggleTheme();
                return;
            }
            Hide();
        }

        protected override void OnClosed(EventArgs e)
        {
            // Don't shutdown the application when this window closes
            base.OnClosed(e);
        }
    }
}