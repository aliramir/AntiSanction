using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Management;

namespace AntiSanction
{
    /// <summary>
    /// Helper class to set networking configuration like IP address, DNS servers, etc.
    /// </summary>
    public class NetworkConfigurator
    {

        /// <summary>
        /// Returns a list of all the network interface class names that are currently enabled in the system
        /// </summary>
        /// <returns>list of nic names</returns>
        ///


        public NetworkInterface GetNIC()
        {
            var Nic = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(
                a => a.OperationalStatus == OperationalStatus.Up &&
                (a.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || a.NetworkInterfaceType == NetworkInterfaceType.Ethernet) &&
                a.GetIPProperties().GatewayAddresses.Any(g => g.Address.AddressFamily.ToString() == "InterNetwork"));

            return Nic;
        }
        /// <summary>
        /// Set's a new IP Address and it's Submask of the local machine
        /// </summary>
        /// <param name="ipAddress">The IP Address</param>
        /// <param name="subnetMask">The Submask IP Address</param>
        /// <param name="gateway">The gateway.</param>
        /// <remarks>Requires a reference to the System.Management namespace</remarks>
        public void SetIP(string ipAddress, string subnetMask, string gateway)
        {
            using (var networkConfigMng = new ManagementClass("Win32_NetworkAdapterConfiguration"))
            {
                using (var networkConfigs = networkConfigMng.GetInstances())
                {
                    foreach (var managementObject in networkConfigs.Cast<ManagementObject>().Where(managementObject => (bool)managementObject["IPEnabled"]))
                    {
                        using (var newIP = managementObject.GetMethodParameters("EnableStatic"))
                        {
                            // Set new IP address and subnet if needed
                            if ((!String.IsNullOrEmpty(ipAddress)) || (!String.IsNullOrEmpty(subnetMask)))
                            {
                                if (!String.IsNullOrEmpty(ipAddress))
                                {
                                    newIP["IPAddress"] = new[] { ipAddress };
                                }

                                if (!String.IsNullOrEmpty(subnetMask))
                                {
                                    newIP["SubnetMask"] = new[] { subnetMask };
                                }

                                managementObject.InvokeMethod("EnableStatic", newIP, null);
                            }

                            // Set mew gateway if needed
                            if (!String.IsNullOrEmpty(gateway))
                            {
                                using (var newGateway = managementObject.GetMethodParameters("SetGateways"))
                                {
                                    newGateway["DefaultIPGateway"] = new[] { gateway };
                                    newGateway["GatewayCostMetric"] = new[] { 1 };
                                    managementObject.InvokeMethod("SetGateways", newGateway, null);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set's the DNS Server of the local machine
        /// </summary>
        /// <param name="nic">NIC address</param>
        /// <param name="dnsServers">Comma seperated list of DNS server addresses</param>
        /// <remarks>Requires a reference to the System.Management namespace</remarks>
        public void SetNameservers(string nic, string dnsServers)
        {
            using (var networkConfigMng = new ManagementClass("Win32_NetworkAdapterConfiguration"))
            {
                using (var networkConfigs = networkConfigMng.GetInstances())
                {
                    foreach (var managementObject in networkConfigs.Cast<ManagementObject>().Where(objMO => (bool)objMO["IPEnabled"] && objMO["Caption"].Equals(nic)))
                    {
                        using (var newDNS = managementObject.GetMethodParameters("SetDNSServerSearchOrder"))
                        {
                            newDNS["DNSServerSearchOrder"] = dnsServers.Split(',');
                            managementObject.InvokeMethod("SetDNSServerSearchOrder", newDNS, null);
                        }
                    }
                }
            }
        }
        private static ConnectionOptions PrepareOptions()
        {
            ConnectionOptions options = new ConnectionOptions();
            options.Impersonation = ImpersonationLevel.Impersonate;
            options.Authentication = System.Management.AuthenticationLevel.Default;
            options.EnablePrivileges = true;
            return options;
        }

        // Method to prepare WMI query management scope.
        private static ManagementScope PrepareScope(string machineName, ConnectionOptions options, string path)
        {
            ManagementScope scope = new ManagementScope();
            scope.Path = new ManagementPath(@"\\" + machineName + path);
            scope.Options = options;
            scope.Connect();
            return scope;
        }
        public void SetDns(string NIC, string DNS1, string DNS2)
        {
            ConnectionOptions options = PrepareOptions();
            ManagementScope scope = PrepareScope(Environment.MachineName, options, @"\root\CIMV2");
            ManagementPath managementPath = new ManagementPath("Win32_NetworkAdapterConfiguration");
            ObjectGetOptions objectGetOptions = new ObjectGetOptions();
            ManagementClass mc = new ManagementClass(scope, managementPath, objectGetOptions);
            ManagementObjectCollection moc = mc.GetInstances();
            foreach (ManagementObject mo in moc)
            {
                if ((bool)mo["IPEnabled"])
                {
                    if (mo["Caption"].ToString().Contains(NIC))
                    {
                        try
                        {
                            ManagementBaseObject objdns = mo.GetMethodParameters("SetDNSServerSearchOrder");
                            if (objdns != null)
                            {
                                string[] s = { DNS1, DNS2 };
                                objdns["DNSServerSearchOrder"] = s;
                                mo.InvokeMethod("SetDNSServerSearchOrder", objdns, null);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                            Console.ReadKey();
                            throw;
                        }
                    }
                }
            }
        }

        public IPAddress GetDnsAdress()
        {
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties ipProperties = networkInterface.GetIPProperties();
                    IPAddressCollection dnsAddresses = ipProperties.DnsAddresses;

                    foreach (IPAddress dnsAdress in dnsAddresses)
                    {
                        return dnsAdress;
                    }
                }
            }

            throw new InvalidOperationException("Unable to find DNS Address");
        }

        public string GetDns()
        {
            NetworkInterface ni = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(x => x.NetworkInterfaceType != NetworkInterfaceType.Loopback && x.OperationalStatus == OperationalStatus.Up);
            if (ni.GetIPProperties().IsDynamicDnsEnabled || ni.GetIPProperties().IsDnsEnabled)
            {

                var dnsServer = ni.GetIPProperties().DnsAddresses;

                foreach (var dns in dnsServer)
                {
                    IPHostEntry hostInfo = Dns.GetHostEntry(dns);
                    Console.WriteLine($"DNS server: {dns.ToString()} ({hostInfo.HostName.ToString()})");
                    return dns.ToString();
                }
                return null;
            }
            return null;
        }

        public void UnsetDNS(string nic)
        {
            ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection objMOC = objMC.GetInstances();
            foreach (ManagementObject objMO in objMOC)
            {
                if ((bool)objMO["IPEnabled"])
                {
                    if (objMO["Caption"].ToString().Contains(nic))
                    {
                        ManagementBaseObject objdns = objMO.GetMethodParameters("SetDNSServerSearchOrder");
                        if (objdns != null)
                        {
                            objdns["DNSServerSearchOrder"] = null;
                            objMO.InvokeMethod("SetDNSServerSearchOrder", objdns, null);
                        }
                    }
                }
            }
        }
    }
}
