using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Open101.IO;
using SharpDX;

namespace Open101.Serializer.PropertyClass
{
    public static class SerializerBinary
    {
        public const uint BINd_MAGIC = 0x644E4942; // BINd
        public const uint BINd_PROPERTY_FLAGS = 7;

        public static PropertyClass ReadBINd(Stream stream)
        {
            // todo: leaveOpen support

            using ByteBuffer buffer = new ByteBuffer(stream);
            return ReadBINd(buffer);
        }
        
        public static PropertyClass ReadBINd(ByteBuffer buffer)
        {
            uint magic = buffer.ReadUInt32();
            if (magic != BINd_MAGIC) 
            {
                throw new InvalidDataException("not BINd magic");
            }
            uint flags = buffer.ReadUInt32();
                
            //Console.Out.WriteLine($"magic: {magic:X}");
            //Console.Out.WriteLine($"flags: {flags:X}");

            ByteBuffer objectBuffer = null;
            if ((flags & 8) != 0)
            {
                bool unk = buffer.ReadBit();
                if (unk)
                {
                    objectBuffer = SerializerFile.Decompress(buffer);
                }
            }
            objectBuffer ??= buffer;
            
            var inst = new SerializerBinaryInstance(objectBuffer, BINd_PROPERTY_FLAGS);
            var propClass = ReadObject(inst);
            objectBuffer.Dispose();

            return propClass;
        }

        public static void WriteBINd(ByteBuffer output, PropertyClass propertyClass, bool compressed=false)
        {
            output.WriteUInt32(BINd_MAGIC);
            if (compressed)
            {
                output.WriteUInt32(7 | 8); // = 15, idk?
                output.WriteBit(true);
                
                using var uncompressedBuf = new ByteBuffer();
                var inst = new SerializerBinaryInstance(uncompressedBuf, BINd_PROPERTY_FLAGS);
                WriteObject(inst, propertyClass);
                uncompressedBuf.GetCurrentStream().Position = 0;
                uncompressedBuf.SwapToRead();
                
                SerializerFile.CompressInto(output, uncompressedBuf);
            } else
            {
                output.WriteUInt32(7); // idk?
                var inst = new SerializerBinaryInstance(output, BINd_PROPERTY_FLAGS);
                WriteObject(inst, propertyClass);
            }
        }
        
        public static PropertyClass ReadObject(SerializerBinaryInstance inst, PropertyClass existing=null)
        {
            if (inst.m_useFlat)
            {
                return ReadObjectFlat(inst, existing);
            }

            return ReadObjectNormal(inst, existing);
        }
        
        public static void WriteObject(SerializerBinaryInstance inst, PropertyClass propertyClass)
        {
            var shouldWrite = inst.PreWriteObject(propertyClass);
            if (!shouldWrite) return;

            if (inst.m_useFlat)
            {
                propertyClass.SerializeBinary(inst);
            } else
            {
                inst.m_lastPropRemainingBits = 0;

                int startPos = inst.m_buffer.TellBitPos();
                
                int sizePos = inst.m_buffer.TellBitPos();
                Debug.Assert(sizePos % 8 == 0);
                int sizePosByte = sizePos >> 3;
                inst.m_buffer.WriteUInt32(0); // size temp
                
                propertyClass.SerializeBinary(inst);
                int endPos = inst.m_buffer.TellBitPos()-inst.m_lastPropRemainingBits;
                inst.m_buffer.FlushBits(); // todo: next data in this data MUST be aligned to a byte for this to make sense
                
                long afterEndFlushPos = inst.m_buffer.GetCurrentStream().Position;

                inst.m_buffer.GetCurrentStream().Position = sizePosByte;
                inst.m_buffer.WriteUInt32((uint)(endPos-startPos));

                inst.m_buffer.GetCurrentStream().Position = afterEndFlushPos;
            }
        }

