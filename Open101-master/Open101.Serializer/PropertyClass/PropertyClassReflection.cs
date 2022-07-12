using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml;
using Open101.Serializer.DML;
using SharpDX;

namespace Open101.Serializer.PropertyClass
{
    [AttributeUsage(AttributeTargets.Field)]
    public class PropReflAttribute : Attribute
    {
        public readonly uint m_hash;
        public readonly string m_name;
        
        public PropReflAttribute(uint hash, string name=null)
        {
            m_hash = hash;
            m_name = name;
        }
        
        public PropReflAttribute(string name)
        {
            m_name = name;
            m_hash = 0;
        }
    }
    
    public static class PropertyClassReflection
    {
        public static Func<T, SerializerBinaryInstance, uint, bool> CreateDeserializeBinaryFieldFunc<T>()
        {
            var objectArg = Expression.Parameter(typeof(T), "object");
            var instArg = Expression.Parameter(typeof(SerializerBinaryInstance), "inst");
            var hashArg = Expression.Parameter(typeof(uint), "hash");
            
            ParameterExpression result = Expression.Parameter(typeof(bool), "result");
            
            var switchCases = new List<SwitchCase>();
            foreach (FieldInfo field in typeof(T).GetFields())
            {
                var propAttr = field.GetCustomAttribute<PropReflAttribute>();
                if (propAttr == null) continue;
                if (propAttr.m_hash == 0) continue;

                var fieldType = field.FieldType;
                Type genericType = null;
                if (fieldType.IsGenericType)
                {
                    genericType = fieldType.GetGenericTypeDefinition();
                }

                var fieldAccess = Expression.MakeMemberAccess(objectArg, field);
                Expression AssignToField(Expression val)
                {
                    return Expression.Assign(fieldAccess, val);
                }
                
                Expression switchBody;
                void AssignFromInstMethod(string funcName)
                {
                    switchBody = AssignToField(Expression.Call(instArg, typeof(SerializerBinaryInstance).GetMethod(funcName)));
                }
                
                if (genericType == typeof(List<>))
                {
                    var elementType = fieldType.GetGenericArguments()[0];
                    if (typeof(PropertyClass).IsAssignableFrom(elementType))
                    {
                        var readObjectVectorMethod = typeof(SerializerBinaryInstance).GetMethod("ReadObjectVector").MakeGenericMethod(elementType);
                        switchBody = Expression.Call(instArg, readObjectVectorMethod, fieldAccess);
                    }
                    else if (elementType == typeof(string)) switchBody = Expression.Call(instArg, typeof(SerializerBinaryInstance).GetMethod("ReadStringVector"), fieldAccess);
                    else if (elementType == typeof(GID)) switchBody = Expression.Call(instArg, typeof(SerializerBinaryInstance).GetMethod("ReadGIDVector"), fieldAccess);
                    else
                    {
                        throw new NotImplementedException($"[PropertyClassReflection:Binary] no implementation for reading List<{elementType}> using reflection");
                    }
                } else if (typeof(PropertyClass).IsAssignableFrom(fieldType))
                {
                    var readObjectMethod = typeof(SerializerBinaryInstance).GetMethod("ReadObject");
                    switchBody = AssignToField(Expression.TypeAs(Expression.Call(instArg, readObjectMethod), fieldType));
                }
                else if (fieldType == typeof(string)) AssignFromInstMethod("ReadString");
                else if (fieldType == typeof(bool)) AssignFromInstMethod("ReadBool");
                else if (fieldType == typeof(sbyte)) AssignFromInstMethod("ReadInt8");
                else if (fieldType == typeof(byte)) AssignFromInstMethod("ReadUInt8");
                else if (fieldType == typeof(short)) AssignFromInstMethod("ReadInt16");
                else if (fieldType == typeof(ushort)) AssignFromInstMethod("ReadUInt16");
                else if (fieldType == typeof(int)) AssignFromInstMethod("ReadInt32");
                else if (fieldType == typeof(uint)) AssignFromInstMethod("ReadUInt32");
                else if (fieldType == typeof(ulong)) AssignFromInstMethod("ReadUInt64");
                else if (fieldType == typeof(float)) AssignFromInstMethod("ReadFloat");
                else if (fieldType == typeof(double)) AssignFromInstMethod("ReadDouble");
                else if (fieldType == typeof(GID)) AssignFromInstMethod("ReadGID");
                else if (fieldType == typeof(Vector3)) AssignFromInstMethod("ReadVector3");
                else if (fieldType.IsEnum)
                {
                    // todo: only supports 4 byte enums. never seen anything else
                    switchBody = Expression.Call(instArg, typeof(SerializerBinaryInstance).GetMethod("ReadEnum").MakeGenericMethod(fieldType), Expression.Constant(4), fieldAccess);
                }
                else
                {
                    throw new NotImplementedException($"[PropertyClassReflection:Binary] no implementation for reading {fieldType} using reflection");
                }
                
                if (switchBody == null) continue;
                switchCases.Add(
                    Expression.SwitchCase(Expression.Block(switchBody, Expression.Assign(result, Expression.Constant(true))), Expression.Constant(propAttr.m_hash))
                );
            }

            return Expression.Lambda<Func<T, SerializerBinaryInstance, uint, bool>>(
                Expression.Block(new[] { result },
                Expression.Switch(
                    typeof(bool),
                    hashArg,
                    Expression.Assign(result, Expression.Constant(false)), null,
                    switchCases.ToArray()
                )), objectArg, instArg, hashArg).Compile();
        }

