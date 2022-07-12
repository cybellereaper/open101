using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DragonLib.IO;
using Open101.IO;
using Open101.Serializer.DML;
using SerializerPlayground;

namespace Open101.Net
{
    public class KIPacketHandler : FoodFrameHandler
    {
        public const int c_serviceCount = 64;

        public static Dictionary<byte, INetworkService> s_services = new Dictionary<byte, INetworkService>
        {
            {0, new ControlNetworkService()},
            {1, new SYSTEM_1_Protocol()},
            {2, new EXTENDEDBASE_2_Protocol()},
            {5, new GAME_5_Protocol()},
            {7, new LOGIN_7_Protocol()},
            {8, new PATCH_8_Protocol()},
            {9, new PET_9_Protocol()},
            {10, new SCRIPT_10_Protocol()},
            {11, new TESTMANAGER_11_Protocol()},
            {12, new WIZARD_12_Protocol()},
            {15, new MOVEBEHAVIOR_15_Protocol()},
            {16, new PHYSICS_16_Protocol()},
            {19, new AISCLIENT_19_Protocol()},
            {25, new SOBLOCKS_MESSAGES_25_Protocol()},
            {40, new SKULLRIDERS_MESSAGES_40_Protocol()},
            {41, new DOODLEDOUG_MESSAGES_41_Protocol()},
            {42, new MG1_MESSAGES_42_Protocol()},
            {43, new MG2_MESSAGES_43_Protocol()},
            {44, new MG3_MESSAGES_44_Protocol()},
            {45, new MG4_MESSAGES_45_Protocol()},
            {46, new MG5_MESSAGES_46_Protocol()},
            {47, new MG6_MESSAGES_47_Protocol()},
            {50, new WIZARDHOUSING_50_Protocol()},
            {51, new WIZARDCOMBAT_MESSAGES_51_Protocol()},
            {52, new QUEST_MESSAGES_52_Protocol()},
            {53, new WIZARD2_53_Protocol()},
            {54, new MG9_MESSAGES_54_Protocol()}
        };
        
        public object[] m_serviceHandlers = new object[c_serviceCount];
        
        protected override void HandleFullMessage(byte[] buffer, int length)
        {
            using Stream memStream = new MemoryStream(buffer,0, length);
            using var byteBuf = new ByteBuffer(memStream);
            
            byte isControl = byteBuf.ReadUInt8();
            byte opCode = byteBuf.ReadUInt8();
            ushort unknown = byteBuf.ReadUInt16();
            
            INetworkService service;

            byte messageID;
            ushort messageLen;
            
            if (isControl != 0)
            {
                service = s_services[0];
                messageID = opCode;
            } else
            {
                Debug.Assert(opCode == 0);
                
                byte serviceID = byteBuf.ReadUInt8();
                messageID = byteBuf.ReadUInt8();

                if (!s_services.TryGetValue(serviceID, out service))
                {
                    Console.Out.WriteLine($"unknown service {serviceID}. msg {messageID}");
                    return;
                }
                messageLen = byteBuf.ReadUInt16();
            }
            
            var message = service.AllocateMessage(messageID);
            message.Deserialize(byteBuf);
            
            HandleMessage(message, service);
        }

        protected virtual void HandleMessage(INetworkMessage message, INetworkService service)
        {
            var handler = m_serviceHandlers[message.GetServiceID()];
            if (handler != null)
            {
                service.Dispatch(handler, message);
            }
        }

        public static void Serialize(ByteBuffer output, INetworkMessage message)
        {
            using var serializedBuffer = new ByteBuffer();
            message.Serialize(serializedBuffer);
            var serializedData = serializedBuffer.GetData();

            bool isControl = message.GetServiceID() == 0;

            using var fullBuffer = new ByteBuffer();
            if (isControl)
            {
                fullBuffer.WriteUInt8(1); // iscontrol
                fullBuffer.WriteUInt8(message.GetID());
            } else
            {
                fullBuffer.WriteUInt8(0); // iscontrol
                fullBuffer.WriteUInt8(0); // opcode
            }
            fullBuffer.WriteUInt16(0);
            if (!isControl)
            {
                fullBuffer.WriteUInt8(message.GetServiceID());
                fullBuffer.WriteUInt8(message.GetID());
                
                if (serializedData.Length+4 > ushort.MaxValue) throw new ArgumentException($"serializedData.Length+4 > ushort.MaxValue ({serializedData.Length+4}). message: {message.GetType()}");
                fullBuffer.WriteUInt16((ushort)(serializedData.Length+4)); // +4 = stuff in this code block
            }
            fullBuffer.WriteBytes(serializedData);
            fullBuffer.WriteUInt8(0);

            SerializeFoodFrame(output, fullBuffer);
        }
    }
    
    public class LoggingKIPacketHandler : KIPacketHandler
    {
        public string m_displayName;
        
        protected override void HandleMessage(INetworkMessage message, INetworkService service)
        {
            Logger.Info(m_displayName, $"Service: {service?.GetType().Name}, Message: {message?.GetType().Name}");
            base.HandleMessage(message, service);
        }
    }
}