using Open101.IO;
using Open101.Net;
using Open101.Serializer;
using Open101.Serializer.PropertyClass;
using SerializerPlayground;
using System;
using System.IO;
using wiz101_server.sockets;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Open101.Serializer.DML;
using SharpDX.Text;

namespace wiz101_server.services
{
    public class GameServiceHandler : InGameMetaService, GAME_5_Protocol.Handler
    {
        public GameServiceHandler(GameSocket _sock) : base(_sock) { }

        public bool NetHandleADCLICKTHROUGH(GAME_5_Protocol.MSG_ADCLICKTHROUGH _msg)
        {
            return true;
        }

        public bool NetHandleAddItemRequest(GAME_5_Protocol.MSG_ADDITEMREQUEST _msg)
        {
            var op = owned_player();
            var new_item = new CoreObject();
            SerializerCoreDataBinaryInstance.InitCoreObjectFromTemplate(new_item, (uint)_msg.m_itemTemplateID.m_value);

            op.inventory.m_itemList.Add(new_item);
            respond(
                new GAME_5_Protocol.MSG_INVENTORYBEHAVIOR_ADDITEM
                {
                    m_globalID = new GID(new_item.m_globalID),
                    m_serializedItem = Util.serialize_item_prop(new_item, new_item.m_nMobileID, true)
                }
            );
            
            return true;
        }

        public bool NetHandleAddObject(GAME_5_Protocol.MSG_ADDOBJECT _msg)
        {
            //should either be socket or broadcast or both
            respond(
                new GAME_5_Protocol.MSG_ADDOBJECT
                {
                    m_direction = _msg.m_direction,
                    m_gameObjectID = _msg.m_gameObjectID,
                    m_locationX = _msg.m_locationX,
                    m_locationY = _msg.m_locationY,
                    m_locationZ = _msg.m_locationZ,
                    m_name = _msg.m_name,
                    m_templateID = _msg.m_templateID,
                }
            );

            return true;
        }

        public bool NetHandleAttach(GAME_5_Protocol.MSG_ATTACH _msg)
        {
            WizardCharacterCreationInfo creation_info;
            if (!LocalRealm.realm.try_get_cached_character_creation_info(_msg.m_charID.m_value, out creation_info))
            {
                attach_player(Util.create_default_player_instance(Util.get_next_gid(), 0x18));
            }
            else
            {
                attach_player(Util.create_player_instance(creation_info, _msg.m_charID, 0x18));
            }

            var op = owned_player();
            op.charID = _msg.m_charID;
            op.gid = _msg.m_userID;
            
            GameSocket socket = m_socket as GameSocket;
            socket.assign_player_to_zone(socket, op);
            
            // Actual packets
            respond(
                new GAME_5_Protocol.MSG_LOGINCOMPLETE
                {
                    m_zoneName = _msg.m_zoneName,
                    m_zoneServer = "Server_Zone",
                    m_altMusicFile = 0,
                    m_criticalObjects = string.Empty, // Should be loaded
                    m_data = Util.serialize_player_prop(op.character_object, op.mobileID, false),
                    m_dynamicServerProcID = 57781,
                    m_dynamicZoneID = 4288020480,
                    m_isBossMarkZone = 0,
                    m_isCSR = 0,
                    m_permissions = 31679,
                    m_realmName = "Realm",
                    m_serverTime = 0,
                    m_showSubscriberIcon = 0,
                    m_subscriberCrownsPricePercent = 100,
                    m_testServer = 0,
                    m_useFriendFinder = 1,
                    m_zoneID = new GID()
                }
            );

            foreach (var player in socket.managing_zone.managed_players)
            {
                if (player.Value.gid.m_value == op.gid.m_value)
                    continue;

                player.Value.character_object.m_globalID = player.Value.gid;
                player.Value.character_object.m_fScale = 1;
                player.Value.character_object.m_location = new SharpDX.Vector3(player.Value.x / 4, player.Value.y / 4, player.Value.z / 4);
                player.Value.character_object.m_nMobileID = player.Value.mobileID;

                respond(
                    new GAME_5_Protocol.MSG_NEWOBJECT
                    {
                        m_data = Util.serialize_player_prop(player.Value.character_object, player.Value.mobileID, true)
                    }
                );
            }

            {
                op.character_object.m_globalID = op.gid;
                op.character_object.m_fScale = 1;
                op.character_object.m_location = new SharpDX.Vector3(op.x / 4, op.y / 4, op.z / 4);
                op.character_object.m_nMobileID = op.mobileID;

                broadcast(false, new GAME_5_Protocol.MSG_NEWOBJECT { m_data = Util.serialize_player_prop(op.character_object, op.mobileID, true) });
            }

            return true;
        }
        public bool NetHandleBan_Rsp(GAME_5_Protocol.MSG_BAN_RSP _msg)
        {
            respond(
                new GAME_5_Protocol.MSG_BAN_RSP
                {
                    m_bannedID = _msg.m_bannedID,
                    m_banTime = _msg.m_banTime,
                    m_banType = _msg.m_banType,
                    m_success = _msg.m_success,
                }
            );
            
            return true;
        }
        public bool NetHandleBuddyRequestList(GAME_5_Protocol.MSG_BUDDYREQUESTLIST _msg)
        {
            var op = owned_player();

            respond(
                new GAME_5_Protocol.MSG_BUDDYLISTCOMPLETE
                {
                    m_listOwnerGID = op.gid
                }
            );

            return true;
        }
        public bool NetHandleChannelChat(GAME_5_Protocol.MSG_CHANNELCHAT _msg)
        {
            var op = owned_player();

            respond(
                new GAME_5_Protocol.MSG_CHANNELCHAT
                {
                    m_filter = 0,
                    m_message = _msg.m_message,
                    m_sourceID = op.gid,
                    m_targetID = _msg.m_targetID,
                    m_sourceName = _msg.m_sourceName,
                    m_flags = _msg.m_flags,
                }
            );

            return true;
        }

