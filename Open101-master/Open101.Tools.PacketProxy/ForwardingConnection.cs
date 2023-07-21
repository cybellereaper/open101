using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Open101.Net;
using SerializerPlayground;

namespace Open101.Tools.PacketProxy
{
    public class ForwardingConnection : KISocket
    {
        protected static int s_currConnectionID;

        public ForwardingConnection PeerConnection { get; set; }
        public PacketDump Dump { get; set; }
        public ProxyServer Proxy { get; set; }

        public ForwardingConnection(Socket socket) : base(socket) { }

        public override void Start()
        {
            m_handler = new ProxyKIPacketHandler(this);
        }

        public override void Update()
        {
            if (IsClosed())
            {
                if (!PeerConnection.IsClosed())
                {
                    PeerConnection.Close();
                }

                PeerConnection.OnSocketClosed();
            }
            else if (PeerConnection.IsClosed())
            {
                PeerConnection.OnSocketClosed();
                Close();
            }
        }

        public override void ReadHandler(SocketAsyncEventArgs args)
        {
            base.ReadHandler(args);

            Dump.Write(args.Buffer, args.BytesTransferred);

            var proxyHandler = (ProxyKIPacketHandler)m_handler;
            if (proxyHandler.m_pendingSend.Count > 0)
            {
                PeerConnection.Send(proxyHandler.m_pendingSend.ToArray());
                proxyHandler.m_pendingSend.Clear();
            }
        }

        public static readonly Random Random = new Random();

        public override void OnSocketClosed()
        {
            Dump?.Dispose();
            Dump = null;
        }
    }

    public class ProxyKIPacketHandler : LoggingKIPacketHandler
    {
        public ForwardingConnection Connection { get; }
        public List<INetworkMessage> PendingSend { get; }

        public ProxyKIPacketHandler(ForwardingConnection connection)
        {
            Connection = connection;
            PendingSend = new List<INetworkMessage>();

            m_displayName = Connection.GetRemoteEndPoint().ToString();
        }

        protected override void HandleMessage(INetworkMessage message, INetworkService service)
        {
            base.HandleMessage(message, service);

            if (message is LOGIN_7_Protocol.MSG_CHARACTERSELECTED characterSelected)
            {
                if (characterSelected.m_prepPhase != 0 && characterSelected.m_IP == string.Empty) return;

                var proxy = CreateZoneProxy(characterSelected.m_IP, characterSelected.m_TCPPort);

                characterSelected.m_IP = proxy?.LocalEndpoint.Address.ToString();
                characterSelected.m_TCPPort = proxy?.LocalEndpoint.Port ?? 0;
            }

            if (message is GAME_5_Protocol.MSG_SERVERTRANSFER transfer)
            {
                var mainProxy = CreateZoneProxy(transfer.m_IP, transfer.m_TCPPort);
                var fallbackProxy = CreateZoneProxy(transfer.m_fallbackIP, transfer.m_fallbackTCPPort);

                transfer.m_IP = mainProxy?.LocalEndpoint.Address.ToString();
                transfer.m_TCPPort = mainProxy?.LocalEndpoint.Port ?? 0;

                if (fallbackProxy != null)
                {
                    transfer.m_fallbackIP = fallbackProxy.LocalEndpoint.Address.ToString();
                    transfer.m_fallbackTCPPort = fallbackProxy.LocalEndpoint.Port;
                }
            }

            if (message is WIZARD2_53_Protocol.MSG_CONNECTIONSTATS stats)
            {
                var proxy = Program.GetProxyServer(stats.m_serverPort);
                stats.m_serverHostname = proxy?.Hostname;
                stats.m_serverPort = proxy?.HostPost ?? 0;
            }

            PendingSend.Add(message);
        }

        private static ProxyServer CreateZoneProxy(string ip, int port)
        {
            if (string.IsNullOrEmpty(ip) || port == 0) return null;
            int proxyPort = ForwardingConnection.Random.Next(23000, 44000);
            var proxyAddr = new IPEndPoint(Program.ProxyIP, proxyPort);
            var proxy = Program.AddProxyServer(proxyAddr, ip, port);
            return proxy;
        }
    }

    public class ForwardingConnectionOwner : ForwardingConnection
    {
        public ForwardingConnectionOwner(Socket socket) : base(socket)
        {
            var localPort = ((IPEndPoint)socket.LocalEndPoint).Port;
            m_proxy = Program.GetProxyServer(localPort);

            var id = Interlocked.Increment(ref s_currConnectionID);

            m_dump = new PacketDump($"packets_from_client_{id}.bin");
            var dumpOther = new PacketDump($"packets_from_server_{id}.bin");

            var remoteSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            remoteSocket.Connect(m_proxy.ExternalEndpoint);
            m_peer = new ForwardingConnection(remoteSocket) { PeerConnection = this, Dump = dumpOther, Proxy = m_proxy };
            m_peer.Start();
            m_peer.StartListening();
        }

        public override void Update()
        {
            m_peer.Update();
            base.Update();
        }
    }

    public class PacketDump : IDisposable
    {
        private readonly string m_file;
        private Stream m_stream;
        private BinaryWriter m_writer;

        public PacketDump(string file)
        {
            m_file = file;
            m_stream = File.OpenWrite(file);
            m_stream.SetLength(0);
            m_writer = new BinaryWriter(m_stream);
        }

        public void Dispose()
        {
            m_writer?.Dispose();
            m_stream?.Dispose();
            m_writer = null;
            m_stream = null;
            GC.SuppressFinalize(this);
        }

        public void Write(byte[] data, int size)
        {
            m_writer.Write(size);
            m_writer.Write(data, 0, size);
            m_stream.Flush();
        }

        ~PacketDump()
        {
            Dispose();
        }
    }
}
