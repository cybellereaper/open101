using System;
using System.Collections.Generic;
using System.Text;

using System.Collections.Concurrent;
using Open101.Serializer.DML;
using wiz101_server.sockets;
using System.Linq;
using SerializerPlayground;
using Open101.Serializer;

namespace wiz101_server
{
    public class ServerRealm
    {

        public uint max_players;
        public uint current_player_count;

        public ConcurrentDictionary<ulong, ServerZone> managed_zones;
        public uint zone_amount;

        public ConcurrentDictionary<ulong, WizardCharacterCreationInfo> character_creation_info_cache;
        public ConcurrentDictionary<ulong, ConcurrentBag<ulong>> user_to_char_map_keys;

        public ServerRealm(uint _max_players, uint _zone_amount, uint _max_players_per_zone)
        {
            max_players = _max_players;
            current_player_count = 0;
            managed_zones = new ConcurrentDictionary<ulong, ServerZone>();

            for (var i = 0; i < _zone_amount; ++i)
            {
                managed_zones.TryAdd(Util.get_next_gid().m_value, new ServerZone(this, _max_players_per_zone));
            }

            character_creation_info_cache = new ConcurrentDictionary<ulong, WizardCharacterCreationInfo>();
            user_to_char_map_keys = new ConcurrentDictionary<ulong, ConcurrentBag<ulong>>();
        }

        public void realm_broadcast(uint _src, INetworkMessage _msg)
        {
            foreach (var zone in managed_zones)
            {
                zone.Value.zone_broadcast(_src, _msg);
            }
        }

        public void assign_zone(GameSocket _sock, PlayerData.InGameCharacterStruct _player)
        {
            foreach (var z in managed_zones)
            {
                if (z.Value.max_players > z.Value.current_player_count)
                {
                    z.Value.add_player(_player.gid.m_value, _sock, _player);
                    break;
                }
            }
        }

        public void cache_character_creation_info(ulong _char_id, ulong _user_id, WizardCharacterCreationInfo _info)
        {
            character_creation_info_cache.TryAdd(_char_id, _info);

            user_to_char_map_keys
                .GetOrAdd(_user_id, new ConcurrentBag<ulong>())
                .Add(_char_id);
        }

        public bool try_get_cached_character_creation_info(ulong _char_id, out WizardCharacterCreationInfo _info)
        {
            return character_creation_info_cache.TryGetValue(_char_id, out _info);
        }

        public List<WizardCharacterCreationInfo> get_cached_characters_for_player(ulong _player_id)
        {
            ConcurrentBag<ulong> char_keys;
            if (user_to_char_map_keys.TryGetValue(_player_id, out char_keys))
            {
                List<WizardCharacterCreationInfo> char_list = new List<WizardCharacterCreationInfo>();

                foreach (var character in char_keys)
                {
                    WizardCharacterCreationInfo cur_info;
                    if (try_get_cached_character_creation_info(character, out cur_info))
                    {
                        cur_info.m_userID = new GID(_player_id);
                        char_list.Add(cur_info);
                    }
                }

                return char_list;
            }
            else
            {
                return new List<WizardCharacterCreationInfo>
                {
                    Util.get_default_character_info()
                };
            }
        }

    }

    public static class LocalRealm
    {
        public static string name = "realm";
        public static ServerRealm realm;
    }

}
