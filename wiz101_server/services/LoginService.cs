using Open101.IO;
using Open101.Net;
using Open101.Serializer;
using Open101.Serializer.PropertyClass;
using SerializerPlayground;
using System;
using System.Collections.Generic;
using System.Linq;
using wiz101_server.sockets;

namespace wiz101_server.services
{
    public class LoginServiceHandler : ServiceHandler, LOGIN_7_Protocol.Handler
    {

        public PlayerData.LoginPlayerStruct login_player_struct;

        public LoginServiceHandler(KISocket _sock) : base(_sock) { }

        public bool NetHandleUserValidate(LOGIN_7_Protocol.MSG_USER_VALIDATE _msg)
        {
            m_socket.Send(
                new LOGIN_7_Protocol.MSG_USER_VALIDATE_RSP
                {
                    m_error = 0,
                    m_reason = string.Empty,
                    m_userID = _msg.m_userID,
                    m_timeStamp = string.Empty,
                    m_payingUser = 1,
                    m_flags = 0
                },
                new LOGIN_7_Protocol.MSG_USER_ADMIT_IND
                {
                    m_status = 1,
                    m_positionInQueue = 0
                }
            );

            login_player_struct = new PlayerData.LoginPlayerStruct { characters = new List<CharacterCreationInfo>(), user_id = _msg.m_userID };

            foreach (var cur_character in LocalRealm.realm.get_cached_characters_for_player(login_player_struct.user_id.m_value))
            {
                login_player_struct.characters.Add(cur_character);
            }

            return true;
        }

        public bool NetHandleRequestCharacterList(LOGIN_7_Protocol.MSG_REQUESTCHARACTERLIST _msg)
        {
            m_socket.Send(
                new LOGIN_7_Protocol.MSG_STARTCHARACTERLIST
                {
                    m_loginServer = "Server_Login",
                    m_purchasedCharacterSlots = 0
                }
            );

            //foreach (var character in login_player_struct.characters)
            foreach (var character in LocalRealm.realm.get_cached_characters_for_player(login_player_struct.user_id.m_value))
            {
                var char_buff = new ByteBuffer();
                var newInst = new SerializerBinaryInstance(char_buff)
                {
                    m_useFlat = true,
                    m_flagFixedCountSize = true,
                    m_flagBinaryEnums = true
                };

                newInst.WriteObject(character);

                m_socket.Send(
                    new LOGIN_7_Protocol.MSG_CHARACTERINFO 
                    {
                        m_characterInfo = char_buff.GetData()
                    }
                );
            }

            m_socket.Send(new LOGIN_7_Protocol.MSG_CHARACTERLIST { m_error = 0 });
            
            return true;
        }

        public bool NetHandleCreateCharacter(LOGIN_7_Protocol.MSG_CREATECHARACTER _msg)
        {
            var plbuf = new ByteBuffer(_msg.m_creationInfo.GetBytes());
            var inst = new SerializerBinaryInstance(plbuf)
            {
                m_useFlat = true,
                m_flagFixedCountSize = true,
                m_flagBinaryEnums = true
            };

            var propClass = (WizardCharacterCreationInfo)SerializerBinary.ReadObject(inst);
            propClass.m_globalID = Util.get_next_gid();
            propClass.m_level = 1;

            LocalRealm.realm.cache_character_creation_info(propClass.m_globalID.m_value, login_player_struct.user_id.m_value, propClass);

            login_player_struct.characters.Add(propClass);

            m_socket.Send(
                new LOGIN_7_Protocol.MSG_CREATECHARACTERRESPONSE
                {
                    m_errorCode = 0
                }
            );

            return true;
        }

        public bool NetHandleSelectCharacter(LOGIN_7_Protocol.MSG_SELECTCHARACTER _msg)
        {
            m_socket.Send(
                new LOGIN_7_Protocol.MSG_CHARACTERSELECTED
                {
                    m_IP = Program.GAME_IP.ToString(),
                    m_TCPPort = Program.GAME_PORT,
                    m_UDPPort = 0,
                    m_key = string.Empty,
                    m_userID = login_player_struct.user_id,//new GID(4295088136144),
                    m_charID = _msg.m_charID,//new GID(191965934135706025),
                    m_zoneID = new GID(123004564835992122),
                    m_zoneName = "WizardCity/WC_Ravenwood",
                    m_location = "Start",
                    m_slot = 0,
                    m_prepPhase = 0,
                    m_error = 0,
                    m_loginServer = "Server"
                }
            );

            return true;
        }

        //public bool NetHandleUserAuthenV3(LOGIN_7_Protocol.MSG_USER_AUTHEN_V3 _msg)
        //{
        //    m_socket.Send(
        //        new LOGIN_7_Protocol.MSG_USER_AUTHEN_RSP
        //        {
        //            m_error = 0,
        //            m_flags = 0,
        //            m_payingUser = 1,
        //            m_reason = string.Empty,
        //            m_rec1 = Util.hex_string_to_bytes("AE BD 27 4D E1 D5 92 7F 23 91 54 70 CF B5 75 E1 A4 66 A2 47 6B BD 47 1B F1 5C 7B 5D 72 34 D4 3E 9F 39 14 D9 53 27 4B DA 68 B4 B5 78 31 22 EC F7 FA DC 9B 91 B3 1E 58 2C E3 1A CD 97 37 29 15 CC C0 94 96 35 AF 92 BA 9E BB A0 06 00 2C A6 03 AF 16 81 74 B1 A7 B5 EC C1"),
        //            m_timeStamp = string.Empty,
        //            m_userID = new GID()
        //        }
        //    );

        //    return true;
        //}

    }

}
