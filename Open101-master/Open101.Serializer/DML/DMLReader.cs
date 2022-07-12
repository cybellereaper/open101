using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Xml;
using Open101.IO;
using Open101.Serializer.PropertyClass;

namespace Open101.Serializer.DML
{
    public enum DMLType
    {
        Invalid = 0,
        GID = 1,
        INT = 2,
        UINT = 3,
        FLT = 4,
        BYT = 5,
        
        UBYT = 6,
        UBYTE = 6, // MSG_PVPMATCHREQUEST::Failure
        
        USHRT = 7,
        DBL = 8,
        STR = 9,
        WSTR = 10
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class DMLField : Attribute
    {
        public string m_name;
        public DMLType m_type;
            
        public DMLField(string name, DMLType type)
        {
            m_name = name;
            m_type = type;
        }
    }
    public static class DMLReader
    {
        private static readonly Dictionary<DMLType, MethodInfo> s_xmlTypeParserMethods = new Dictionary<DMLType, MethodInfo>
        {
            // todo: fix unsigned being stored as signed in SerializerXML for PropertyClasses, and then move methods to this type
            {DMLType.BYT, typeof(SerializerXML).GetMethod(nameof(SerializerXML.ReadInt8))},
            {DMLType.UBYT, typeof(SerializerXML).GetMethod(nameof(SerializerXML.ReadUInt8))},
            //{DMLType.SHRT, typeof(SerializerXML).GetMethod(nameof(SerializerXML.ReadInt16))},
            {DMLType.USHRT,typeof(SerializerXML).GetMethod(nameof(SerializerXML.ReadUInt16))},
            {DMLType.INT, typeof(SerializerXML).GetMethod(nameof(SerializerXML.ReadInt32))},
            {DMLType.UINT, typeof(SerializerXML).GetMethod(nameof(SerializerXML.ReadUInt32))},
            {DMLType.STR, ((Func<XmlNode, ByteString>)ReadXMLByteString).Method},
            {DMLType.WSTR, ((Func<XmlNode, string>)SerializerXML.ReadWString).Method},
            {DMLType.FLT, typeof(SerializerXML).GetMethod(nameof(SerializerXML.ReadFloat))},
            {DMLType.DBL, typeof(SerializerXML).GetMethod(nameof(SerializerXML.ReadDouble))},
            {DMLType.GID, ((Func<XmlNode, GID>)ReadXmlGID).Method}
        };
        
        private static readonly Dictionary<DMLType, MethodInfo> s_binaryTypeParserMethods = new Dictionary<DMLType, MethodInfo>
        {
            {DMLType.BYT, ((Func<ByteBuffer, sbyte>)BasicBinarySerializer.ReadInt8).Method},
            {DMLType.UBYT, ((Func<ByteBuffer, byte>)BasicBinarySerializer.ReadUInt8).Method},
            //{DMLType.SHRT, ((Func<ByteBuffer, short>)BasicBinarySerializer.ReadInt16).Method},
            {DMLType.USHRT, ((Func<ByteBuffer, ushort>)BasicBinarySerializer.ReadUInt16).Method},
            {DMLType.INT, ((Func<ByteBuffer, int>)BasicBinarySerializer.ReadInt32).Method},
            {DMLType.UINT, ((Func<ByteBuffer, uint>)BasicBinarySerializer.ReadUInt32).Method},
            {DMLType.STR, ((Func<ByteBuffer, ByteString>)ReadBinaryByteString).Method},
            {DMLType.WSTR, ((Func<ByteBuffer, string>)ReadBinaryWString).Method}, // todo: fix
            {DMLType.FLT, ((Func<ByteBuffer, float>)BasicBinarySerializer.ReadFloat).Method},
            {DMLType.DBL, ((Func<ByteBuffer, double>)BasicBinarySerializer.ReadDouble).Method},
            {DMLType.GID, ((Func<ByteBuffer, GID>)BasicBinarySerializer.ReadGID).Method}
        };
        
