using System;
using System.Xml;

namespace Open101.Serializer.PropertyClass
{
    public class PropertyClass
    {
        public const int PROPERTY_FLAGS_OPTIONAL = 0x100;
        public const int PROPERTY_FLAGS_C_ENUM = 0x100000;
        
        public virtual uint GetHash() => throw new NotImplementedException();
        public virtual uint GetBehaviorInstanceHash() => 0;
        public virtual byte GetCoreType() => 0;
        
        public virtual bool DeserializeBinaryField(SerializerBinaryInstance serializer, uint hash)
        {
            return false;
        }
        
        public virtual void DeserializeBinaryFlat(SerializerBinaryInstance serializer)
        {
        }

        public virtual bool DeserializeXMLField(XmlNode node)
        {
            return false;
        }
        
        public virtual void SerializeBinary(SerializerBinaryInstance serializer)
        {
        }
        
        public virtual PropertyClass CopyThis() => null;

        protected virtual void CopyTo(PropertyClass other)
        { }
    }
}