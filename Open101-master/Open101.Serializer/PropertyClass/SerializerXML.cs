using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Xml;
using DragonLib.IO;
using SharpDX;

namespace Open101.Serializer.PropertyClass
{
    public static class SerializerXML
    {
        public static PropertyClass ReadObject(XmlNode xmlObject)
        {
            return ReadObjectInternal(xmlObject.FirstChild);
        }

        private const string OLD_CLASS_PREFIX = "class.";

        public static readonly HashSet<string> s_missingClasses = new HashSet<string>();
        public static readonly HashSet<string> s_missingFields = new HashSet<string>();
        
        private static PropertyClass ReadObjectInternal(XmlNode xmlObject)
        {
            if (xmlObject == null) return null;

            var nameAttribute = xmlObject.Attributes.GetNamedItem("Name");
            string objectName;
            if (nameAttribute != null)
            {
                objectName = nameAttribute.Value;
            } else if (xmlObject.LocalName.StartsWith(OLD_CLASS_PREFIX))
            {
                objectName = "class " + xmlObject.LocalName.Substring(OLD_CLASS_PREFIX.Length);
            } else
            {
                throw new InvalidDataException();
            }
            
            var objectHash = SerializerFile.HashString(objectName);
            var propertyClass = PropertyClassRegistry.AllocateObject(objectHash);

            if (propertyClass == null)
            {
                if (s_missingClasses.Add(objectName))
                {
                    //Logger.Warn("SerializerXML",$"unknown property class \"{objectName}\" / {objectHash:X} / {objectHash}");
                    
                    //foreach (XmlElement property in xmlObject)
                    //{
                    //    //var propKey = $"{objectName}:{property.Name}";
                    //    //s_missingFields.Add(propKey);
                    //    //Logger.Warn($"SerializerXML:{propertyClass.GetType().Name}",$"Missing field \"{property.Name}\"");
                    //}
                }
                return null;
            }

            foreach (XmlElement property in xmlObject)
            {
                if (property is XmlElement element && element.IsEmpty) continue; // leave at default :)
                
                var result = propertyClass.DeserializeXMLField(property);
                if (!result)
                {
                    //var propKey = $"{objectName}:{property.Name}";
                    //s_missingFields.Add(propKey);
                    Logger.Warn($"SerializerXML:{propertyClass.GetType().Name}",$"Missing field \"{property.Name}\"");
                }
            }

            return propertyClass;
        }

        public static void ReadObjectVector<T>(XmlNode node, ref List<T> list) where T: PropertyClass
        {
            if (!node.HasChildNodes) return;
            EnsureVec(ref list);
            
            Debug.Assert(node.ChildNodes.Count == 1);

            list.Add((T)ReadObject(node));
        }

        public static string ReadString(XmlNode node)
        {
            return node.InnerText;
        }
        
        public static string ReadWString(XmlNode node)
        {
            return node.InnerText;
        }
        
        public static bool ReadBool(XmlNode node)
        {
            var lowerText = node.InnerText.ToLower();
            return lowerText switch
            {
                "false" => false,
                "true" => true,
                _ => int.Parse(node.InnerText) != 0
            };
        }
        
        public static sbyte ReadInt8(XmlNode node)
        {
            return sbyte.Parse(node.InnerText);
        }
        
        public static byte ReadUInt8(XmlNode node)
        {
            return byte.Parse(node.InnerText);
        }
        
        public static short ReadInt16(XmlNode node)
        {
            return short.Parse(node.InnerText);
        }
        
        public static ushort ReadUInt16(XmlNode node)
        {
            return ushort.Parse(node.InnerText);
        }
        
        public static int ReadInt32(XmlNode node)
        {
            return int.Parse(node.InnerText);
        }
        
        public static uint ReadUInt32(XmlNode node)
        {
            return uint.Parse(node.InnerText);
        }
        
        public static ulong ReadUInt64(XmlNode node)
        {
            return ulong.Parse(node.InnerText);
        }
        
        public static void ReadUInt64Vector(XmlNode node, ref List<ulong> list)
        {
            throw new NotImplementedException();
        }

        public static float ReadFloatStr(string node)
        {
            return float.Parse(node, CultureInfo.InvariantCulture);
        }
        
        public static float ReadFloat(XmlNode node)
        {
            return ReadFloatStr(node.InnerText);
        }
        
        public static double ReadDouble(XmlNode node)
        {
            return double.Parse(node.InnerText);
        }

