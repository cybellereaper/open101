using System;
using System.Threading;
using DragonLib.IO;
using Haukcode.PcapngUtils.PcapNG;
using Open101.Net;
using Open101.Serializer.DML;
using PacketDotNet;
using SerializerPlayground;

namespace Open101.Tools.CaptureAnalyser
{
    public static class Program
    {
        private static readonly AnalysisHandlerBase s_handler = new AnalysisHandlerBase();

        public static void Main(string[] args)
        {
            SerializerPlayground.Program.Init();

            using var reader = new PcapNGReader(@"D:\re\wiz101\net\ravenwood.pcapng", false);
            CancellationTokenSource source = new CancellationTokenSource();
            reader.OnReadPacketEvent += OnReadPacketEvent;
            reader.ReadPackets(source.Token);
        }

        private static void OnReadPacketEvent(object context, IPacket packet)
        {
            var p = Packet.ParsePacket(LinkLayers.Ethernet, packet.Data);
            var ipv4 = p.PayloadPacket as IPv4Packet;
            if (ipv4 == null) return;

            if (!(ipv4.PayloadPacket is TcpPacket tcp)) return;

            s_handler.ParsePacket(tcp, packet.Seconds);
        }
    }

    public class AnalysisHandlerBase : GAME_5_Protocol.Handler, LOGIN_7_Protocol.Handler, WIZARD_12_Protocol.Handler,
        QUEST_MESSAGES_52_Protocol.Handler, WIZARD2_53_Protocol.Handler
    {
        private int m_currentZonePort;
        private bool m_loginActive;

        private LoggingKIPacketHandler m_loginInCapture;
        private LoggingKIPacketHandler m_loginOutHandler;

        private TcpCaptureCleaner m_zoneInCapture;
        private TcpCaptureCleaner m_zoneOutCapture;

        public void ParsePacket(TcpPacket packet, ulong timestamp)
        {
            bool handled = false;
            if (packet.SourcePort == 12000)
            {
                if (packet.PayloadData.Length == 0) return;
                SetLoginActive();
                m_loginInCapture.ConsumeBuffer(packet.PayloadData, packet.PayloadData.Length);
                handled = true;
            }
            else if (packet.DestinationPort == 12000)
            {
                if (packet.PayloadData.Length == 0) return;
                SetLoginActive();
                m_loginOutHandler.ConsumeBuffer(packet.PayloadData, packet.PayloadData.Length);
                handled = true;
            }

            if (packet.SourcePort == m_currentZonePort)
            {
                m_zoneInCapture.ProcessPacket(packet);
                handled = true;
            }
            else if (packet.DestinationPort == m_currentZonePort)
            {
                m_zoneOutCapture.ProcessPacket(packet);
                handled = true;
            }

            if (handled && packet.PayloadData.Length > 0)
            {
                Logger.Debug("Packet", DateTimeOffset.FromUnixTimeSeconds((long)timestamp).ToString());
            }
        }

        private void SetLoginActive()
        {
            if (m_loginActive) return;
            m_currentZonePort = 0;
            m_loginActive = true;
            m_zoneInCapture = null;
            m_zoneOutCapture = null;

            m_loginInCapture = CreatePacketHandler();
            m_loginOutHandler = CreatePacketHandler();
            m_loginInCapture.m_displayName = "Login In";
            m_loginOutHandler.m_displayName = "Login Out";
        }

        private void SetZoneActive()
        {
            m_loginActive = false;
            m_loginInCapture = null;
            m_loginOutHandler = null;

            var zoneInHandler = CreatePacketHandler();
            var zoneOutHandler = CreatePacketHandler();

            zoneInHandler.m_displayName = "Zone In";
            zoneOutHandler.m_displayName = "Zone Out";

            m_zoneInCapture = new TcpCaptureCleaner
            {
                m_handler = zoneInHandler
            };

            m_zoneOutCapture = new TcpCaptureCleaner
            {
                m_handler = zoneOutHandler
            };
        }

        private LoggingKIPacketHandler CreatePacketHandler()
        {
            var result = new LoggingKIPacketHandler();
            result.m_serviceHandlers[GAME_5_Protocol.c_serviceID] = this;
            result.m_serviceHandlers[LOGIN_7_Protocol.c_serviceID] = this;
            result.m_serviceHandlers[WIZARD2_53_Protocol.c_serviceID] = this;
            return result;
        }

        public bool NetHandleServerTransfer(GAME_5_Protocol.MSG_SERVERTRANSFER msg)
        {
            m_currentZonePort = msg.m_TCPPort;
            SetZoneActive();
            return true;
        }

        public bool NetHandleCharacterSelected(LOGIN_7_Protocol.MSG_CHARACTERSELECTED msg)
        {
            if (msg.m_TCPPort == 0) return true;
            m_currentZonePort = msg.m_TCPPort;
            SetZoneActive();
            return true;
        }

        public bool NetHandleCONNECTIONSTATS(WIZARD2_53_Protocol.MSG_CONNECTIONSTATS msg)
        {
            return true;
        }
    }
}
