using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace IPPopper
{
    /// <summary>
    /// Represents information about an IP address including its type, interface, and MAC address.
    /// </summary>
    public class IPInfo
    {
        /// <summary>
        /// Gets or sets the IP address.
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the network type (e.g., "Local/LAN", "External/Public", "Local/VPN").
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this is the primary IP address used for outbound connections.
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Gets or sets the network interface name.
        /// </summary>
        public string InterfaceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the MAC address of the network interface.
        /// </summary>
        public string MacAddress { get; set; } = string.Empty;
    }

    /// <summary>
    /// Provides services for retrieving IP address information including local, LAN, VPN, and external addresses.
    /// </summary>
    public class IPService
    {
        /// <summary>
        /// Retrieves all IP addresses including local and external/public addresses.
        /// </summary>
        /// <returns>A list of <see cref="IPInfo"/> objects containing all detected IP addresses.</returns>
        public static async Task<List<IPInfo>> GetAllIPAddressesAsync()
        {
            List<IPInfo> ipList = [];

            // Retrieve all local network interface IP addresses
            List<IPInfo> localIPs = GetLocalIPAddresses();
            ipList.AddRange(localIPs);

            // Retrieve external/public IP via external service
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
                // External IP lookup failed - add placeholder entry
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

        /// <summary>
        /// Retrieves the primary local IP address used for outbound network connections.
        /// </summary>
        /// <returns>The primary IP address string, or "No IP found" if unavailable.</returns>
        public static async Task<string> GetPrimaryLocalIPAsync()
        {
            List<IPInfo> localIPs = GetLocalIPAddresses();
            IPInfo? primary = localIPs.FirstOrDefault(ip => ip.IsPrimary);
            return primary?.Address ?? "No IP found";
        }

        /// <summary>
        /// Retrieves all local IP addresses from active network interfaces.
        /// Excludes loopback interfaces and includes only IPv4 addresses.
        /// </summary>
        /// <returns>A list of local IP addresses sorted with the primary IP first.</returns>
        private static List<IPInfo> GetLocalIPAddresses()
        {
            List<IPInfo> ipList = [];
            string primaryIP = GetPrimaryIPAddress();

            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                // Only process active, non-loopback interfaces
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

        /// <summary>
        /// Retrieves the MAC address from a network interface in colon-separated hexadecimal format.
        /// </summary>
        /// <param name="networkInterface">The network interface to query.</param>
        /// <returns>MAC address string in format "XX:XX:XX:XX:XX:XX", or "N/A" if unavailable.</returns>
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

        /// <summary>
        /// Determines the primary IP address by attempting to connect to an external DNS server (8.8.8.8).
        /// No actual connection is made; this technique identifies the outbound interface.
        /// </summary>
        /// <returns>The primary IP address string, or empty string if determination fails.</returns>
        private static string GetPrimaryIPAddress()
        {
            try
            {
                // Use socket connect technique to determine outbound interface IP
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

        /// <summary>
        /// Determines the network type classification for an IP address.
        /// </summary>
        /// <param name="networkInterface">The network interface.</param>
        /// <param name="addressInfo">The unicast IP address information.</param>
        /// <returns>Network type string (e.g., "Local/LAN", "Local/VPN", "Local/Link-Local").</returns>
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

        /// <summary>
        /// Determines whether an IP address is in a private address range.
        /// Checks for RFC 1918 ranges: 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16.
        /// </summary>
        /// <param name="ipAddress">The IP address to check.</param>
        /// <returns>True if the address is in a private range; otherwise, false.</returns>
        private static bool IsPrivateAddress(IPAddress ipAddress)
        {
            byte[] bytes = ipAddress.GetAddressBytes();
            if (bytes.Length != 4)
            {
                return false;
            }

            // 10.0.0.0/8
            if (bytes[0] == 10)
            {
                return true;
            }

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
            {
                return true;
            }

            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether an IP address is in the Carrier-Grade NAT (CGNAT) range.
        /// Checks for RFC 6598 range: 100.64.0.0/10.
        /// </summary>
        /// <param name="ipAddress">The IP address to check.</param>
        /// <returns>True if the address is in the CGNAT range; otherwise, false.</returns>
        private static bool IsCarrierGradeNat(IPAddress ipAddress)
        {
            byte[] bytes = ipAddress.GetAddressBytes();
            if (bytes.Length != 4)
            {
                return false;
            }

            return bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127;
        }

        /// <summary>
        /// Determines whether an IP address is a link-local address.
        /// Checks for range: 169.254.0.0/16.
        /// </summary>
        /// <param name="ipAddress">The IP address to check.</param>
        /// <returns>True if the address is link-local; otherwise, false.</returns>
        private static bool IsLinkLocal(IPAddress ipAddress)
        {
            byte[] bytes = ipAddress.GetAddressBytes();
            if (bytes.Length != 4)
            {
                return false;
            }

            return bytes[0] == 169 && bytes[1] == 254;
        }

        /// <summary>
        /// Determines whether a network interface is a VPN interface based on type or name.
        /// </summary>
        /// <param name="networkInterface">The network interface to check.</param>
        /// <returns>True if the interface is VPN-related; otherwise, false.</returns>
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

        /// <summary>
        /// Retrieves the external/public IP address by querying multiple external services.
        /// Tries services sequentially until one succeeds.
        /// </summary>
        /// <returns>The external IP address string, or empty string if all services fail.</returns>
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

                    // Validate response is a valid IP address
                    if (IPAddress.TryParse(ip, out _))
                    {
                        return ip;
                    }
                }
                catch
                {
                    // Service failed - try next in list
                    continue;
                }
            }

            return string.Empty;
        }
    }
}