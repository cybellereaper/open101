using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Open101.IO;
using Open101.Serializer;
using Open101.Serializer.PropertyClass;
using SerializerPlayground;

namespace wiz101_server
{

    public static class Util
    {

        public static bool are_bytes_equal(List<Byte> _a, List<Byte> _b)
        {
            if (_a.Count != _b.Count)
                return false;
            for (int i = 0; i < _a.Count; ++i)
            {
                if (_a[i] != _b[i])
                    return false;
            }
            return true;
        }

        public static string get_chat_message(List<Byte> _msg)
        {
            var msg_str = _msg.GetRange(2, _msg.Count - 2);
            var msg_len_pos = msg_str.IndexOf(0x00);
            var msg_len = _msg.GetRange(0, msg_len_pos)[0] -= 1;
            return System.Text.Encoding.Unicode.GetString(msg_str.ToArray());
        }

        public static Byte[] hex_string_to_bytes(string _hexstring)
        {
            string data = _hexstring.Replace(" ", "");
            Byte[] str_bytes =
                Enumerable.Range(0, data.Length)
                    .Where(x => x % 2 == 0)
                    .Select(x => Convert.ToByte(data.Substring(x, 2), 16))
                    .ToArray();
            return str_bytes;
        }

        public static Byte[] Decompress(Byte[] _bytes)
        {
            MemoryStream res_stream = new MemoryStream();
            using (MemoryStream mem_stream = new MemoryStream(_bytes))
            {
                using (InflaterInputStream inflater = new InflaterInputStream(mem_stream))
                {
                    inflater.CopyTo(res_stream);
                }
            }
            return res_stream.ToArray();
        }

        public static Byte[] Compress(Byte[] _bytes)
        {
            Deflater deflater = new Deflater(Deflater.BEST_COMPRESSION, false);
            deflater.SetInput(_bytes);
            deflater.Finish();

            using (var ms = new MemoryStream())
            {
                var outputBuffer = new byte[65536 * 4];
                while (deflater.IsNeedingInput == false)
                {
                    var read = deflater.Deflate(outputBuffer);
                    ms.Write(outputBuffer, 0, read);

                    if (deflater.IsFinished == true)
                        break;
                }

                deflater.Reset();

                return ms.ToArray();
            }
        }

        private static Int64 cur_gid = 0;
        public static GID get_next_gid() // Very simple, but should work. Better than random, as it avoids collision.
        {
            Interlocked.Increment(ref cur_gid);
            return new GID(unchecked((UInt64)cur_gid++));
        }

        public static WizClientObject get_fallback_character(uint _flags)
        {
            var orig_data = Util.hex_string_to_bytes("7C09000078DACB606264606050600081531E9E854C40BA98B34588818189010164F85E07318259B14BEF59AAAC7B5D0562338345CE8BA5B33330BC6664C0091AEC41E6317032100D98C1AEF8D422C68030F62211F6807490620F4829C49FDB3F27E7C27CD2D2A2021581D999F9592D1DA2CE25A5D819EC1B14001205DB8DE993D9D37D40740010A78045CEB6B6EB3030785A374A4054087CBF5A0C53FDEB505F228CBD30FBA7EE66B31F2C6A4036AAA7458525E31818D607154A40C427BFE5CB07C5091B98B752F2750E441D7BEF85228B06864016C1060E10BF63DF411698872B660BDA32305838EA43CC064989338467562516A53825A6A4A716C7BBFA068444867886F8B86EAE8686D62B8F586028DF3E9B390DEE1B08B87639539E814138EA690103C33B972B3170F76A6086C8BEE86D2EA8227B66385A33304432E95532C0FD9AD1FDD81093F79C1911D708FD378C6DCB409E7AA67A4D0BDD36B4E4126F502C8B6E5C42608105038329DF4C161CA96446EF84520686968E6DB5C030EDDFE20D4CF30BB6017D2AFFE19E32034398243B3F426DA0535601886E2EC8025ABD0AE8AC86030C0C078E3030181C82A9494B737300F900EC36269498656884EB93B91EE83E01C80619FE60320B43873024CC41410A1247D6C8CF402960440A2B0106CAC1A819A366D0DA8CD130A42D8017B62C29F4B55C6090998304D849507B23D6D01D8441E1E8F3AC2664C68168DF736F0C2461F20060D1961C00");
            var decompressed_data = Util.Decompress(orig_data.Skip(4).ToArray());
            var plbuf = new ByteBuffer(decompressed_data);
            var inst = new SerializerCoreDataBinaryInstance(plbuf, _flags);
            var result = (WizClientObject)SerializerBinary.ReadObject(inst);
            return result;
        }

