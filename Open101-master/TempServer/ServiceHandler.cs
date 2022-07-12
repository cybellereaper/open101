using Open101.Net;
using Open101.Serializer.DML;
using SerializerPlayground;

namespace TempServer
{
    public class ServiceHandler : ControlNetworkService.Handler, SYSTEM_1_Protocol.Handler
    {
        protected KISocket m_socket;
        public bool m_accepted;

        public ServiceHandler(KISocket socket)
        {
            m_socket = socket;
        }

        public bool NetHandleSessionAccept(ControlNetworkService.SessionAccept msg)
        {
            m_accepted = true;
            return true;
        }

        public bool NetHandlePing(SYSTEM_1_Protocol.MSG_PING msg)
        {
            if (!m_accepted)
            {
                m_socket.Send( new ControlNetworkService.SessionOffer
                {
                    m_sessionID = 4513,
                    m_unknown1 = 0,
                    m_timestamp = 0,
                    m_milliseconds = 0
                });
            } else
            {
                m_socket.Send(new SYSTEM_1_Protocol.MSG_PING_RSP());
            }
            return true;
        }
    }
}