        public bool NetHandleClientMove(GAME_5_Protocol.MSG_CLIENTMOVE _msg)
        {
            var op = owned_player();
            op.x = unchecked((short)_msg.m_locationX) * 4.0f;
            op.y = unchecked((short)_msg.m_locationY) * 4.0f;
            op.z = unchecked((short)_msg.m_locationZ) * 4.0f;
            op.direction = (float)(_msg.m_direction * Math.PI * 2 / 250);

            broadcast(false,
                new GAME_5_Protocol.MSG_SERVERMOVE
                {
                    //m_gameObjectID = op.gid,
                    m_locationX = (ushort)(op.x / 4),
                    m_locationY = (ushort)(op.y / 4),
                    m_locationZ = (ushort)(op.z / 4),
                    m_direction = (byte)(op.direction / Math.PI / 2 * 250),
                    m_mobileID = op.mobileID,
                    //m_name = op.zone_name,
                    //m_startDragging = 0,
                    //m_templateID = op.gid,
                }
            );
            
            return true;
        }
        public bool NetHandleEquipItem(GAME_5_Protocol.MSG_EQUIPITEM _msg)
        {
            var op = owned_player();
            if (_msg.m_isEquip == 0)
            {
                respond(
                    _msg // should be validated
                );

                broadcast(true,
                    new GAME_5_Protocol.MSG_EQUIPMENTBEHAVIOR_PUBLICUNEQUIPITEM
                    {
                        m_globalID = op.gid,
                        m_indexToRemove = (byte)op.inventory.m_itemList.FindIndex(x => x.m_globalID == _msg.m_itemID.m_value)
                    }
                );
            }
            else
            {
                //no check for valid yet
                respond(
                    _msg
                );

                var itemObj = op.inventory.m_itemList.Find(x => x.m_globalID == _msg.m_itemID.m_value);
                if (itemObj != null)
                {
                    var mobile_id = itemObj.m_nMobileID; // Seems weird

                    broadcast(true,
                        new GAME_5_Protocol.MSG_EQUIPMENTBEHAVIOR_PUBLICEQUIPITEM
                        {
                            m_globalID = op.gid,
                            m_serializedInfo = Util.serialize_item_prop(
                                itemObj,
                                mobile_id,
                                false
                            ),
                        }
                    );
                }
            }


            return true;
        }
        public bool NetHandleMarkLocation(GAME_5_Protocol.MSG_MARK_LOCATION _msg)
        {
            var op = owned_player();
            op.markerX = op.x;
            op.markerY = op.y;
            op.markerZ = op.z;
            op.markerDirection = op.direction;

            if (op.curMana >= op.maxMana / 10)
            {
                respond(
                    new WIZARD_12_Protocol.MSG_UPDATEMANA
                    {
                        m_mana = (op.curMana -= (op.maxMana / 10)),
                        m_maxMana = op.maxMana
                    },
                    new GAME_5_Protocol.MSG_MARK_LOCATION_RESPONSE
                    {
                        m_result = 1,
                        m_zoneName = owned_player().zone_name,
                        m_zoneDisplayNameId = "Ravenwood",
                        m_zoneType = 0,
                        m_instanceId = new GID(1),
                        m_locationX = op.x,
                        m_locationY = op.y,
                        m_locationZ = op.z,
                        m_direction = op.direction,
                        m_commonsZoneId = "0",
                        m_markType = "1"
                    }
                );
            }
            else
            {
                respond(
                    new GAME_5_Protocol.MSG_MARK_LOCATION_RESPONSE
                    {
                        m_result = 0
                    }
                );
            }

            return true;
        }

