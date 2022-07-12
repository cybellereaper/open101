using Open101.IO;
using Open101.Net;
using Open101.Serializer;
using Open101.Serializer.DML;
using SerializerPlayground;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using wiz101_server.services;

namespace wiz101_server.sockets
{

    public class GameSocket : KISocket
    {
        public PlayerData.InGameCharacterStruct managed_player;

        public ServerZone managing_zone;
        public GameSocket(Socket _sock) : base(_sock)
        {
            //managed_player = Util.create_player_instance();
        }

        public void attach_zone(ServerZone _zone)
        {
            managing_zone = _zone;
        }
        public void assign_player_to_zone(GameSocket socket, PlayerData.InGameCharacterStruct player)
        {
            LocalRealm.realm.assign_zone(socket, player);
        }

        public void detach_zone()
        {
            managing_zone = null;
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
            
            m_handler.m_serviceHandlers[WIZARD_12_Protocol.c_serviceID] = new WizardServiceHandler(this);
        }

        public override void OnSocketClosed()
        {
            base.OnSocketClosed();

            if (managed_player != null)
            {
                if (managing_zone != null)
                    managing_zone.zone_broadcast(managed_player.gid.m_value,
                        new GAME_5_Protocol.MSG_REMOVEOBJECT { m_gameObjectID = managed_player.gid }
                    );

                managing_zone.remove_player(managed_player.gid.m_value, out _); // Just disposing of it for now. If it was saved somewhere, this would be a piece of the puzzle
            }
        }

    }

}