        private static readonly Dictionary<DMLType, MethodInfo> s_binaryTypeSerializerMethods = new Dictionary<DMLType, MethodInfo>
        {
            {DMLType.BYT, ((Action<ByteBuffer, sbyte>)BasicBinarySerializer.WriteInt8).Method},
            {DMLType.UBYT, ((Action<ByteBuffer, byte>)BasicBinarySerializer.WriteUInt8).Method},
            //{DMLType.SHRT, ((Action<ByteBuffer, short>)BasicBinarySerializer.WriteInt16).Method},
            {DMLType.USHRT, ((Action<ByteBuffer, ushort>)BasicBinarySerializer.WriteUInt16).Method},
            {DMLType.INT, ((Action<ByteBuffer, int>)BasicBinarySerializer.WriteInt32).Method},
            {DMLType.UINT, ((Action<ByteBuffer, uint>)BasicBinarySerializer.WriteUInt32).Method},
            {DMLType.STR, ((Action<ByteBuffer, ByteString>)SerializeBinaryByteString).Method},
            {DMLType.WSTR, ((Action<ByteBuffer, string>)SerializeBinaryWString).Method},
            {DMLType.FLT, ((Action<ByteBuffer, float>)BasicBinarySerializer.WriteFloat).Method},
            {DMLType.DBL, ((Action<ByteBuffer, double>)BasicBinarySerializer.WriteDouble).Method},
            {DMLType.GID, ((Action<ByteBuffer, GID>)BasicBinarySerializer.WriteGID).Method}
        };

        public static readonly PropertyInfo s_xmlNodeNameProp = typeof(XmlNode).GetProperty("Name");
        private static readonly PropertyInfo s_xmlNodeNextSiblingProp = typeof(XmlNode).GetProperty("NextSibling");
        
        public static Action<T, XmlNode> CreateReadXMLFunc<T>()
        {
            //return null;
            
            var objectArg = Expression.Parameter(typeof(T), "record");
            var xmlNodeArg = Expression.Parameter(typeof(XmlNode), "node");
            
            var switchCases = new List<SwitchCase>();
            foreach (FieldInfo field in typeof(T).GetFields())
            {
                var dmlAttr = field.GetCustomAttribute<DMLField>();
                if (dmlAttr == null) continue;

                var parserMethod = s_xmlTypeParserMethods[dmlAttr.m_type];
                
                if (dmlAttr.m_type == DMLType.STR && field.FieldType == typeof(string))
                {
                    parserMethod = ((Func<XmlNode, string>)SerializerXML.ReadString).Method;
                }

                switchCases.Add(
                    Expression.SwitchCase(Expression.Block(
                        Expression.Assign(
                            Expression.MakeMemberAccess(objectArg, field),
                            Expression.Call(parserMethod, xmlNodeArg)
                            )
                        ), 
                    Expression.Constant(dmlAttr.m_name))
                );
            }

            var breakLabel = Expression.Label("LoopBreak");
            var lambda = Expression.Lambda<Action<T, XmlNode>>(Expression.Block(new Expression[]
            {
                Expression.Loop(
                    Expression.IfThenElse(
                        Expression.Equal(xmlNodeArg, Expression.Constant(null)),
                        Expression.Break(breakLabel),
                        Expression.Block(new Expression[]
                        {
                            Expression.Switch(
                                typeof(void),
                                Expression.MakeMemberAccess(xmlNodeArg, s_xmlNodeNameProp),
                                null, null,
                                switchCases.ToArray()
                            ),
                            Expression.Assign(xmlNodeArg, Expression.MakeMemberAccess(xmlNodeArg, s_xmlNodeNextSiblingProp))
                        })
                    ),
                    breakLabel)
            }), objectArg, xmlNodeArg);
            
            return lambda.Compile();
        }

