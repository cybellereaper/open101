using System;
using System.IO;
using System.Net.Sockets;
using DragonLib.IO;
using Open101.IO;
using Open101.Net;
using Open101.Serializer;
using Open101.Serializer.DML;
using SerializerPlayground;

namespace TempServer
{
    public class GameSocket : KISocket
    {
        public GameSocket(Socket socket) : base(socket)
        {
        }

        public override void Start()
        {
            base.Start();
            
            m_handler = new LoggingKIPacketHandler
            {
                m_displayName = GetRemoteEndPoint().ToString()
            };

            var serviceHandler = new GameServiceHandler(this);
            m_handler.m_serviceHandlers[ControlNetworkService.c_serviceID] = serviceHandler;
            m_handler.m_serviceHandlers[SYSTEM_1_Protocol.c_serviceID] = serviceHandler;
            m_handler.m_serviceHandlers[GAME_5_Protocol.c_serviceID] = serviceHandler;
        }
    }
    
    public class GameServiceHandler : ServiceHandler, GAME_5_Protocol.Handler
    {
        public GameServiceHandler(KISocket socket) : base(socket)
        { }
        
        public bool NetHandleAttach(GAME_5_Protocol.MSG_ATTACH msg)
        {
            m_socket.Send(new GAME_5_Protocol.MSG_LOGINCOMPLETE
            {
                m_zoneName = msg.m_zoneName,
                m_zoneServer = "Open101_TempServer_Zone",
                m_altMusicFile = 0,
                m_criticalObjects = string.Empty,
                m_data = Program.HexStr("6D 09 00 00 78 DA CB 60 62 64 60 60 90 67 00 81 53 1E 9E 85 4C 40 BA 98 B3 45 88 81 81 89 01 01 64 F8 5E 07 31 82 59 B1 4B EF 59 36 70 5B A5 83 D8 CC 60 91 A5 F6 C7 D9 18 18 9E 33 32 E0 04 0D F6 20 F3 18 38 19 88 06 CC 60 57 7C 6A 11 63 40 18 BB 9C 08 7B 40 3A 48 B1 07 04 20 FE DC FE 39 39 17 E6 93 96 16 15 A8 08 CC CE CC CF 6A E9 10 75 2E 29 C5 CE 60 DF A0 00 90 28 D8 6E 4C 9F CC 9E EE 03 A2 03 80 38 05 2C 72 B6 B5 5D 87 81 C1 D3 BA 51 02 A2 42 E0 FB D5 62 98 EA 5F 87 FA 12 61 EC 85 D9 3F 75 5B 78 56 B3 00 15 33 A0 7A 5A 54 58 32 8E 81 61 7D 50 A1 04 44 7C F2 5B BE 7C 50 9C B0 83 79 2B 25 5F E7 40 D4 B1 F7 5E 28 52 61 38 B0 63 4A 90 83 26 48 65 C7 BE 83 2C 30 33 2A 66 0B DA 32 30 88 71 30 32 30 22 A4 36 57 43 24 39 5F 79 C4 02 83 F3 F6 D9 CC 69 70 67 43 C0 B5 CB 99 C0 B4 22 1C F5 B4 80 81 E1 9D CB 95 18 B8 C3 34 30 7C BE 2F 7A 9B 0B AA C8 9E 19 8E D6 0C 0C 91 4C 7A 95 0C 70 3F 65 74 3F 36 C4 E4 3D 67 46 C4 29 42 FF 0D 63 DB 32 50 2C 3C 53 BD A6 85 6E 1B 5A B2 88 37 28 96 45 37 2E 21 B0 C0 82 81 C1 94 6F 26 0B 8E 94 30 A3 77 42 29 03 43 4B C7 B6 5A 60 D8 F5 6F F1 66 60 38 BF 60 1B D0 A3 F2 1F EE 29 33 30 84 49 B2 F3 C3 63 75 3D 28 3B AC 62 62 68 E8 05 FA D1 C1 01 5B 5A 64 44 24 2E B8 1B 19 61 FA 18 64 AE 07 BA 1F 02 B2 41 46 3E 98 CC C2 D0 21 0C 09 68 50 38 82 C4 91 13 31 3F 03 A5 80 11 29 84 04 18 28 07 A3 66 8C 9A 41 6B 33 46 C3 90 B6 00 5E C4 B2 A4 D0 D7 72 81 41 66 0E 12 60 27 41 ED 8D 58 43 77 10 06 85 E3 64 86 0A 85 75 EB 5A 9C 37 0A 89 AA 81 E4 00 BE 8C 86 8B 00"),
                m_dynamicServerProcID = 57781,
                m_dynamicZoneID = 4288020480,
                m_isBossMarkZone = 0,
                m_isCSR = 1,
                m_permissions = 31679,
                m_realmName = "Realm",
                m_serverTime = 1586711547,
                m_showSubscriberIcon = 0,
                m_subscriberCrownsPricePercent = 100,
                m_testServer = 0,
                m_useFriendFinder = 0,
                m_zoneID = new GID(2)
            });
            
            m_socket.Send(new GAME_5_Protocol.MSG_NEWOBJECT
            {
                m_data = File.ReadAllBytes(@"D:\re\wiz101\OpenWizard101\Open101.Tools.CaptureAnalyser\bin\Debug\netcoreapp3.1\objects\object26")
            });

            /*int i = 0;
            while (true)
            {
                string fileName =
                    @$"D:\re\wiz101\OpenWizard101\Open101.Tools.CaptureAnalyser\bin\Debug\netcoreapp3.1\objects\object{i}";
                if (!File.Exists(fileName)) break;
                
                m_socket.Send(new GAME_5_Protocol.MSG_NEWOBJECT
                {
                    m_data = File.ReadAllBytes(fileName)
                });
                
                i++;
            }*/
            
            return true;
        }

        public bool NetHandleQuery_Logout(GAME_5_Protocol.MSG_QUERY_LOGOUT msg)
        {
            m_socket.Send(new GAME_5_Protocol.MSG_CLIENT_DISCONNECT());
            return true;
        }

        public bool NetHandleReqAskServer(GAME_5_Protocol.MSG_REQASKSERVER msg)
        {
            Console.Out.WriteLine($"requirement: {msg.m_requirement}");
            return true;
        }
        
        private ushort m_x;
        private ushort m_y;
        private ushort m_z;
        private byte m_direction;

        public bool NetHandleClientMove(GAME_5_Protocol.MSG_CLIENTMOVE msg)
        {
            Console.Out.WriteLine($"move: {msg.m_locationX} {msg.m_locationY} {msg.m_locationZ} {msg.m_direction} {msg.m_zoneCounter}");
            m_x = msg.m_locationX;
            m_y = msg.m_locationY;
            m_z = msg.m_locationZ;
            m_direction = msg.m_direction;
            return true;
        }

        public bool NetHandleJump(GAME_5_Protocol.MSG_JUMP msg)
        {
            // get unstuck from the floor
            m_socket.Send(new GAME_5_Protocol.MSG_SERVERTELEPORT
            {
                m_direction = m_direction,
                m_locationX = m_x,
                m_locationY = m_y,
                m_locationZ = (ushort)(m_z+1000),
                m_mobileID = 1
            });
        return true;
    }
    }
}