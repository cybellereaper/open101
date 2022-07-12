using System;
using System.Linq;
using System.Net;
using DragonLib.IO;
using Open101.Net;

namespace Open101.Tools.PacketProxy
{
    public class ProxyServer
    {
        public SocketManager<ForwardingConnectionOwner, AsyncTcpAcceptor> m_server;
        
        public readonly string m_hostname;
        public readonly int m_hostPost;
        
        public readonly IPEndPoint m_localEndpoint;
        public readonly IPEndPoint m_externalEndpoint;

        public DateTime? m_timeoutTimer;
	    
        public bool m_persistent;
        public bool m_hasHadConnection;

        public ProxyServer(IPEndPoint endPoint, string externalHost, int externalPort)
        {
            m_hostname = externalHost;
            m_hostPost = externalPort;
            var external = new IPEndPoint(Dns.GetHostAddresses(externalHost).First(), externalPort);

            m_localEndpoint = endPoint;
            m_externalEndpoint = external;

            m_server = new SocketManager<ForwardingConnectionOwner, AsyncTcpAcceptor>();
            m_server.Start(endPoint.Address, (ushort)endPoint.Port);
        }

        public void Update()
        {
            m_server.UpdateAll();
            if (!m_persistent)
            {
                var sockCount = m_server.GetSocketCount();

                if (sockCount == 0)
                {
                    if (m_timeoutTimer == null)
                    {
                        m_timeoutTimer = DateTime.Now;
                    }

                    if (DateTime.Now - m_timeoutTimer > TimeSpan.FromSeconds(30))
                    {
                        Logger.Warn($"{m_localEndpoint}", "Proxy server closing due to timeout");
                        Stop();
                    }
                } else
                {
                    m_timeoutTimer = null;
                }
            }
        }

        public void Stop()
        {
            m_server.Stop();
            lock (Program.s_proxyServers)
            {
                Program.s_proxyServers.Remove(m_localEndpoint.Port);
            }
        }
    }
}