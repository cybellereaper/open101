using System;
using System.Collections.Generic;
using System.Xml;
using Open101.Serializer;
using Open101.Serializer.PropertyClass;

namespace SerializerPlayground
{
    public class TriggerList : PropertyClass
    {
        public override uint GetHash() => 0;
        public override uint GetBehaviorInstanceHash() => 0;
        public override byte GetCoreType() => 0;
        
        [PropRefl(0x3F1DB764)] public List<Trigger> m_3F1DB764 = new List<Trigger>();
        
        public override bool DeserializeBinaryField(SerializerBinaryInstance buffer, uint hash)
        {
            if (base.DeserializeBinaryField(buffer, hash)) return true;
            return PropertyClassReflection.Cache<TriggerList>.DeserializeBinaryField(this, buffer, hash);
        }
        public override void DeserializeBinaryFlat(SerializerBinaryInstance serializer) => throw new NotImplementedException();
        public override bool DeserializeXMLField(XmlNode node) => throw new NotImplementedException();
        public override void SerializeBinary(SerializerBinaryInstance serializer) => throw new NotImplementedException();
    }
    
    public class Trigger : PropertyClass
    {
        public override uint GetHash() => 0;
        public override uint GetBehaviorInstanceHash() => 0;
        public override byte GetCoreType() => 0;
        
        [PropRefl(0xB8C90C10, "m_triggerName")] public string m_triggerName;

        [PropRefl(0xE11C8ADA)] public ResultList m_E11C8ADA;
        [PropRefl(0xA955FFA6)] public RequirementList m_A955FFA6;

        [PropRefl(0x7DB09CC1)] public List<string> m_7DB09CC1 = new List<string>();
        [PropRefl(0xA7BEADF6)] public List<string> m_A7BEADF6 = new List<string>();
        [PropRefl(0x62A2160A)] public List<string> m_62A2160A = new List<string>();

        [PropRefl(0x8177DA98, "m_triggerObjectInfo")] public TriggerObjectInfo m_triggerObjectInfo;

        [PropRefl(0x3933D634)] public int m_3933D634;
        [PropRefl(0x767AAC3C)] public uint m_767AAC3C; // signed unknown
        [PropRefl(0x2E8B9981)] public uint m_2E8B9981; // signed unknown
        [PropRefl(0x3282D78A)] public bool m_3282D78A;
        [PropRefl(0x5C548D5F)] public byte m_5C548D5F;
        [PropRefl(0x794EA0DF)] public uint m_794EA0DF; // signed unknown
        [PropRefl(0x88B9D287)] public byte m_88B9D287;

        public override bool DeserializeBinaryField(SerializerBinaryInstance buffer, uint hash)
        {
            if (base.DeserializeBinaryField(buffer, hash)) return true;
            return PropertyClassReflection.Cache<Trigger>.DeserializeBinaryField(this, buffer, hash);
        }
        public override void DeserializeBinaryFlat(SerializerBinaryInstance serializer) => throw new NotImplementedException();
        public override bool DeserializeXMLField(XmlNode node) => throw new NotImplementedException();
        public override void SerializeBinary(SerializerBinaryInstance serializer) => throw new NotImplementedException();
    }

    public class TriggerObjectInfo : CoreObjectInfo
    {
        public override uint GetHash() => 0;
        public override uint GetBehaviorInstanceHash() => 0;
        public override byte GetCoreType() => 0;
        
        // PropClass_2147D190: missing field C6E6048B / 3336963211 (size: 176)
        // PropClass_2147D190: missing field 7DB3F828 / 2108946472 (size: 32)
        // PropClass_2147D190: missing field 7DB3F829 / 2108946473 (size: 32)
        // PropClass_2147D190: missing field 7DB3F82A / 2108946474 (size: 32)
        // PropClass_2147D190: missing field 40183401 / 1075328001 (size: 64)

        // what the heck, same as volume
        [PropRefl(0xC6E6048B, "m_triggerObjName")] public string m_triggerObjName;
        [PropRefl(0x7DB3F828, "m_locationX")] public float m_locationX;
        [PropRefl(0x7DB3F829, "m_locationY")] public float m_locationY;
        [PropRefl(0x7DB3F82A, "m_locationZ")] public float m_locationZ;
        [PropRefl(0x40183401)] public GID m_templateID2;
        
        public override bool DeserializeBinaryField(SerializerBinaryInstance buffer, uint hash)
        {
            if (base.DeserializeBinaryField(buffer, hash)) return true;
            return PropertyClassReflection.Cache<TriggerObjectInfo>.DeserializeBinaryField(this, buffer, hash);
        }
        public override void DeserializeBinaryFlat(SerializerBinaryInstance serializer) => throw new NotImplementedException();
        public override bool DeserializeXMLField(XmlNode node) => throw new NotImplementedException();
        public override void SerializeBinary(SerializerBinaryInstance serializer) => throw new NotImplementedException();
    }
}