        public static void ReadStringVector(XmlNode node, ref List<string> list)
        {
            EnsureVec(ref list);
            list.Add(ReadString(node));
        }
        
        public static void ReadWStringVector(XmlNode node, ref List<string> list)
        {
            EnsureVec(ref list);
            list.Add(ReadString(node));
        }
        
        public static void ReadUInt8Vector(XmlNode node, ref List<byte> list)
        {
            EnsureVec(ref list);
            list.Add(ReadUInt8(node));
        }
        
        public static void ReadUInt16Vector(XmlNode node, ref List<ushort> list)
        {
            EnsureVec(ref list);
            list.Add(ReadUInt16(node));
        }
        
        public static void ReadInt32Vector(XmlNode node, ref List<int> list)
        {
            EnsureVec(ref list);
            list.Add(ReadInt32(node));
        }
        
        public static void ReadUInt32Vector(XmlNode node, ref List<uint> list)
        {
            EnsureVec(ref list);
            list.Add(ReadUInt32(node));
        }

        public static void ReadFloatVector(XmlNode node, ref List<float> list)
        {
            EnsureVec(ref list);
            list.Add(ReadFloat(node));
        }
        
        public static void ReadEnum<T>(XmlNode node, int size, out T val) where T: struct, Enum
        {
            SerializerFile.ParseEnumString(node.InnerText, out val);
        }

        public static Color ReadColor(XmlNode node)
        {
            var a = node.InnerText.Substring(0, 2);
            var r = node.InnerText.Substring(2, 2);
            var g = node.InnerText.Substring(4, 2);
            var b = node.InnerText.Substring(6, 2);
            return new Color(
                byte.Parse(r, NumberStyles.HexNumber), 
                byte.Parse(g, NumberStyles.HexNumber),
                byte.Parse(b, NumberStyles.HexNumber), 
                byte.Parse(a, NumberStyles.HexNumber));
        }
        
        public static Rectangle ReadRectangle(XmlNode node)
        {
            var args = node.InnerText.Split(",");
            return new Rectangle
            {
                Left = int.Parse(args[0]),
                Top = int.Parse(args[1]),
                Right = int.Parse(args[2]),
                Bottom = int.Parse(args[3])
            };
        }
        
        public static void ReadRectangleVector(XmlNode node, ref List<Rectangle> list)
        {
            EnsureVec(ref list);
            list.Add(ReadRectangle(node));
        }
        
        public static RectangleF ReadRectangleF(XmlNode node)
        {
            var args = node.InnerText.Split(",");
            return new RectangleF
            {
                Left = ReadFloatStr(args[0]),
                Top = ReadFloatStr(args[1]),
                Right = ReadFloatStr(args[2]),
                Bottom = ReadFloatStr(args[3])
            };
        }
        
        public static void ReadRectangleFVector(XmlNode node, ref List<RectangleF> list)
        {
            EnsureVec(ref list);
            list.Add(ReadRectangleF(node));
        }
        
        public static Vector2 ReadVector2(XmlNode node)
        {
            var args = node.InnerText.Split(",");
            return new Vector2(ReadFloatStr(args[0]), ReadFloatStr(args[1]));
        }
        
        public static void ReadVector2Vector(XmlNode node, ref List<Vector2> list)
        {
            EnsureVec(ref list);
            list.Add(ReadVector2(node));
        }
        
        public static Vector3 ReadVector3(XmlNode node)
        {
            var args = node.InnerText.Split(",");
            return new Vector3(ReadFloatStr(args[0]), ReadFloatStr(args[1]), ReadFloatStr(args[2]));
        }
        
        public static void ReadVector3Vector(XmlNode node, ref List<Vector3> list)
        {
            EnsureVec(ref list);
            list.Add(ReadVector3(node));
        }
        
        public static Point ReadPoint(XmlNode node)
        {
            var args = node.InnerText.Split(",");
            return new Point(int.Parse(args[0]), int.Parse(args[1]));
        }
        
        public static void ReadPointVector(XmlNode node, ref List<Point> list)
        {
            EnsureVec(ref list);
            list.Add(ReadPoint(node));
        }
        
        public static GID ReadGID(XmlNode node)
        {
            if (!ulong.TryParse(node.InnerText, out ulong result))
            {
                throw new NotImplementedException();
            }
            return new GID(result);
        }
        
        public static void ReadGIDVector(XmlNode node, ref List<GID> list)
        {
            throw new NotImplementedException();
        }

        private static void EnsureVec<T>(ref List<T> list)
        {
            if (list == null)
            {
                list = new List<T>();
            }
        }
    }
}