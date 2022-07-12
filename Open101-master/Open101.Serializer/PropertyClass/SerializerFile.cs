using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq.Expressions;
using System.Xml;
using Ionic.Zlib;
using Open101.IO;

namespace Open101.Serializer.PropertyClass
{
    public static class SerializerFile
    {
        public static T OpenClass<T>(Wad wad, string file) where T: PropertyClass
        {
            using (var stream = wad.OpenFile(file))
            {
                if (stream == null) return null;
                return (T)Load(stream);
            }
        }
        
        public static T OpenClass<T>(string file) where T: PropertyClass
        {
            using (var stream = ResourceManager.OpenFile(file))
            {
                if (stream == null) return null;
                return (T)Load(stream);
            }
        }

        public static PropertyClass Load(Stream stream)
        {
            Span<byte> fourCCBytes = stackalloc byte[4];
            stream.Read(fourCCBytes);
            uint fourCC = BitConverter.ToUInt32(fourCCBytes);

            stream.Position -= 4;
            
            if (fourCC == SerializerBinary.BINd_MAGIC)
            {
                return SerializerBinary.ReadBINd(stream);
            }
            XmlDocument xmlDoc= new XmlDocument(); // Create an XML document object
            xmlDoc.Load(stream); // Load the XML document from the specified file
            var objectCollection = xmlDoc["Objects"];
            return SerializerXML.ReadObject(objectCollection);
        }
        
        public static string CleanOptionName(string value)
        {
            return value.Replace(" ", string.Empty).Replace("-", string.Empty);
        }

        public static void ParseEnumString<T>(string str, out T val) where T: struct, Enum
        {
            if (string.IsNullOrEmpty(str))
            {
                throw new InvalidDataException("enum with empty string, talk to zingy");
                // pfft. idk
                val = default;
                return;
            }
            
            str = CleanOptionName(str);
            if (str.Contains("|"))
            {
                val = default;
                foreach (string s in str.Split("|"))
                {
                    val = EnumHelper<T>.OrFunction(val, Enum.Parse<T>(s));
                }
                return;
            }

            var scopeIdx = str.LastIndexOf("::", StringComparison.Ordinal);
            if (scopeIdx != -1)
            {
                str = str.Substring(scopeIdx+2); // after the scope only, used on old files
            }
            val = Enum.Parse<T>(str);
        }
        
        public static uint HashString(string input)
        {
            int result = 0;

            var shift1 = 0;
            var shift2 = 32;
            foreach (char c in input)
            {
                var cb = (byte) c;
                
                result ^= (cb - 32) << shift1;
                
                if ( shift1 > 24 )
                {
                    result ^= (cb - 32) >> shift2;
                    if ( shift1 >= 27 )
                    {
                        shift1 -= 32;
                        shift2 += 32;
                    }
                }
                shift1 += 5;
                shift2 -= 5;
            }

            if (result < 0)
            {
                result = -result;
            }

            return (uint)result;
        }

        public static uint HashPropertyName(string name, string typeName)
        {
            uint typeHash = HashString(typeName);
            var propHash = HashPropertyNameInternal(name);
            return propHash + typeHash;
        }
        
        public static uint HashPropertyNameInternal(string input)
        {
            // todo: final hash value is added to something
            
            if (input.Length == 0) return 0;
            
            var output = 5381;
            foreach (char c in input)
            {
                var cb = (byte) c;
                var temp = 33 * output;
                output = temp + cb;
            }
            return (uint)(output & 0x7FFFFFFFu);
        }
        
        public static byte[] Compress(byte[] bytes) 
        {
            var compressed = ZlibStream.CompressBuffer(bytes);
            using var memStream = new MemoryStream(compressed.Length+4);
            using var writer = new BinaryWriter(memStream);
            writer.Write(bytes.Length);
            writer.Write(compressed);
            return memStream.GetBuffer();
        }
        
        public static void CompressInto(ByteBuffer output, ByteBuffer input) 
        {
            output.WriteUInt32((uint)input.GetCurrentStream().Length);
            using (var stream = new ZlibStream(input.GetCurrentStream(), CompressionMode.Compress))
            {
                stream.CopyTo(output.GetCurrentStream());
            }
        }

        public static ByteBuffer Decompress(ByteBuffer buffer)
        {
            var decompressedSize = buffer.ReadUInt32();
            byte[] outBytes = new byte[decompressedSize];
            using (var stream = new ZlibStream(buffer.GetCurrentStream(), CompressionMode.Decompress))
            {
                int read = stream.Read(outBytes);
                if (read != decompressedSize) throw new EndOfStreamException($"Decompress: expected {decompressedSize} bytes, got {read}");
            }
            return new ByteBuffer(outBytes);
        }
    }

    // https://stackoverflow.com/questions/27845420/bitwise-or-ing-enums-using-generics
    [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
    public class EnumHelper<T> where T : struct, IConvertible
    {
        private static readonly Type s_typeofT = typeof(T);
        private static readonly Type s_underlyingType = Enum.GetUnderlyingType(s_typeofT);

        private static readonly ParameterExpression[] s_parameters =
        {
            Expression.Parameter(s_typeofT),
            Expression.Parameter(s_typeofT)
        };

        public static Func<T, T, T> OrFunction { get; } = Expression.Lambda<Func<T, T, T>>(
            Expression.Convert(Expression.Or(
                Expression.Convert(s_parameters[0], s_underlyingType),
                Expression.Convert(s_parameters[1], s_underlyingType)
            ), s_typeofT), s_parameters).Compile();
    }
}