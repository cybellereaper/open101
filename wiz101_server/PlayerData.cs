using Open101.Net;
using Open101.Serializer;
using Open101.Serializer.DML;
using SerializerPlayground;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using wiz101_server.sockets;

namespace wiz101_server
{
    public static class PlayerData
    {

        public class LoginPlayerStruct
        {
            public List<CharacterCreationInfo> characters;
            public GID user_id;
        }

        public class InGameCharacterStruct
        {
            public WizClientObject character_object;
            public ClientWizInventoryBehavior inventory;

            public bool initialized { get; set; }

            public float x { get; set; }
            public float y { get; set; }
            public float z { get; set; }
            public float direction { get; set; }

            public GID gid { get; set; }
            public GID charID { get; set; }
            public ByteString zone_name { get; set; }
            public GID zoneID { get; set; }

            public float markerX { get; set; }
            public float markerY { get; set; }
            public float markerZ { get; set; }
            public float markerDirection { get; set; }
            public ByteString marker_zone_name { get; set; }
            public GID marker_zoneID { get; set; }

            public ushort mobileID { get; set; }

            public int maxMana { get; set; }
            public int curMana { get; set; }

            public GameSocket player_socket { get; set; }
        }
    }

}
