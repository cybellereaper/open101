using Open101.Serializer;
using Open101.Serializer.DML;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using wiz101_server.sockets;

namespace wiz101_server
{

    public class ServerZone
    {

        public uint max_players;
        public uint current_player_count;

        public ServerRealm managing_realm;

        //turn the u_long into the gid-idk why it's anything different-it just makes things more complicated
        public ConcurrentDictionary<ulong, PlayerData.InGameCharacterStruct> managed_players;
        
        private ushort mobile_id_counter; // This is not great if there are many mobiles


        public ServerZone(ServerRealm _managing_realm, uint _max_players)
        {
            managing_realm = _managing_realm;
            max_players = _max_players;
            current_player_count = 0;
            managed_players = new ConcurrentDictionary<ulong, PlayerData.InGameCharacterStruct>();
            mobile_id_counter = 1;
        }

        public void zone_broadcast(ulong _src, params INetworkMessage[] _msg)
        {
            foreach (var pl in managed_players)
            {
                if (pl.Value.gid.m_value == _src)
                    continue;
                pl.Value.player_socket.Send(_msg);
            }
        }

        public void add_player(ulong _user_id, GameSocket _sock, PlayerData.InGameCharacterStruct _player)
        {
            //_sock.player_gid = new GID(_id);
            //_sock.attach_zone(this);
            _player.gid = new GID(_user_id);
            _player.mobileID = find_free_mobile_id();
            _player.character_object.m_nMobileID = _player.mobileID;
            _player.player_socket = _sock;
            _player.player_socket.attach_zone(this);
            managed_players.TryAdd(_user_id, _player);

            current_player_count++;
        }

        public bool remove_player(ulong _id, out PlayerData.InGameCharacterStruct _out_player)
        {
            current_player_count--;
            return managed_players.TryRemove(_id, out _out_player);
        }

        public ushort find_free_mobile_id()
        {
            return mobile_id_counter++;
        }

    }

}
