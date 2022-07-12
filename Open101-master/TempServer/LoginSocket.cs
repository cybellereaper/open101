using System.Net.Sockets;
using Open101.Net;
using Open101.Serializer;
using Open101.Serializer.DML;
using SerializerPlayground;

namespace TempServer
{
    public class LoginSocket : KISocket
    {
        public LoginSocket(Socket socket) : base(socket)
        {
        }

        public override void Start()
        {
            base.Start();
            
            m_handler = new LoggingKIPacketHandler
            {
                m_displayName = GetRemoteEndPoint().ToString()
            };

            var serviceHandler = new LoginServiceHandler(this);
            m_handler.m_serviceHandlers[ControlNetworkService.c_serviceID] = serviceHandler;
            m_handler.m_serviceHandlers[SYSTEM_1_Protocol.c_serviceID] = serviceHandler;
            m_handler.m_serviceHandlers[LOGIN_7_Protocol.c_serviceID] = serviceHandler;
        }
    }
    
    public class LoginServiceHandler : ServiceHandler, LOGIN_7_Protocol.Handler
    {
        public LoginServiceHandler(KISocket socket) : base(socket)
        { }
        
        public bool NetHandleUserValidate(LOGIN_7_Protocol.MSG_USER_VALIDATE msg)
        {
            m_socket.Send(new LOGIN_7_Protocol.MSG_USER_VALIDATE_RSP
            {
                m_error = 0,
                m_reason = string.Empty,
                m_userID = msg.m_userID,
                m_timeStamp = string.Empty,
                m_payingUser = 1,
                m_flags = 0
            }, new LOGIN_7_Protocol.MSG_USER_ADMIT_IND
            {
                m_status = 1,
                m_positionInQueue = 0
            });
            return true;
        }

        public bool NetHandleRequestCharacterList(LOGIN_7_Protocol.MSG_REQUESTCHARACTERLIST msg)
        {
            m_socket.Send(new LOGIN_7_Protocol.MSG_STARTCHARACTERLIST
            {
                m_loginServer = "Open101_TempServer_Login",
                m_purchasedCharacterSlots = 0
            }, new LOGIN_7_Protocol.MSG_CHARACTERINFO
            {
                m_characterInfo = Program.HexStr("4C 8F 6E 11 01 00 00 00 00 00 00 E9 30 20 01 00 00 AB 02 D0 DF 33 07 E8 03 00 00 00 07 8D D0 72 24 80 C0 58 20 81 80 40 01 00 00 00 88 BE C1 04 00 00 00 00 00 00 C3 CA F5 40 03 00 00 00 44 64 73 43 84 12 00 00 00 00 00 00 00 00 00 00 00 00 44 64 73 43 C4 EF 01 00 00 00 00 00 00 00 00 00 00 00 44 64 73 43 4B 54 01 00 00 00 00 00 00 00 00 00 00 00 00 00 02 00 00 00 49 1C 01 00 30 33 0B 00")
            }, new LOGIN_7_Protocol.MSG_CHARACTERLIST
            {
                m_error = 0
            });
            return true;
        }

        public bool NetHandleCreateCharacter(LOGIN_7_Protocol.MSG_CREATECHARACTER msg)
        {
            m_socket.Send(new LOGIN_7_Protocol.MSG_CREATECHARACTERRESPONSE
            {
                m_errorCode = 1
            });
            return true;
        }

        public bool NetHandleSelectCharacter(LOGIN_7_Protocol.MSG_SELECTCHARACTER msg)
        {
            m_socket.Send(new LOGIN_7_Protocol.MSG_CHARACTERSELECTED // show validation
            {
                m_IP = string.Empty,
                m_TCPPort = 0,
                m_UDPPort = 0,
                m_key = string.Empty,
                m_userID = new GID(),
                m_charID = new GID(),
                m_zoneID = new GID(),
                m_zoneName = string.Empty,
                m_location = string.Empty,
                m_slot = 0,
                m_prepPhase = 1,
                m_error = 0,
                m_loginServer = "Open101_TempServer_Login"
            });
                
            // .. whatever
            m_socket.Send(new LOGIN_7_Protocol.MSG_CHARACTERSELECTED
            {
                m_IP = "127.0.0.1",
                m_TCPPort = 12002,
                m_UDPPort = 0,
                m_key = string.Empty,
                m_userID = new GID(4295088136144),
                m_charID = new GID(191965934135706025),
                m_zoneID = new GID(123004564835992122),
                m_zoneName = "WizardCity/WC_Ravenwood",
                m_location = "Start",
                m_slot = 0,
                m_prepPhase = 0,
                m_error = 0,
                m_loginServer = "Open101_TempServer"
            });
            return true;
        }
    }
}