        public static WizardCharacterCreationInfo get_default_character_info()
        {
            return new WizardCharacterCreationInfo
            {
                m_level = 1,
                m_schoolOfFocus = 0x04f836b3,
                m_location = "WizardCity/WC_Ravenwood",
                m_templateID = 1,
                m_nameIndices = 0,
                m_avatarBehavior = new WizardCharacterBehavior
                {
                    m_eGender = eGender.Neutral,
                    m_eRace = eRace.Human
                }
            };
        }

        // This function just modifies a already existing character for now. Not very good. Should be loaded/generated from scratch
        public static WizClientObject character_creation_data_to_game_data(WizardCharacterCreationInfo _info, GID _char_id, uint _flags)
        {
            var result = new WizClientObject();
            SerializerCoreDataBinaryInstance.InitCoreObjectFromTemplate(result, (uint)_info.m_templateID); // This is just for fun. It is super unsafe

            try_replace_behavior_instance<WizardCharacterBehavior>(result.m_inactiveBehaviors, _info.m_avatarBehavior);

            if (_info.m_equipmentInfoList != null)
            {
                ClientWizEquipmentBehavior clientWizEquipBehaviour;
                if (find_behavior_instance(result.m_inactiveBehaviors, out clientWizEquipBehaviour))
                {
                    clientWizEquipBehaviour.m_publicItemList = new List<EquippedItemInfo>();
                    foreach (var item_to_add in _info.m_equipmentInfoList.m_infoList)
                    {
                        clientWizEquipBehaviour.m_publicItemList.Add(item_to_add);
                    }
                }
            }

            ClientMagicSchoolBehavior clientMagicSchoolBehavior;
            if (find_behavior_instance(result.m_inactiveBehaviors, out clientMagicSchoolBehavior))
            {
                clientMagicSchoolBehavior.m_level = _info.m_level;
                clientMagicSchoolBehavior.m_schoolOfFocus = _info.m_schoolOfFocus;
            }

            ClientWizPlayerNameBehavior clientWizPlayerNameBehavior;
            if (find_behavior_instance(result.m_inactiveBehaviors, out clientWizPlayerNameBehavior))
            {
                clientWizPlayerNameBehavior.m_eGender = _info.m_avatarBehavior.m_eGender;
                clientWizPlayerNameBehavior.m_eRace = _info.m_avatarBehavior.m_eRace;
                clientWizPlayerNameBehavior.m_nameKeys = _info.m_nameIndices;
            }

            result.m_globalID = _info.m_globalID;
            result.m_characterId = _char_id;
            result.m_templateID = new GID((ulong)_info.m_templateID); // unsafe

            return result;
        }

        public static PlayerData.InGameCharacterStruct create_player_instance(WizardCharacterCreationInfo _info, GID _char_id, uint _flags)
        {
            return new PlayerData.InGameCharacterStruct
            {
                character_object = character_creation_data_to_game_data(_info, _char_id, _flags),

                gid = _info.m_globalID,//_gid,
                charID = _char_id,//new GID(191965934135706025),
                zone_name = "WizardCity/WC_Ravenwood",
                zoneID = new GID(123004564835992122),

                x = 1132.0f,
                y = 3,
                z = 3,
                direction = 0,

                markerX = 0,
                markerY = 0,
                markerZ = 0,
                markerDirection = 0,
                marker_zoneID = new GID(123004564835992122),
                marker_zone_name = "WizardCity/WC_Ravenwood",

                curMana = 15,
                maxMana = 15,
                inventory = create_player_inventory(),

                initialized = true
            };
        }

