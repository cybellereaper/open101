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
        //private static string s_actualLoginServer = "127.0.0.1";
        //private static ushort s_actualLoginPort = 12001;
        
        private static string s_actualLoginServer = "26.25.98.202";
        private static ushort s_actualLoginPort = 12000;

        //private static string s_actualLoginServer = "login.us.wizard101.com";
        //private static ushort s_actualLoginPort = 12000;

        public static IPAddress s_proxyIP;
        public static IPEndPoint s_actualLoginEndpoint;
        private static IPEndPoint s_loginProxy;

        public static Dictionary<int, ProxyServer> s_proxyServers = new Dictionary<int, ProxyServer>();

        private static void CreateLoginProxyAddr()
        {
            //string text7;
            //using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.IP))
            //{
            //    socket.Connect("8.8.8.8", 65530);
            //    text7 = ((IPEndPoint)socket.LocalEndPoint).Address.ToString();
            //}
            //s_proxyIP = IPAddress.Parse(text7);
            s_proxyIP = IPAddress.Parse("192.168.0.77");
            
            //int port = new Random().Next(13000, 14000);
            int port = 13000;
            s_loginProxy = new IPEndPoint(s_proxyIP, port);
            
            Console.WriteLine($"s_loginProxy: {s_loginProxy}");
        }

        public static ProxyServer GetProxyServer(int port)
        {
	        lock (s_proxyServers)
	        {
		        return s_proxyServers[port];
	        }
        }

        public static ProxyServer AddProxyServer(IPEndPoint localAddress, string externalHostname, int externalPort, bool persistent=false)
        {
	        var server = new ProxyServer(localAddress, externalHostname, externalPort)
	        {
		        m_persistent = persistent
	        };
	        lock (s_proxyServers)
	        {
		        s_proxyServers[localAddress.Port] = server;
	        }
	        Logger.Success($"{localAddress}", "Proxy server created");
	        return server;
        }

        public static void Main(string[] args)
        {
	        SerializerPlayground.Program.Init();

	        s_actualLoginEndpoint = new IPEndPoint(Dns.GetHostAddresses(s_actualLoginServer).First(), s_actualLoginPort);
            CreateLoginProxyAddr();

            AddProxyServer(s_loginProxy, s_actualLoginServer, s_actualLoginPort, true);

            while (true)
            {
	            Thread.Sleep(100);
	            lock (s_proxyServers)
	            {
		            foreach (ProxyServer server in s_proxyServers.Values.ToArray())
		            {
			            server.Update();
		            }
		            if (s_proxyServers.Count == 0) break;
	            }
            }
        }
    }
}