using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using DragonLib.IO;

namespace Open101.Tools.PacketProxy
{
    public static class Program
    {
        // Private fields for actual login server and ports
        private static readonly string s_actualLoginServer = "26.25.98.202";
        private static readonly ushort s_actualLoginPort = 12000;

        // Public static fields
        public static IPAddress ProxyIP { get; private set; }
        public static IPEndPoint ActualLoginEndpoint { get; private set; }
        private static IPEndPoint _loginProxy;

        private static readonly Dictionary<int, ProxyServer> _proxyServers = new Dictionary<int, ProxyServer>();

        private static void CreateLoginProxyAddress()
        {
            ProxyIP = IPAddress.Parse("192.168.0.77"); // Use IPAddress.TryParse to validate IP
            int port = 13000; // new Random().Next(13000, 14000);
            _loginProxy = new IPEndPoint(ProxyIP, port);
            Console.WriteLine($"_loginProxy: {_loginProxy}");
        }

        /// <summary>
        /// Gets the proxy server associated with the specified port.
        /// </summary>
        /// <param name="port">The port number of the proxy server.</param>
        /// <returns>The proxy server associated with the specified port.</returns>
        public static ProxyServer GetProxyServer(int port)
        {
            lock (_proxyServers)
            {
                return _proxyServers.ContainsKey(port) ? _proxyServers[port] : null;
            }
        }

        /// <summary>
        /// Adds a new proxy server with the given parameters to the list of proxy servers.
        /// </summary>
        /// <param name="localAddress">The local IP and port to bind the proxy server.</param>
        /// <param name="externalHostname">The external hostname of the server to forward packets.</param>
        /// <param name="externalPort">The external port of the server to forward packets.</param>
        /// <param name="persistent">Indicates if the proxy server should be persistent.</param>
        /// <returns>The newly created proxy server.</returns>
        public static ProxyServer AddProxyServer(IPEndPoint localAddress, string externalHostname, int externalPort, bool persistent = false)
        {
            var server = new ProxyServer(localAddress, externalHostname, externalPort)
            {
                Persistent = persistent
            };

            lock (_proxyServers)
            {
                _proxyServers[localAddress.Port] = server;
            }

            Logger.Success($"{localAddress}", "Proxy server created");
            return server;
        }

        public static void Main(string[] args)
        {
            SerializerPlayground.Program.Init();

            ActualLoginEndpoint = new IPEndPoint(Dns.GetHostAddresses(s_actualLoginServer).First(), s_actualLoginPort);
            CreateLoginProxyAddress();

            AddProxyServer(_loginProxy, s_actualLoginServer, s_actualLoginPort, true);

            while (true)
            {
                Thread.Sleep(100);
                lock (_proxyServers)
                {
                    foreach (ProxyServer server in _proxyServers.Values.ToArray())
                    {
                        server.Update();
                    }

                    if (_proxyServers.Count == 0) break;
                }
            }
        }
    }
}
