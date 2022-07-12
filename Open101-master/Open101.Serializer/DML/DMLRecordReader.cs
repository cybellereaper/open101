using System;
using System.Xml;
using Open101.IO;

namespace Open101.Serializer.DML
{
    public static class DMLRecordReader<T> where T: new()
    {
        private static readonly Action<T, XmlNode> s_readXml = DMLReader.CreateReadXMLFunc<T>();
        private static readonly Action<T, ByteBuffer> s_readBinary = DMLReader.CreateReadBinaryFunc<T>();
        
        private static readonly Action<ByteBuffer, T> s_writeBinary = DMLReader.CreateWriteBinaryFunc<T>();

        public static T Read(XmlNode recordNode)
        {
            var t = new T();
            s_readXml(t, recordNode.FirstChild);
            return t;
        }
        
        public static T Read(ByteBuffer buffer)
        {
            var t = new T();
            Read(buffer, t);
            return t;
        }

        public static void Read(ByteBuffer buffer, T t)
        {
            s_readBinary(t, buffer);
        }
        
        public static void Write(ByteBuffer buffer, T t)
        {
            s_writeBinary(buffer, t);
        }
    }
}