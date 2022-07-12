using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Open101.Net;
using Open101.Serializer.DML;
using SerializerPlayground;

namespace Open101.Tools.PacketProxy
{
    public class ForwardingConnection : KISocket
    {
        protected static int s_currConnectionID;
        
        public ForwardingConnection m_peer;
        public PacketDump m_dump;

        public ProxyServer m_proxy;
	    
        public ForwardingConnection(Socket socket) : base(socket) {}
	    
        public override void Start()
        {
            m_handler = new ProxyKIPacketHandler(this);
        }
	    
        public override void Update()
        {
            if (IsClosed())
            {
                if (!m_peer.IsClosed())
                {
                    m_peer.Close();
                }

                m_peer.OnSocketClosed();
            } else if (m_peer.IsClosed())
            {
                m_peer.OnSocketClosed();
                Close();
            }
        }
	    
        public override void ReadHandler(SocketAsyncEventArgs args)
        {
            base.ReadHandler(args);
            
            m_dump.Write(args.Buffer, args.BytesTransferred);

            var proxyHandler = (ProxyKIPacketHandler) m_handler;
            if (proxyHandler.m_pendingSend.Count > 0)
            {
                m_peer.Send(proxyHandler.m_pendingSend.ToArray());
                proxyHandler.m_pendingSend.Clear();
            }
        }

        public static readonly Random s_random = new Random();

        public override void OnSocketClosed()
        {
            if (m_dump != null)
            {
                m_dump.Dispose();
                m_dump = null;
            }
        }
    }

    public class ProxyKIPacketHandler : LoggingKIPacketHandler
    {
        public ForwardingConnection m_connection;
        public List<INetworkMessage> m_pendingSend;

        public ProxyKIPacketHandler(ForwardingConnection connection)
        {
            m_connection = connection;
            m_pendingSend = new List<INetworkMessage>();

            m_displayName = m_connection.GetRemoteEndPoint().ToString();
        }
        
        protected override void HandleMessage(INetworkMessage message, INetworkService service)
        {
            base.HandleMessage(message, service);
            
            if (message is LOGIN_7_Protocol.MSG_CHARACTERSELECTED characterSelected)
            {
                if (characterSelected.m_prepPhase != 0 && characterSelected.m_IP == string.Empty) return;
			    
                var proxy = CreateZoneProxy(characterSelected.m_IP, characterSelected.m_TCPPort);
			    
                characterSelected.m_IP = proxy.m_localEndpoint.Address.ToString();
                characterSelected.m_TCPPort = proxy.m_localEndpoint.Port;
            }

            if (message is GAME_5_Protocol.MSG_SERVERTRANSFER transfer)
            {
                var mainProxy = CreateZoneProxy(transfer.m_IP, transfer.m_TCPPort);
                var fallbackProxy = CreateZoneProxy(transfer.m_fallbackIP, transfer.m_fallbackTCPPort);

                transfer.m_IP = mainProxy.m_localEndpoint.Address.ToString();
                transfer.m_TCPPort = mainProxy.m_localEndpoint.Port;

                if (fallbackProxy != null)
                {
                    transfer.m_fallbackIP = fallbackProxy.m_localEndpoint.Address.ToString();
                    transfer.m_fallbackTCPPort = fallbackProxy.m_localEndpoint.Port;
                }
            }

            if (message is WIZARD2_53_Protocol.MSG_CONNECTIONSTATS stats)
            {
                var proxy = Program.GetProxyServer(stats.m_serverPort);
                stats.m_serverHostname = proxy.m_hostname;
                stats.m_serverPort = proxy.m_hostPost;
            }
            
            m_pendingSend.Add(message);
        }

        private static ProxyServer CreateZoneProxy(string ip, int port)
        {
            if (ip == string.Empty || port == 0) return null;
            int proxyPort = ForwardingConnection.s_random.Next(23000, 44000);
            var proxyAddr = new IPEndPoint(Program.s_proxyIP, proxyPort);
            var proxy = Program.AddProxyServer(proxyAddr, ip, port);
            return proxy;
        }
    }
    
    public class ForwardingConnectionOwner : ForwardingConnection
    {
        public ForwardingConnectionOwner(Socket socket) : base(socket)
        {
            var localPort = ((IPEndPoint)socket.LocalEndPoint).Port;
            var remotePort = ((IPEndPoint)socket.RemoteEndPoint).Port;
            m_proxy = Program.GetProxyServer(localPort);

            var id = Interlocked.Increment(ref s_currConnectionID);
            
            m_dump = new PacketDump($"packets_from_client_{id}.bin");
            var dumpOther = new PacketDump($"packets_from_server_{id}.bin");

            var remoteSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            remoteSocket.Connect(m_proxy.m_externalEndpoint);
            m_peer = new ForwardingConnection(remoteSocket) {m_peer = this, m_dump = dumpOther, m_proxy = m_proxy};
            m_peer.Start();
            m_peer.StartListening();
        }

        public override void Update()
        {
            m_peer.Update();
            base.Update();
        }
    }
    
    
    public class PacketDump // todo: this implementation is so bad lol
    {
        private readonly string m_file;
        private Stream m_stream;
        private BinaryWriter m_writer;
        
        public PacketDump(string file)
        {
            /*m_file = file;
            m_stream = File.OpenWrite(file);
            m_stream.SetLength(0);
            m_writer = new BinaryWriter(m_stream);*/
        }

        public void Dispose()
        {
            /*if (m_stream != null)
            {
                long length = m_stream.Length;
                
                m_stream.Flush();
                m_stream.Dispose();
                m_stream = null;
                
                if (length == 0) File.Delete(m_file);
            }
            if (m_writer != null)
            {
                m_writer.Dispose();
                m_writer = null;
            }
            GC.SuppressFinalize(this);*/
        }

        public void Write(byte[] data, int size)
        {
            /*m_writer.Write(size);
            m_writer.Write(data, 0, size);
            
            m_stream.Flush();*/
        }

        ~PacketDump()
        {
            Dispose();
        }
    }
}