        public bool NetHandleRecallLocation(GAME_5_Protocol.MSG_RECALL_LOCATION _msg)
        {
            // No idea what this ACTUALLY expects. Before it just used ServerTransfer, but that seems wrong
            var op = owned_player();
            op.x = op.markerX;
            op.y = op.markerY;
            op.z = op.markerZ;
            op.direction = op.markerDirection;

            broadcast(true,
                new GAME_5_Protocol.MSG_SERVERTELEPORT
                {
                    m_direction = (byte)(op.direction / Math.PI / 2 * 250),
                    m_locationX = (ushort)(op.x / 4),
                    m_locationY = (ushort)(op.y / 4),
                    m_locationZ = (ushort)(op.z / 4),
                    m_mobileID = op.mobileID,
                }
            );
            
            return true;
        }
        public bool NetHandleRequestRadialChat(GAME_5_Protocol.MSG_REQUESTRADIALCHAT _msg)
        {
            var string_buffer = new ByteBuffer(_msg.m_message.GetBytes());

            uint full_length = string_buffer.GetSize();
            ushort count = string_buffer.ReadUInt16();

            if (count*2 > full_length-2)
            {
                // Unhandled for now. Could just broadcast, but who knows what is in there
            }
            else
            {
                var str_bytes = string_buffer.ReadBytes(count * 2);
                var msg_text = Encoding.Unicode.GetString(str_bytes).Trim();
                
                if (msg_text.StartsWith("/TRANSFER "))
                {
                    var target_loc = msg_text.Substring(9).Trim();
                    owned_player().zone_name = target_loc;
                    respond(
                        new GAME_5_Protocol.MSG_ZONETRANSFERREQUEST
                        {
                            m_sendAck = 0,
                            m_zoneName = target_loc
                        }
                    );
                }
                else
                {
                    broadcast(false,
                        new GAME_5_Protocol.MSG_RADIALCHAT
                        {
                            m_message = _msg.m_message,
                            m_sourceID = owned_player().gid,
                            m_sourceName = "?",
                        }
                    );
                }
            }

            return true;
        }

        public bool NetHandleRequestRadialQuickChat(GAME_5_Protocol.MSG_REQUESTRADIALQUICKCHAT _msg)
        {
            broadcast(false,
                new GAME_5_Protocol.MSG_RADIALQUICKCHAT
                {
                    m_messageID = _msg.m_messageID,
                    m_sourceID = owned_player().charID,
                    m_sourceName = "?"
                }
            );

            return true;
        }

        public bool NetHandleRequestRadialQuickChatExt(GAME_5_Protocol.MSG_REQUESTRADIALQUICKCHATEXT _msg)
        {
            broadcast(false,
                new GAME_5_Protocol.MSG_RADIALQUICKCHATEXT
                {
                    m_message = _msg.m_message,
                    m_sourceID = owned_player().charID,
                    m_sourceName = "?"
                }
            );

            return true;
        }
        public bool NetHandleReqAskServer(GAME_5_Protocol.MSG_REQASKSERVER _msg)
        {
            respond(
                new GAME_5_Protocol.MSG_REQASKSERVER
                {
                    m_requestID = _msg.m_requestID,
                    m_response = 0,
                    m_requirement = ""
                }
            );

            return true;
        }

        public bool NetHandleRequestGifts(GAME_5_Protocol.MSG_REQUEST_GIFTS _msg)
        {
            respond(
                new GAME_5_Protocol.MSG_RECEIVE_GIFTS
                {
                    m_crownsRewards = 1000,
                    m_data = Util.hex_string_to_bytes("2A EA 7A 22 01 00 00 00 29 8A 52 78 {0} 00 09 00 00 01 00 00 00 00 00 00 00 20 00 38 61 64 36 61 34 32 37 37 30 35 63 62 37 38 36 30 31 37 30 39 31 33 61 35 64 34 39 35 39 35 37"),
                    m_success = 1
                }
            );

            return true;
        }

        public bool NetHandleQuery_Logout(GAME_5_Protocol.MSG_QUERY_LOGOUT _msg)
        {
            respond(
                new GAME_5_Protocol.MSG_CLIENT_DISCONNECT()
            );

            return true;
        }

        public bool NetHandleJump(GAME_5_Protocol.MSG_JUMP _msg)
        {
            return true;
        }

        public bool NetHandleZoneTransferAck(GAME_5_Protocol.MSG_ZONETRANSFERACK _msg)
        {
            respond(
                new GAME_5_Protocol.MSG_SERVERTRANSFER
                {
                    //m_IP = Program.GAME_IP.ToString(),
                    m_IP = Program.GAME_IP.ToString(),
                    m_TCPPort = Program.GAME_PORT,
                    m_key = 1337,
                    m_userID = owned_player().gid,
                    m_charID = owned_player().charID,
                    m_zoneName = owned_player().zone_name,
                    m_zoneID = new GID(5555),
                    m_location = string.Empty,
                    m_slot = 1,
                    m_sessionID = new GID(1),
                    m_sessionSlot = 1,
                    m_targetPlayerID = owned_player().gid,
                    m_transitionID = 1
                }
            );

            return true;
        }

        public bool NetHandleRetryTeleport(GAME_5_Protocol.MSG_RETRYTELEPORT _msg)
        {
            respond(
                new GAME_5_Protocol.MSG_ZONETRANSFERREQUEST
                {
                    m_sendAck = 0,
                    m_zoneName = owned_player().zone_name
                }
            );

            return true;
        }

    }

}
