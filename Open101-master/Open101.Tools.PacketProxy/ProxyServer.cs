using System;
using System.Linq;
using System.Net;
using DragonLib.IO;
using Open101.Net;

namespace Open101.Tools.PacketProxy
{
    public class ProxyServer
    {
        private SocketManager<ForwardingConnectionOwner, AsyncTcpAcceptor> _server;
        private DateTime _lastActivityTime;

        public ProxyServer(IPEndPoint localEndpoint, string externalHost, int externalPort)
        {
            LocalEndpoint = localEndpoint;
            ExternalEndpoint = new IPEndPoint(Dns.GetHostAddresses(externalHost).First(), externalPort);

            _server = new SocketManager<ForwardingConnectionOwner, AsyncTcpAcceptor>();
            _server.Start(localEndpoint.Address, (ushort)localEndpoint.Port);

            HasHadConnection = false;
        }

        public IPEndPoint LocalEndpoint { get; }
        public IPEndPoint ExternalEndpoint { get; }

        /// <summary>
        /// Gets or sets whether the proxy server should be persistent.
        /// </summary>
        public bool Persistent { get; set; }

        /// <summary>
        /// Gets or sets whether the proxy server has had a connection.
        /// </summary>
        public bool HasHadConnection { get; set; }

        /// <summary>
        /// Updates the proxy server.
        /// </summary>
        public void Update()
        {
            _server.UpdateAll();

            if (!Persistent)
            {
                var sockCount = _server.GetSocketCount();

                if (sockCount == 0)
                {
                    if (_lastActivityTime == DateTime.MinValue)
                    {
                        _lastActivityTime = DateTime.Now;
                    }

                    if (DateTime.Now - _lastActivityTime > TimeSpan.FromSeconds(30))
                    {
                        Logger.Warn($"{LocalEndpoint}", "Proxy server closing due to timeout");
                        Stop();
                    }
                }
                else
                {
                    _lastActivityTime = DateTime.MinValue;
                }
            }
        }

        /// <summary>
        /// Stops the proxy server.
        /// </summary>
        public void Stop()
        {
            _server.Stop();

            lock (Program.ProxyServers)
            {
                Program.ProxyServers.Remove(LocalEndpoint.Port);
            }
        }
    }
}
