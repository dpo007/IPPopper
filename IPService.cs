using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace IPPopper
{
    public class IPInfo
    {
        public string Address { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public string InterfaceName { get; set; } = string.Empty;
    }

    public class IPService
    {
        public async Task<List<IPInfo>> GetAllIPAddressesAsync()
        {
            var ipList = new List<IPInfo>();
            
            // Get local IP addresses
            var localIPs = GetLocalIPAddresses();
            ipList.AddRange(localIPs);

            // Get external IP address
            try
            {
                var externalIP = await GetExternalIPAddressAsync();
                if (!string.IsNullOrEmpty(externalIP))
                {
                    ipList.Add(new IPInfo
                    {
                        Address = externalIP,
                        Type = "External/Public",
                        IsPrimary = false,
                        InterfaceName = "Internet"
                    });
                }
            }
            catch
            {
                // If we can't get external IP, continue without it
                ipList.Add(new IPInfo
                {
                    Address = "Unable to determine",
                    Type = "External/Public",
                    IsPrimary = false,
                    InterfaceName = "Internet"
                });
            }

            return ipList;
        }

        public async Task<string> GetPrimaryLocalIPAsync()
        {
            await Task.CompletedTask; // Fix async warning
            var localIPs = GetLocalIPAddresses();
            var primary = localIPs.FirstOrDefault(ip => ip.IsPrimary);
            return primary?.Address ?? "No IP found";
        }

        private List<IPInfo> GetLocalIPAddresses()
        {
            var ipList = new List<IPInfo>();
            var primaryIP = GetPrimaryIPAddress();

            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up &&
                    networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    foreach (var addressInfo in networkInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (addressInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            var address = addressInfo.Address.ToString();
                            var isPrimary = address == primaryIP;

                            ipList.Add(new IPInfo
                            {
                                Address = address,
                                Type = GetNetworkType(address),
                                IsPrimary = isPrimary,
                                InterfaceName = networkInterface.Name
                            });
                        }
                    }
                }
            }

            // Sort so primary IP is first
            return ipList.OrderByDescending(ip => ip.IsPrimary).ToList();
        }

        private string GetPrimaryIPAddress()
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                socket.Connect("8.8.8.8", 65530);
                var endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint?.Address.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private string GetNetworkType(string ipAddress)
        {
            if (ipAddress.StartsWith("192.168.") || 
                ipAddress.StartsWith("10.") ||
                (ipAddress.StartsWith("172.") && 
                 int.TryParse(ipAddress.Split('.')[1], out int second) && 
                 second >= 16 && second <= 31))
            {
                return "Private/LAN";
            }
            
            if (ipAddress.StartsWith("169.254."))
            {
                return "Link-Local";
            }

            return "Public/Routable";
        }

        private async Task<string> GetExternalIPAddressAsync()
        {
            var services = new[]
            {
                "https://api.ipify.org",
                "https://icanhazip.com",
                "https://ipecho.net/plain",
                "https://myexternalip.com/raw"
            };

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            foreach (var service in services)
            {
                try
                {
                    var response = await client.GetStringAsync(service);
                    var ip = response.Trim();
                    
                    // Validate it's a proper IP address
                    if (IPAddress.TryParse(ip, out _))
                    {
                        return ip;
                    }
                }
                catch
                {
                    // Try next service
                    continue;
                }
            }

            return string.Empty;
        }
    }
}