        public static PlayerData.InGameCharacterStruct create_default_player_instance(GID _gid, uint _flags)
        {
            return new PlayerData.InGameCharacterStruct
            {
                character_object = get_fallback_character(_flags),

                gid = _gid,//_gid,
                charID = get_next_gid(),//new GID(191965934135706025),
                zone_name = "WizardCity/WC_Ravenwood",
                zoneID = new GID(123004564835992122),

                x = 1132.0f,
                y = 3,
                z = 3,
                direction = 0,

                markerX = 0,
                markerY = 0,
                markerZ = 0,
                markerDirection = 0,
                marker_zoneID = new GID(123004564835992122),
                marker_zone_name = "WizardCity/WC_Ravenwood",

                curMana = 15,
                maxMana = 15,
                inventory = create_player_inventory(),

                initialized = true
            };
        }
        public static ClientWizInventoryBehavior create_player_inventory()
        {
            var inventory = new ClientWizInventoryBehavior();
            //we'll need to serialize stuff here and give players legit default stuff
            return inventory;
        }

        public static byte[] serialize_player_prop(WizClientObject _obj, ushort _mobile_id, bool _new_obj)
        {
            var new_player_buffer = new ByteBuffer();
            byte[] new_player_data;

            var newInst = new SerializerCoreDataBinaryInstance(new_player_buffer, _new_obj ? (uint)0x1C : (uint)0x18);
            newInst.WriteObject(_obj);

            new_player_buffer.SwapToRead();
            new_player_buffer.GetCurrentStream().Position = 0;

            if (_new_obj)
            {
                return new_player_buffer.GetData();
            }
            else
            {
                int uncompressed_size = new_player_buffer.GetData().Length;

                var compressed_data = Util.Compress(new_player_buffer.GetData());

                new_player_data = new byte[compressed_data.Length + 4];

                Buffer.BlockCopy(BitConverter.GetBytes(uncompressed_size), 0, new_player_data, 0, 4);
                Buffer.BlockCopy(compressed_data, 0, new_player_data, 4, compressed_data.Length);

                return new_player_data;
            }
        }
        public static byte[] serialize_item_prop(CoreObject _obj, ushort _mobile_id, bool _new_obj)
        {
            var new_player_buffer = new ByteBuffer();
            byte[] new_player_data;

            var newInst = new SerializerCoreDataBinaryInstance(new_player_buffer, _new_obj ? (uint)0x1C : (uint)0x18);
            newInst.WriteObject(_obj);

            new_player_buffer.SwapToRead();
            new_player_buffer.GetCurrentStream().Position = 0;

            if (_new_obj)
            {
                return new_player_buffer.GetData();
            }
            else
            {
                int uncompressed_size = new_player_buffer.GetData().Length;

                var compressed_data = Util.Compress(new_player_buffer.GetData());

                new_player_data = new byte[compressed_data.Length + 4];

                Buffer.BlockCopy(BitConverter.GetBytes(uncompressed_size), 0, new_player_data, 0, 4);
                Buffer.BlockCopy(compressed_data, 0, new_player_data, 4, compressed_data.Length);

                return new_player_data;
            }
        }

        public static bool find_behavior_instance<T>(List<BehaviorInstance> _behaviors, out T _output)
            where T : BehaviorInstance
        {
            foreach (var b in _behaviors)
            {
                if (b is T)
                {
                    _output = (T)b;
                    return true;
                }
            }

            _output = default;
            return false;
        }

        public static void try_replace_behavior_instance<T>(List<BehaviorInstance> _behaviors, BehaviorInstance _new_behavior)
            where T : BehaviorInstance
        {
            for (int i = 0; i < _behaviors.Count; i++)
            {
                if (_behaviors[i] is T)
                {
                    _behaviors[i] = _new_behavior;
                }
            }
        }

    }

}
