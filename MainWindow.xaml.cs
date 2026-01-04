using System.Text;
using System.Windows;
using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;

namespace IPPopper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IPService _ipService;
        private List<IPInfo> _currentIPs = new();

        public MainWindow(IPService ipService)
        {
            InitializeComponent();
            _ipService = ipService;

            // Set header with computer name
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
                _currentIPs = await _ipService.GetAllIPAddressesAsync();

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
                ShowTemporaryMessage("Primary IP copied to clipboard!");
            }
            else
            {
                MessageBox.Show("No primary IP address found.", "Information",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
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
            sb.AppendLine("IP Address Information");
            sb.AppendLine("========================");
            sb.AppendLine();

            // Group by type
            List<IPInfo> localIPs = _currentIPs.Where(ip => ip.Type.Contains("LAN") || ip.Type.Contains("Private") || ip.Type.Contains("Link-Local")).ToList();
            List<IPInfo> externalIPs = _currentIPs.Where(ip => ip.Type.Contains("External") || ip.Type.Contains("Public")).ToList();

            if (localIPs.Count > 0)
            {
                sb.AppendLine("Local/LAN IP Addresses:");
                sb.AppendLine("------------------------");
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
            ShowTemporaryMessage("All IP information copied to clipboard!");
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadIPAddresses();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void ShowTemporaryMessage(string message)
        {
            string originalTitle = Title;
            Title = message;
            await Task.Delay(2000);
            Title = originalTitle;
        }

        protected override void OnClosed(EventArgs e)
        {
            // Don't shutdown the application when this window closes
            base.OnClosed(e);
        }
    }
}