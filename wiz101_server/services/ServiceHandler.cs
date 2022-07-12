using Open101.Net;
using Open101.Serializer;
using Open101.Serializer.DML;
using SerializerPlayground;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace wiz101_server.sockets
{
    public class ServiceHandler : ControlNetworkService.Handler, SYSTEM_1_Protocol.Handler
    {

        protected KISocket m_socket;
        public bool m_accepted;

        public ServiceHandler(KISocket _sock)
        {
            m_socket = _sock;
        }

        public bool NetHandleSessionAccept(ControlNetworkService.SessionAccept _msg)
        {
            m_accepted = true;
            return true;
        }

        public bool NetHandlePing(SYSTEM_1_Protocol.MSG_PING _msg)
        {
            if (!m_accepted)
            {
                m_socket.Send(new ControlNetworkService.SessionOffer
                {
                    m_sessionID = 4513, // Load once conversion is done
                    m_unknown1 = 0,
                    m_timestamp = 0,
                    m_milliseconds = 0
                });
            }
            else
            {
                m_socket.Send(new SYSTEM_1_Protocol.MSG_PING_RSP());
            }

            return true;
        }

    }

}
