using Open101.IO;

namespace Open101.Serializer
{
    public static class BasicBinarySerializer
    {
        #region READ
        
        public static bool ReadBit(ByteBuffer buffer)
        {
            return buffer.ReadBit();
        }
        
        public static sbyte ReadInt8(ByteBuffer buffer)
        {
            return buffer.ReadInt8();
        }
        
        public static byte ReadUInt8(ByteBuffer buffer)
        {
            return buffer.ReadUInt8();
        }
        
        public static short ReadInt16(ByteBuffer buffer)
        {
            return buffer.ReadInt16();
        }
        
        public static ushort ReadUInt16(ByteBuffer buffer)
        {
            return buffer.ReadUInt16();
        }
        
        public static int ReadInt32(ByteBuffer buffer)
        {
            return buffer.ReadInt32();
        }
        
        public static uint ReadUInt32(ByteBuffer buffer)
        {
            return buffer.ReadUInt32();
        }
        
        public static long ReadInt64(ByteBuffer buffer)
        {
            return buffer.ReadInt64();
        }
        
        public static ulong ReadUInt64(ByteBuffer buffer)
        {
            return buffer.ReadUInt64();
        }

        public static float ReadFloat(ByteBuffer buffer)
        {
            return buffer.ReadFloat();
        }
        
        public static double ReadDouble(ByteBuffer buffer)
        {
            return buffer.ReadDouble();
        }
        
        public static GID ReadGID(ByteBuffer buffer)
        {
            return new GID(buffer.ReadUInt64());
        }
        
        #endregion

        #region WRITE
        
        public static void WriteBool(ByteBuffer buffer, bool val)
        {
            buffer.WriteBit(val);
        }

        public static void WriteInt8(ByteBuffer buffer, sbyte val)
        {
            buffer.WriteInt8(val);
        }
        
        public static void WriteUInt8(ByteBuffer buffer, byte val)
        {
            buffer.WriteUInt8(val);
        }
        
        public static void WriteInt16(ByteBuffer buffer, short val)
        {
            buffer.WriteInt16(val);
        }
        
        public static void WriteUInt16(ByteBuffer buffer, ushort val)
        {
            buffer.WriteUInt16(val);
        }
        
        public static void WriteInt32(ByteBuffer buffer, int val)
        {
            buffer.WriteInt32(val);
        }
        public static void WriteUInt32(ByteBuffer buffer, uint val)
        {
            buffer.WriteUInt32(val);
        }
        
        public static void WriteInt64(ByteBuffer buffer, long val)
        {
            buffer.WriteInt64(val);
        }
        public static void WriteUInt64(ByteBuffer buffer, ulong val)
        {
            buffer.WriteUInt64(val);
        }
        
        public static void WriteFloat(ByteBuffer buffer, float val)
        {
            buffer.WriteFloat(val);
        }
        
        public static void WriteDouble(ByteBuffer buffer, double val)
        {
            buffer.WriteDouble(val);
        }
        
        public static void WriteGID(ByteBuffer buffer, GID val)
        {
            buffer.WriteUInt64(val.m_value);
        }

        #endregion
    }
}