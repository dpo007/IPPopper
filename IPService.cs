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
        public string MacAddress { get; set; } = string.Empty;
    }

    public class IPService
    {
        public static async Task<List<IPInfo>> GetAllIPAddressesAsync()
        {
            List<IPInfo> ipList = [];

            // Get local IP addresses
            List<IPInfo> localIPs = GetLocalIPAddresses();
            ipList.AddRange(localIPs);

            // Get external IP address
            try
            {
                string externalIP = await GetExternalIPAddressAsync();
                if (!string.IsNullOrEmpty(externalIP))
                {
                    ipList.Add(new IPInfo
                    {
                        Address = externalIP,
                        Type = "External/Public",
                        IsPrimary = false,
                        InterfaceName = "Internet",
                        MacAddress = "N/A"
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
                    InterfaceName = "Internet",
                    MacAddress = "N/A"
                });
            }

            return ipList;
        }

        public static async Task<string> GetPrimaryLocalIPAsync()
        {
            await Task.CompletedTask; // Fix async warning
            List<IPInfo> localIPs = GetLocalIPAddresses();
            IPInfo? primary = localIPs.FirstOrDefault(ip => ip.IsPrimary);
            return primary?.Address ?? "No IP found";
        }

        private static List<IPInfo> GetLocalIPAddresses()
        {
            List<IPInfo> ipList = [];
            string primaryIP = GetPrimaryIPAddress();

            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up &&
                    networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    string macAddress = GetMacAddress(networkInterface);

                    foreach (UnicastIPAddressInformation addressInfo in networkInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (addressInfo.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            string address = addressInfo.Address.ToString();
                            bool isPrimary = address == primaryIP;

                            ipList.Add(new IPInfo
                            {
                                Address = address,
                                Type = GetNetworkType(networkInterface, addressInfo),
                                IsPrimary = isPrimary,
                                InterfaceName = networkInterface.Name,
                                MacAddress = macAddress
                            });
                        }
                    }
                }
            }

            // Sort so primary IP is first
            return ipList.OrderByDescending(ip => ip.IsPrimary).ToList();
        }

        private static string GetMacAddress(NetworkInterface networkInterface)
        {
            try
            {
                byte[] macBytes = networkInterface.GetPhysicalAddress().GetAddressBytes();
                if (macBytes.Length == 0)
                {
                    return "N/A";
                }

                return string.Join(":", macBytes.Select(b => b.ToString("X2")));
            }
            catch
            {
                return "N/A";
            }
        }

        private static string GetPrimaryIPAddress()
        {
            try
            {
                using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint? endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint?.Address.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetNetworkType(NetworkInterface networkInterface, UnicastIPAddressInformation addressInfo)
        {
            IPAddress ipAddress = addressInfo.Address;

            if (IPAddress.IsLoopback(ipAddress))
            {
                return "Local/Loopback";
            }

            if (IsLinkLocal(ipAddress))
            {
                return "Local/Link-Local";
            }

            if (IsVpnInterface(networkInterface))
            {
                return "Local/VPN";
            }

            if (IsPrivateAddress(ipAddress) || IsCarrierGradeNat(ipAddress))
            {
                return "Local/LAN";
            }

            return "Local/Public";
        }

        private static bool IsPrivateAddress(IPAddress ipAddress)
        {
            byte[] bytes = ipAddress.GetAddressBytes();
            if (bytes.Length != 4)
            {
                return false;
            }

            if (bytes[0] == 10)
            {
                return true;
            }

            if (bytes[0] == 192 && bytes[1] == 168)
            {
                return true;
            }

            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            {
                return true;
            }

            return false;
        }

        private static bool IsCarrierGradeNat(IPAddress ipAddress)
        {
            byte[] bytes = ipAddress.GetAddressBytes();
            if (bytes.Length != 4)
            {
                return false;
            }

            return bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127;
        }

        private static bool IsLinkLocal(IPAddress ipAddress)
        {
            byte[] bytes = ipAddress.GetAddressBytes();
            if (bytes.Length != 4)
            {
                return false;
            }

            return bytes[0] == 169 && bytes[1] == 254;
        }

        private static bool IsVpnInterface(NetworkInterface networkInterface)
        {
            if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Tunnel ||
                networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ppp)
            {
                return true;
            }

            string name = networkInterface.Name;
            string description = networkInterface.Description;

            return name.Contains("vpn", StringComparison.OrdinalIgnoreCase) ||
                   description.Contains("vpn", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<string> GetExternalIPAddressAsync()
        {
            string[] services =
            [
                "https://api.ipify.org",
                "https://icanhazip.com",
                "https://ipecho.net/plain",
                "https://myexternalip.com/raw"
            ];

            using HttpClient client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            foreach (string? service in services)
            {
                try
                {
                    string response = await client.GetStringAsync(service);
                    string ip = response.Trim();

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