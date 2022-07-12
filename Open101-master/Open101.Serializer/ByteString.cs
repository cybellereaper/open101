using System.Text;

namespace Open101.Serializer
{
    public struct ByteString
    {
        private readonly byte[] m_backing;

        public ByteString(byte[] bytes)
        {
            m_backing = bytes;
        }

        public byte[] GetBytes() => m_backing;

        public static implicit operator string(ByteString dis)
        {
            return Encoding.UTF8.GetString(dis.m_backing);
        }
        
        public static implicit operator ByteString(string dis)
        {
            if (dis == null) return default; // empty
            return new ByteString(Encoding.UTF8.GetBytes(dis));
        }
        
        public static implicit operator ByteString(byte[] dis)
        {
            if (dis == null) return default; // empty
            return new ByteString(dis);
        }

        public override string ToString()
        {
            return this;
        }
    }
}