        private static PropertyClass ReadObjectNormal(SerializerBinaryInstance inst, PropertyClass propertyClass)
        {
            var buffer = inst.m_buffer;
            var shouldRead = inst.PreLoadObject(ref propertyClass);

            if (!shouldRead)
            {
                return null;
            }
            
            int objectStart = buffer.TellBitPos();
            uint objectSize = buffer.ReadUInt32();
            
            if (propertyClass == null) // should have read object but failed
            {
                buffer.SeekBit((int) (objectStart + objectSize));
                return null;
            }
            
            //Console.Out.WriteLine($"typeHash: {typeHash:X}");
            //Console.Out.WriteLine($"objectSize: {objectSize:X} bits");
            
            while (buffer.TellBitPos() - objectStart < objectSize)
            {
                int propertyStart = buffer.TellBitPos();

                uint propertySize = buffer.ReadUInt32(); // property size, including padding to align to byte here
                uint propertyHash = buffer.ReadUInt32();

                long propDataStart = buffer.TellBitPos();
                long realPropSize = propertySize - (propDataStart - propertyStart);

                //Console.Out.WriteLine($"property {propIdx}:");
                //Console.Out.WriteLine($"    size: {propertySize:X} bits");
                //Console.Out.WriteLine($"    hash: {propertyHash:X}");

                //if (propertyHash == 2171167736)
                //{
                //    
                //}

                bool propResult = propertyClass.DeserializeBinaryField(inst, propertyHash);

                var readBits = buffer.TellBitPos() - propertyStart;
                if (!propResult)
                {
                    Console.Out.WriteLine($"{propertyClass.GetType().Name}: missing field {propertyHash:X} / {propertyHash} (size: {realPropSize})");
                } else if (readBits < propertySize)
                {
                    Console.Out.WriteLine($"{propertyClass.GetType().Name}: short read on {propertyHash:X} / {propertyHash} (size: {realPropSize}, read: {readBits-64})");
                } else if (readBits > propertySize)
                {
                    Console.Out.WriteLine($"{propertyClass.GetType().Name}: LONG READ ON {propertyHash:X} / {propertyHash} (size: {realPropSize}, read: {readBits-64})");
                }

                buffer.SeekBit((int) (propertyStart + propertySize));
            }

            buffer.SeekBit((int) (objectStart + objectSize));

            return propertyClass;
        }

        private static PropertyClass ReadObjectFlat(SerializerBinaryInstance inst, PropertyClass propertyClass)
        {
            var shouldRead = inst.PreLoadObject(ref propertyClass);

            if (!shouldRead)
            {
                return null;
            }
            if (propertyClass == null)
            {
                throw new Exception();
            }

            propertyClass.DeserializeBinaryFlat(inst);
            return propertyClass;
        }
        
        public static uint ReadOptimisedCount(ByteBuffer buffer)
        {
            if (buffer.ReadBit())
            {
                return buffer.ReadBits<uint>(31);
            }
            return buffer.ReadBits<uint>(7);
        }
        public static void WriteOptimisedCount(ByteBuffer buffer, int count)
        {
            if (count < 0) throw new InvalidEnumArgumentException(nameof(count));
            if (count >= 128)
            {
                buffer.WriteBit(1);
                buffer.WriteBits(count, 31);
            } else
            {
                
                buffer.WriteBit(0);
                buffer.WriteBits((byte)count, 7);
            }
        }

        public static void ReadBinaryEnum<T>(ByteBuffer buffer, int size, out T t) where T: struct, Enum
        {
            ulong raw;
            if (size == 4)
            {
                raw = buffer.ReadUInt32();
            } else
            {
                throw new NotImplementedException();
            }
            t = Unsafe.As<ulong, T>(ref raw);
        }
    }

    public class SerializerBinaryInstance
    {
        public ByteBuffer m_buffer;

        public uint m_allowedPropertyFlags;
        public bool m_flagFixedCountSize;
        public bool m_flagBinaryEnums;
        public bool m_useFlat = false; // 48

        public const int DEFAULT_ALLOWED_PROP_FLAGS = 0x18;

        public SerializerBinaryInstance(ByteBuffer buffer, uint allowedPropertyFlags=DEFAULT_ALLOWED_PROP_FLAGS)
        {
            m_buffer = buffer;
            m_allowedPropertyFlags = allowedPropertyFlags;
        }
        
        public virtual bool PreLoadObject(ref PropertyClass propertyClass)
        {
            uint typeHash = m_buffer.ReadUInt32();
            if (typeHash == 0)
            {
                propertyClass = null;
                return false;
            }
            propertyClass = AllocatePropertyClass(typeHash);
            return true;
        }

        public virtual bool PreWriteObject(PropertyClass propertyClass)
        {
            if (propertyClass == null)
            {
                m_buffer.WriteUInt32(0);
                return false;
            }
            var hash = propertyClass.GetHash();
            if (hash == 0) return false; // intentionally do not serialize
            m_buffer.WriteUInt32(hash);
            return true;
        }

