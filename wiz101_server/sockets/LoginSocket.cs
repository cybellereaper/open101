using Open101.Net;
using Open101.Serializer;
using Open101.Serializer.DML;
using SerializerPlayground;
using System.Net.Sockets;
using wiz101_server.services;

namespace wiz101_server.sockets
{
    public class LoginSocket : KISocket
    {

        public LoginSocket(Socket _sock) : base(_sock) { }

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

}
