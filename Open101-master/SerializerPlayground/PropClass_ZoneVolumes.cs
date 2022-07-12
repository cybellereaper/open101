using System;
using System.Collections.Generic;
using System.Xml;
using Open101.Serializer;
using Open101.Serializer.PropertyClass;

namespace SerializerPlayground
{
    public class TriggerVolumeList : PropertyClass
    {
        public override uint GetHash() => 0;
        public override uint GetBehaviorInstanceHash() => 0;
        public override byte GetCoreType() => 0;
        
        [PropRefl(0x884BFB48)] public List<TriggerVolume> m_884BFB48 = new List<TriggerVolume>();

        public override bool DeserializeBinaryField(SerializerBinaryInstance buffer, uint hash)
        {
            if (base.DeserializeBinaryField(buffer, hash)) return true;
            return PropertyClassReflection.Cache<TriggerVolumeList>.DeserializeBinaryField(this, buffer, hash);
        }
        public override void DeserializeBinaryFlat(SerializerBinaryInstance serializer) => throw new NotImplementedException();
        public override bool DeserializeXMLField(XmlNode node) => throw new NotImplementedException();
        public override void SerializeBinary(SerializerBinaryInstance serializer) => throw new NotImplementedException();
    }

    public class TriggerVolume : CoreObjectInfo
    {
        public override uint GetHash() => 0;
        public override uint GetBehaviorInstanceHash() => 0;
        public override byte GetCoreType() => 0;

        [PropRefl(0xC6E6048B, "m_triggerObjName")] public string m_triggerObjName;
        [PropRefl(0x7DB3F828, "m_locationX")] public float m_locationX;
        [PropRefl(0x7DB3F829, "m_locationY")] public float m_locationY;
        [PropRefl(0x7DB3F82A, "m_locationZ")] public float m_locationZ;

        [PropRefl(0x40183401)] public GID m_templateID2;

        [PropRefl(0x8987B2CC)] public string m_8987B2CC;
        [PropRefl(0x3AF933DF)] public float m_radius;
        [PropRefl(0x2D481539)] public float m_length;

        [PropRefl(0x8576192E, "m_enterEvents")] public List<string> m_enterEvents = new List<string>();
        [PropRefl(0xAB57CF4A, "m_exitEvents")] public List<string> m_exitEvents = new List<string>();

        [PropRefl(0x35EBF597)] public float m_width;
        [PropRefl(0x3492258C)] public uint m_3492258C; // type unknown
        [PropRefl(0x3B3CD5DA)] public bool m_3B3CD5DA;
        [PropRefl(0x71FCB022)] public bool m_71FCB022;
        
        public override bool DeserializeBinaryField(SerializerBinaryInstance buffer, uint hash)
        {
            if (base.DeserializeBinaryField(buffer, hash)) return true;
            return PropertyClassReflection.Cache<TriggerVolume>.DeserializeBinaryField(this, buffer, hash);
        }
        public override void DeserializeBinaryFlat(SerializerBinaryInstance serializer) => throw new NotImplementedException();
        public override bool DeserializeXMLField(XmlNode node) => throw new NotImplementedException();
        public override void SerializeBinary(SerializerBinaryInstance serializer) => throw new NotImplementedException();
    }
}