        public byte m_lastPropRemainingBits;
        public void WriteProperty(uint hash, uint flags, Action action)
        {
            if (!m_useFlat)
            {
                int propStart = m_buffer.TellBitPos()-m_lastPropRemainingBits; // "rewind" to the bit end pos of the last field
            
                long sizePos = m_buffer.GetCurrentStream().Position; // will be byte aligned
                m_buffer.WriteUInt32(0); // sizeTemp
                m_buffer.WriteUInt32(hash);

                m_lastPropRemainingBits = 0;
                action(); // write prop
                
                int propEnd = m_buffer.TellBitPos();
                byte thisPropRemainingBits = (byte)(m_buffer.CurrentByteBitPos % 8); // get amount of bits next prop should add to size
                if (m_lastPropRemainingBits != 0)
                {
                    propEnd -= m_lastPropRemainingBits; // was an object, and didn't fill the full byte
                    thisPropRemainingBits = m_lastPropRemainingBits;
                }
                m_buffer.FlushBits(); // align here, for the next property

                long endPos = m_buffer.GetCurrentStream().Position;
                m_buffer.GetCurrentStream().Position = sizePos;
                
                m_buffer.WriteUInt32((uint)(propEnd-propStart)); // rewrite size
                m_buffer.GetCurrentStream().Position = endPos; // back to end
                
                m_lastPropRemainingBits = thisPropRemainingBits;
            } else
            {
                if ((flags & PropertyClass.PROPERTY_FLAGS_OPTIONAL) != 0)
                {
                    m_buffer.WriteBit(true);
                }
                action();
            }
        }

        protected PropertyClass AllocatePropertyClass(uint hash)
        {
            var propertyClass = PropertyClassRegistry.AllocateObject(hash);
            if (propertyClass == null)
            {
                Console.Out.WriteLine($"[SerializerBinary]: unknown property class {hash:X8}");
            }
            return propertyClass;
        }
        
        public PropertyClass ReadObject()
        {
            return SerializerBinary.ReadObject(this);
        }
        public void WriteObject(PropertyClass propertyClass)
        {
            SerializerBinary.WriteObject(this, propertyClass);
        }

        private uint ReadStringCount()
        {
            if (m_flagFixedCountSize) return m_buffer.ReadUInt16();
            return SerializerBinary.ReadOptimisedCount(m_buffer);
        }
        private void WriteStringCount(int count)
        {
            if (m_flagFixedCountSize) m_buffer.WriteUInt16((ushort) count);
            else SerializerBinary.WriteOptimisedCount(m_buffer, count);
        }

        private uint ReadVectorCount()
        {
            if (m_flagFixedCountSize) return m_buffer.ReadUInt32();
            return SerializerBinary.ReadOptimisedCount(m_buffer);
        }
        private void WriteVectorCount(int count)
        {
            if (m_flagFixedCountSize) m_buffer.WriteUInt32((uint)count);
            else SerializerBinary.WriteOptimisedCount(m_buffer, count);
        }
        
        private void ReadVectorEz<T>(Func<T> action, ref List<T> list)
        {
            Debug.Assert(list.Count == 0); // should be empty, special handling done by ReadObjectVector
            uint count = ReadVectorCount();
            for (int i = 0; i < count; i++)
            {
                list.Add(action());
            }
        }
        private void WriteVectorEz<T>(Action<T> action, IReadOnlyCollection<T> list)
        {
            if (list == null)
            {
                WriteVectorCount(0);
                return;
            }
            WriteVectorCount(list.Count);
            foreach (T t in list)
            {
                action(t);
            }
        }
        
        public void ReadEnumString<T>(out T val) where T: struct, Enum
        {
            SerializerFile.ParseEnumString(ReadString(), out val);
        }
        
        public void ReadEnum<T>(int size, out T t) where T: struct, Enum
        {
            if (m_flagBinaryEnums)
            {
                SerializerBinary.ReadBinaryEnum(m_buffer, size, out t);
                return;
            }
            ReadEnumString(out t);
        }
        public void WriteEnum<T>(int size, T t) where T: struct, Enum
        {
            if (m_flagBinaryEnums)
            {
                if (size == 4)
                {
                    var asUint = Unsafe.As<T, uint>(ref t);
                    m_buffer.WriteUInt32(asUint);
                } else
                {
                    throw new NotImplementedException($"binary enum write size {size}");
                }
                return;
            }
            throw new NotImplementedException();
        }

        #region String
        
        public string ReadString()
        {
            var strLength = ReadStringCount();
            if (strLength == 0) return string.Empty;
            string str = m_buffer.ReadString((int)strLength);
            return str;
        }
        public void ReadStringVector(ref List<string> list) => ReadVectorEz(ReadString, ref list);
        public void WriteString(string value)
        {
            if (value == null)
            {
                WriteStringCount(0);
                return;
            }
            WriteStringCount(value.Length);
            m_buffer.WriteString(value);
        }
        public void WriteStringVector(List<string> list) => WriteVectorEz(WriteString, list);

