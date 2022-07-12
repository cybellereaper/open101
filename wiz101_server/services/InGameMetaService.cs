using Open101.Serializer;
using Open101.Serializer.DML;
using SerializerPlayground;
using System;
using System.Collections.Generic;
using System.Text;
using wiz101_server.sockets;

namespace wiz101_server.services
{
    public class InGameMetaService : ServiceHandler
    {

        public InGameMetaService(GameSocket _sock) : base(_sock) { }

        public void attach_player(PlayerData.InGameCharacterStruct _player)
        {
            ((GameSocket)m_socket).managed_player = _player;
        }

        public PlayerData.InGameCharacterStruct owned_player()
        {
            return ((GameSocket)m_socket).managed_player;
        }

        public void respond(params INetworkMessage[] _msgs)
        {
            m_socket.Send(_msgs);
        }

        public void broadcast(bool _include_self, params INetworkMessage[] _to_broadcast)
        {
            var sock = m_socket as GameSocket;
            
            if (sock.managing_zone != null)
                sock.managing_zone.zone_broadcast(_include_self ? ulong.MaxValue : ((GameSocket)m_socket).managed_player.gid.m_value, _to_broadcast);
        }

    }

}