        public static Action<T, ByteBuffer> CreateReadBinaryFunc<T>()
        {
            var objectArg = Expression.Parameter(typeof(T), "record");
            var byteBufferArg = Expression.Parameter(typeof(ByteBuffer), "byteBuffer");

            var bodyExpressions = new List<Expression>();
            foreach (FieldInfo field in typeof(T).GetFields())
            {
                var dmlAttr = field.GetCustomAttribute<DMLField>();
                if (dmlAttr == null) continue;

                var readMethod = s_binaryTypeParserMethods[dmlAttr.m_type];
                if (readMethod == null)
                {
                    throw new NullReferenceException();
                }
                //Console.Out.WriteLine($"DMLReader::Binary: {dmlAttr.m_name}");

                if (dmlAttr.m_type == DMLType.STR && field.FieldType == typeof(string))
                {
                    readMethod = ((Func<ByteBuffer, string>)ReadBinaryString).Method;
                }
                bodyExpressions.Add(Expression.Assign(
                    Expression.MakeMemberAccess(objectArg, field),
                    Expression.Call(readMethod, byteBufferArg)
                ));
            }

            return Expression.Lambda<Action<T, ByteBuffer>>(Expression.Block(bodyExpressions.ToArray()), objectArg, byteBufferArg).Compile();
        }
        
        public static Action<ByteBuffer, T> CreateWriteBinaryFunc<T>()
        {
            var byteBufferArg = Expression.Parameter(typeof(ByteBuffer), "byteBuffer");
            var objectArg = Expression.Parameter(typeof(T), "record");
            
            var bodyExpressions = new List<Expression>();
            foreach (FieldInfo field in typeof(T).GetFields())
            {
                var dmlAttr = field.GetCustomAttribute<DMLField>();
                if (dmlAttr == null) continue;

                var writeMethod = s_binaryTypeSerializerMethods[dmlAttr.m_type];
                if (writeMethod == null)
                {
                    throw new NullReferenceException();
                }
                if (dmlAttr.m_type == DMLType.STR && field.FieldType == typeof(string))
                {
                    writeMethod = ((Action<ByteBuffer, string>)SerializeBinaryString).Method;
                }
                //Console.Out.WriteLine($"DMLReader::Binary: {dmlAttr.m_name}");
                
                bodyExpressions.Add(Expression.Call(writeMethod, byteBufferArg, Expression.MakeMemberAccess(objectArg, field)));
            }

            return Expression.Lambda<Action<ByteBuffer, T>>(Expression.Block(bodyExpressions.ToArray()), byteBufferArg, objectArg).Compile();
        }

        private static GID ReadXmlGID(XmlNode node)
        {
            // <LastPlayer TYPE="GID">191965934135706025</LastPlayer>
            throw new NotImplementedException();
        }

        #region String deserialization
        
        private static ByteString ReadXMLByteString(XmlNode node)
        {
            return (ByteString)node.InnerText;
        }
        
        private static ByteString ReadBinaryByteString(ByteBuffer buffer)
        {
            ushort count = buffer.ReadUInt16();
            return new ByteString(buffer.ReadBytes(count));
        }
        
        public static string ReadBinaryString(ByteBuffer buffer)
        {
            ushort count = buffer.ReadUInt16();
            return Encoding.UTF8.GetString(buffer.ReadBytes(count));
        }
        
        private static string ReadBinaryWString(ByteBuffer buffer)
        {
            ushort count = buffer.ReadUInt16();
            var strBytes = buffer.ReadBytes((int)count * 2);
            var str = Encoding.Unicode.GetString(strBytes);
            return str;
        }

        #endregion

        #region String serialization

        private static void SerializeBinaryString(ByteBuffer buffer, string val)
        {
            if (val == null)
            {
                buffer.WriteUInt16(0);
                return;
            }
            var bytes = Encoding.UTF8.GetBytes(val);
            buffer.WriteUInt16((ushort)bytes.Length);
            buffer.WriteBytes(bytes);
        }
        
        private static void SerializeBinaryByteString(ByteBuffer buffer, ByteString val)
        {
            var bytes = val.GetBytes();
            if (bytes == null)
            {
                buffer.WriteUInt16(0);
                return;
            }
            buffer.WriteUInt16((ushort)bytes.Length);
            buffer.WriteBytes(bytes);
        }
        
        private static void SerializeBinaryWString(ByteBuffer buffer, string val)
        {
            if (val == null)
            {
                buffer.WriteUInt16(0);
                return;
            }
            var bytes = Encoding.Unicode.GetBytes(val);
            buffer.WriteUInt16((ushort)val.Length);
            buffer.WriteBytes(bytes);
        }

        #endregion
    }
}