        #endregion

        #region WString
        
        public string ReadWString()
        {
            var strLength = ReadStringCount();
            if (strLength == 0) return string.Empty;
            var strBytes = m_buffer.ReadBytes((int)strLength * 2);
            var str = Encoding.Unicode.GetString(strBytes);
            return str;
        }
        public void ReadWStringVector(ref List<string> list) => ReadVectorEz(ReadWString, ref list);
        public void WriteWString(string value)
        {
            if (value == null)
            {
                WriteStringCount(0);
                return;
            }
            WriteStringCount(value.Length);

            var bytes = Encoding.Unicode.GetBytes(value);
            m_buffer.WriteBytes(bytes);
        }
        public void WriteWStringVector(List<string> list) => WriteVectorEz(WriteWString, list);

        #endregion

        public void ReadObjectVector<T>(ref List<T> list) where T: PropertyClass
        {
            uint count = ReadVectorCount();
            if (list.Count != 0)
            {
                Debug.Assert(list.Count == count);
                //int i = 0;
                foreach (T element in list)
                {
                    SerializerBinary.ReadObject(this, element);
                    //i++;
                }
            } else
            {
                for (int i = 0; i < count; i++)
                {
                    list.Add((T)SerializerBinary.ReadObject(this));
                }
            }
        }
        public void WriteObjectVector<T>(List<T> list) where T : PropertyClass => WriteVectorEz(WriteObject, list);
        
        public Rectangle ReadRectangle() => throw new NotImplementedException();
        public void ReadRectangleVector(ref List<Rectangle> list) => ReadVectorEz(ReadRectangle, ref list);
        public void WriteRectangle(Rectangle value) => throw new NotImplementedException();
        public void WriteRectangleVector(List<Rectangle> list) => WriteVectorEz(WriteRectangle, list);

        public RectangleF ReadRectangleF() => throw new NotImplementedException();
        public void ReadRectangleFVector(ref List<RectangleF> list) => ReadVectorEz(ReadRectangleF, ref list);
        public void WriteRectangleF(RectangleF value) => throw new NotImplementedException();
        public void WriteRectangleFVector(List<RectangleF> list) => WriteVectorEz(WriteRectangleF, list);
        
        public Vector2 ReadVector2() => new Vector2(m_buffer.ReadFloat(), m_buffer.ReadFloat());
        public void ReadVector2Vector(ref List<Vector2> list) => ReadVectorEz(ReadVector2, ref list);
        public void WriteVector2(Vector2 value) => throw new NotImplementedException();
        public void WriteVector2Vector(List<Vector2> list) => WriteVectorEz(WriteVector2, list);
        
        public Vector3 ReadVector3() => new Vector3(m_buffer.ReadFloat(), m_buffer.ReadFloat(), m_buffer.ReadFloat());
        public void ReadVector3Vector(ref List<Vector3> list) => ReadVectorEz(ReadVector3, ref list);
        public void WriteVector3(Vector3 value)
        {
            m_buffer.WriteFloat(value.X);
            m_buffer.WriteFloat(value.Y);
            m_buffer.WriteFloat(value.Z);
        }
        public void WriteVector3Vector(List<Vector3> list) => WriteVectorEz(WriteVector3, list);
        
        public Point ReadPoint() => new Point(m_buffer.ReadInt32(), m_buffer.ReadInt32());
        public void ReadPointVector(ref List<Point> list) => ReadVectorEz(ReadPoint, ref list);
        public void WritePoint(Point value) => throw new NotImplementedException();
        public void WritePointVector(List<Point> list) => WriteVectorEz(WritePoint, list);
        
        public Color ReadColor()
        {
            var b = m_buffer.ReadUInt8();
            var g = m_buffer.ReadUInt8();
            var r = m_buffer.ReadUInt8();
            var a = m_buffer.ReadUInt8();
            // todo: ORDER definitely is not rgba. see HumanTemplate - m_commonTable.m_hairColorList ???
            // bgra seems ok...
            return new Color(r, g, b, a);
        }
        public void WriteColor(Color value) => throw new NotImplementedException();

        public bool ReadBool() => BasicBinarySerializer.ReadBit(m_buffer);
        // ReadBoolVector - unused
        public void WriteBool(bool value) => BasicBinarySerializer.WriteBool(m_buffer, value);
        // WriteBoolVector - unused