        public static Func<T, XmlNode, bool> CreateDeserializeXMLFieldFunc<T>()
        {
            var objectArg = Expression.Parameter(typeof(T), "object");
            var nodeArg = Expression.Parameter(typeof(XmlNode), "node");
            
            ParameterExpression result = Expression.Parameter(typeof(bool), "result");
            
            var switchCases = new List<SwitchCase>();
            foreach (FieldInfo field in typeof(T).GetFields())
            {
                var propAttr = field.GetCustomAttribute<PropReflAttribute>();
                if (propAttr?.m_name == null) continue;

                var fieldType = field.FieldType;
                Type genericType = null;
                if (fieldType.IsGenericType)
                {
                    genericType = fieldType.GetGenericTypeDefinition();
                }

                var fieldAccess = Expression.MakeMemberAccess(objectArg, field);
                Expression AssignToField(Expression val)
                {
                    return Expression.Assign(fieldAccess, val);
                }

                Expression switchBody;

                void AssignFromStaticMethod<U>(Func<XmlNode, U> func)
                {
                    switchBody = AssignToField(Expression.Call(func.Method, nodeArg));
                }
                
                if (genericType == typeof(List<>))
                {
                    var elementType = fieldType.GetGenericArguments()[0];
                    if (typeof(PropertyClass).IsAssignableFrom(elementType))
                    {
                        var readObjectVectorMethod = typeof(SerializerXML).GetMethod(nameof(SerializerXML.ReadObjectVector)).MakeGenericMethod(elementType);
                        switchBody = Expression.Call(readObjectVectorMethod, nodeArg, fieldAccess);
                    } 
                    else if (elementType == typeof(string)) switchBody = Expression.Call(typeof(SerializerXML).GetMethod(nameof(SerializerXML.ReadStringVector)), nodeArg, fieldAccess);
                    else if (elementType == typeof(GID)) switchBody = Expression.Call(typeof(SerializerXML).GetMethod(nameof(SerializerXML.ReadGIDVector)), nodeArg, fieldAccess);
                    else
                    {
                        throw new NotImplementedException($"[PropertyClassReflection:XML] no implementation for reading List<{elementType}> using reflection");
                    }
                } else if (typeof(PropertyClass).IsAssignableFrom(fieldType))
                {
                    switchBody = AssignToField(Expression.TypeAs(Expression.Call(((Func<XmlNode, PropertyClass>)SerializerXML.ReadObject).Method, nodeArg), fieldType));
                } else if (fieldType == typeof(string))
                {
                    switchBody = AssignToField(Expression.Call(((Func<XmlNode, string>)SerializerXML.ReadString).Method, nodeArg));
                } else if (fieldType == typeof(bool)) AssignFromStaticMethod(SerializerXML.ReadBool);
                else if (fieldType == typeof(sbyte)) AssignFromStaticMethod(SerializerXML.ReadInt8);
                else if (fieldType == typeof(byte)) AssignFromStaticMethod(SerializerXML.ReadUInt8);
                else if (fieldType == typeof(short)) AssignFromStaticMethod(SerializerXML.ReadInt16);
                else if (fieldType == typeof(ushort)) AssignFromStaticMethod(SerializerXML.ReadUInt16);
                else if (fieldType == typeof(int)) AssignFromStaticMethod(SerializerXML.ReadInt32);
                else if (fieldType == typeof(uint)) AssignFromStaticMethod(SerializerXML.ReadUInt32);
                //else if (fieldType == typeof(long)) AssignFromStaticMethod(SerializerXML.ReadInt64);
                else if (fieldType == typeof(ulong)) AssignFromStaticMethod(SerializerXML.ReadUInt64);
                else if (fieldType == typeof(float)) AssignFromStaticMethod(SerializerXML.ReadFloat);
                else if (fieldType == typeof(double)) AssignFromStaticMethod(SerializerXML.ReadDouble);
                else if (fieldType == typeof(GID)) AssignFromStaticMethod(SerializerXML.ReadGID);
                else if (fieldType == typeof(Vector3)) AssignFromStaticMethod(SerializerXML.ReadVector3);
                else if (fieldType.IsEnum)
                {
                    // todo: only supports 4 byte enums. never seen anything else
                    switchBody = Expression.Call(typeof(SerializerXML).GetMethod(nameof(SerializerXML.ReadEnum)).MakeGenericMethod(fieldType), nodeArg, Expression.Constant(4), fieldAccess);
                }
                else
                {
                    throw new NotImplementedException($"[PropertyClassReflection:XML] no implementation for reading {fieldType} using reflection");
                }
                
                if (switchBody == null) continue;
                switchCases.Add(
                    Expression.SwitchCase(Expression.Block(switchBody, Expression.Assign(result, Expression.Constant(true))), Expression.Constant(propAttr.m_name))
                );
            }

            return Expression.Lambda<Func<T, XmlNode, bool>>(Expression.Block(
                new[] {result},
                Expression.Switch(
                    typeof(bool),
                    Expression.MakeMemberAccess(nodeArg, DMLReader.s_xmlNodeNameProp),
                    Expression.Assign(result, Expression.Constant(false)), null,
                    switchCases.ToArray()
                )), objectArg, nodeArg).Compile();

            return null;
        }

        public static class Cache<T>
        {
            public static Func<T, SerializerBinaryInstance, uint, bool> s_deserializeBinaryFieldFunc =
                CreateDeserializeBinaryFieldFunc<T>();

            public static Func<T, XmlNode, bool> s_deserializeXMLFieldFunc = CreateDeserializeXMLFieldFunc<T>();

            public static bool DeserializeBinaryField(T dis, SerializerBinaryInstance buffer, uint hash)
            {
                return s_deserializeBinaryFieldFunc(dis, buffer, hash);
            }
            
            public static bool DeserializeXMLField(T dis, XmlNode node)
            {
                return s_deserializeXMLFieldFunc(dis, node);
            }
        }
    }
}