        public sbyte ReadInt8() => BasicBinarySerializer.ReadInt8(m_buffer);
        // ReadInt8Vector - unused
        public void WriteInt8(sbyte value) => BasicBinarySerializer.WriteInt8(m_buffer, value);
        // WriteInt8Vector - unused

        public byte ReadUInt8() => BasicBinarySerializer.ReadUInt8(m_buffer);
        public void ReadUInt8Vector(ref List<byte> list) => ReadVectorEz(ReadUInt8, ref list);
        public void WriteUInt8(byte value) => BasicBinarySerializer.WriteUInt8(m_buffer, value);
        public void WriteUInt8Vector(List<byte> list) => WriteVectorEz(WriteUInt8, list);

        public short ReadInt16() => BasicBinarySerializer.ReadInt16(m_buffer);
        // ReadInt16Vector - unused
        public void WriteInt16(short value) => BasicBinarySerializer.WriteInt16(m_buffer, value);
        // WriteInt16Vector - unused

        public ushort ReadUInt16() => BasicBinarySerializer.ReadUInt16(m_buffer);
        public void ReadUInt16Vector(ref List<ushort> list) => ReadVectorEz(ReadUInt16, ref list);
        public void WriteUInt16(ushort value) => BasicBinarySerializer.WriteUInt16(m_buffer, value);
        public void WriteUInt16Vector(List<ushort> list) => WriteVectorEz(WriteUInt16, list);

        public int ReadInt32() => BasicBinarySerializer.ReadInt32(m_buffer);
        public void ReadInt32Vector(ref List<int> list) => ReadVectorEz(ReadInt32, ref list);
        public void WriteInt32(int value) => BasicBinarySerializer.WriteInt32(m_buffer, value);
        public void WriteInt32Vector(List<int> list) => WriteVectorEz(WriteInt32, list);

        public uint ReadUInt32() => BasicBinarySerializer.ReadUInt32(m_buffer);
        public void ReadUInt32Vector(ref List<uint> list) => ReadVectorEz(ReadUInt32, ref list);
        public void WriteUInt32(uint value) => BasicBinarySerializer.WriteUInt32(m_buffer, value);
        public void WriteUInt32Vector(List<uint> list) => WriteVectorEz(WriteUInt32, list);

        public ulong ReadUInt64() => BasicBinarySerializer.ReadUInt64(m_buffer);
        public void ReadUInt64Vector(ref List<ulong> list) => ReadVectorEz(ReadUInt64, ref list);
        public void WriteUInt64(ulong value) => BasicBinarySerializer.WriteUInt64(m_buffer, value);
        public void WriteUInt64Vector(List<ulong> list) => WriteVectorEz(WriteUInt64, list);

        public float ReadFloat() => BasicBinarySerializer.ReadFloat(m_buffer);
        public void ReadFloatVector(ref List<float> list) => ReadVectorEz(ReadFloat, ref list);
        public void WriteFloat(float value) => BasicBinarySerializer.WriteFloat(m_buffer, value);
        public void WriteFloatVector(List<float> list) => WriteVectorEz(WriteFloat, list);

        public double ReadDouble() => BasicBinarySerializer.ReadDouble(m_buffer);
        // ReadDoubleVector - unused
        public void WriteDouble(double value) => BasicBinarySerializer.WriteDouble(m_buffer, value);
        // WriteDoubleVector - unused

        public GID ReadGID() => BasicBinarySerializer.ReadGID(m_buffer);
        public void ReadGIDVector(ref List<GID> list) => ReadVectorEz(ReadGID, ref list);
        public void WriteGID(GID value) => BasicBinarySerializer.WriteGID(m_buffer, value);
        public void WriteGIDVector(List<GID> list) => WriteVectorEz(WriteGID, list);

        public byte ReadBitUInt2() => m_buffer.ReadBits<byte>(2);
        public void WriteBitUInt2(byte value) => m_buffer.WriteBits(value, 2);
        
        public byte ReadBitUInt4() => m_buffer.ReadBits<byte>(4);
        public void WriteBitUInt4(byte value) => m_buffer.WriteBits(value, 4);
        
        public byte ReadBitUInt5() => m_buffer.ReadBits<byte>(5);
        public void WriteBitUInt5(byte value) => m_buffer.WriteBits(value, 5);
        
        public byte ReadBitUInt7() => m_buffer.ReadBits<byte>(7);
        public void WriteBitUInt7(byte value) => m_buffer.WriteBits(value, 7);
    }
}