using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using Open101.IO;
using Open101.Serializer.DML;

namespace Open101.Generator
{
    public static class NetworkDMLGenerator
    {
        [DebuggerDisplay("Service {m_serviceID} Version {m_protocolVersion}: {m_protocolType} ({m_name}) - {m_protocolDescription}")]
        public class ProtocolInfo
        {
            [DMLField("ServiceID", DMLType.UBYT)] public byte m_serviceID;
            [DMLField("ProtocolType", DMLType.STR)] public string m_protocolType;
            [DMLField("ProtocolVersion", DMLType.INT)] public int m_protocolVersion;
            [DMLField("ProtocolDescription", DMLType.STR)] public string m_protocolDescription;

            public string m_name;
            public string m_file;

            public List<MessageInfo> m_messages = new List<MessageInfo>();
        }

        [DebuggerDisplay("Message {m_order}: {m_name} - {m_description}")]
        public class MessageInfo
        {
            //[DMLField("_MsgType", DMLType.UBYT)] public byte m_type; // not needed
            [DMLField("_MsgOrder", DMLType.UBYT)] public byte m_order;
            [DMLField("_MsgName", DMLType.STR)] public string m_name;
            [DMLField("_MsgDescription", DMLType.STR)] public string m_description;
            [DMLField("_MsgHandler", DMLType.STR)] public string m_handler;
            [DMLField("_MsgAccessLvl", DMLType.UBYT)] public byte m_accessLvl;

            public string m_elementName;
            public List<MessageField> m_fields = new List<MessageField>();

            public string GetCSHandlerName() => $"NetHandle{m_handler.Replace("MSG_", "")}";
        }

        [DebuggerDisplay("{" + nameof(m_type) + "} " + "{" + nameof(m_name) + "}")]
        public class MessageField
        {
            public string m_name;
            public DMLType m_type;
        }
        
        private static readonly Dictionary<DMLType, string> s_csharpDmlTypes = new Dictionary<DMLType, string>
        {
            {DMLType.BYT, "sbyte"},
            {DMLType.UBYT, "byte"},
            //{DMLType.SHRT, "short"},
            {DMLType.USHRT, "ushort"},
            {DMLType.INT, "int"},
            {DMLType.UINT, "uint"},
            {DMLType.STR, "ByteString"},
            {DMLType.WSTR, "string"},
            {DMLType.FLT, "float"},
            {DMLType.DBL, "double"},
            {DMLType.GID, "GID"}
        };

        public static ProtocolInfo LoadProtocolFromFile(string file)
        {
            using var stream = File.OpenRead(file);
            return LoadProtocolFile(file, stream);
        }

        public static ProtocolInfo LoadProtocolFile(string file, Stream stream)
        {
            var protocol = LoadProtocolXML(file, stream);
            if (protocol == null) return null;
            
            var fileName = Path.GetFileName(protocol.m_file);
            if (fileName == "CatchAKeyMessages.xml")
            {
                // lazy devs left these fields as "3" while all of the messages have "9"
                protocol.m_name = "MG9Messages";
                protocol.m_protocolDescription = "Messages for MG9 MinigameWindow Mini-Game";
                protocol.m_protocolType = "MG9_MESSAGES";
            }

            if (fileName == "WizCombatMessages.xml")
            {
                protocol.m_protocolType = "WIZARDCOMBAT_MESSAGES";
                protocol.m_protocolDescription = "Wizard Combat Messages";
            }
            return protocol;
        }
        
        public static void Run()
        {
            var protocols = new List<ProtocolInfo>();
            
            //foreach (string file in Directory.EnumerateFiles(@"D:\re\wiz101\extract_live\Root", "*", SearchOption.AllDirectories))
            //{

            var rootWad = ResourceManager.GetWad(ResourceManager.ROOT_WAD);
            foreach (var filePair in rootWad.m_recordDict)
            {
                var file = filePair.Value.m_filename;
                if (Path.GetExtension(file) != ".xml") continue;
                if (!file.Contains("Messages")) continue;

                //ResourceManager.DumpFile(file);
                
                using var stream = rootWad.OpenFile(file);
                var protocol = LoadProtocolFile(file, stream);
                if (protocol != null) protocols.Add(protocol);
            }
            
            IndentedTextWriter fullBuilder = new IndentedTextWriter(new StringWriter(), "    ");
            fullBuilder.Indent++;
            
            foreach (ProtocolInfo protocol in protocols)
            {
                var serviceClassName = $"{protocol.m_protocolType}_{protocol.m_serviceID}_Protocol";
                var handlerClassName = "Handler";
                
                fullBuilder.WriteLine($"public class {serviceClassName}: {nameof(INetworkService)}");
                fullBuilder.WriteLine("{");
                fullBuilder.Indent++;
                fullBuilder.WriteLine($"public const byte c_serviceID = {protocol.m_serviceID};");
                fullBuilder.WriteLine($"public byte GetID() => c_serviceID;");
                
                if (protocol.m_messages.Count > 0)
                {
                    fullBuilder.WriteLine();
                    WriteMessageClasses(fullBuilder, protocol);
                }
                
                fullBuilder.WriteLine();
                WriteDispatcherMethod(fullBuilder, handlerClassName, protocol);

                fullBuilder.WriteLine();
                WriteServiceAllocatorMethod(fullBuilder, protocol);
                
                //fullBuilder.Indent--; // end protocol class
                //fullBuilder.WriteLine("}");
                
                fullBuilder.WriteLine();
                fullBuilder.WriteLine($"public interface {handlerClassName}");
                fullBuilder.WriteLine("{");
                fullBuilder.Indent++;
                foreach (MessageInfo message in protocol.m_messages)
                {
                    fullBuilder.WriteLine($"bool {message.GetCSHandlerName()}({message.m_name} msg) => false;");
                }
                
                fullBuilder.Indent--; // end protocol class
                fullBuilder.WriteLine("}");
                
                fullBuilder.Indent--;
                fullBuilder.WriteLine("}");
            }
        }

        private static void WriteMessageClasses(IndentedTextWriter fullBuilder,  ProtocolInfo protocol)
        {
            foreach (MessageInfo message in protocol.m_messages)
            {
                fullBuilder.WriteLine($"public class {message.m_name} : {nameof(INetworkMessage)}");
                fullBuilder.WriteLine("{");
                fullBuilder.Indent++;
                fullBuilder.WriteLine($"public const byte c_messageID = {message.m_order};");
                fullBuilder.WriteLine("public byte GetID() => c_messageID;");
                fullBuilder.WriteLine($"public byte GetServiceID() => c_serviceID;");

                if (message.m_fields.Count > 0)
                {
                    fullBuilder.WriteLine();
                    foreach (MessageField field in message.m_fields)
                    {
                        string attribute =
                            $"[{nameof(DMLField)}(\"{field.m_name}\", {nameof(DMLType)}.{field.m_type})]";

                        fullBuilder.WriteLine(
                            $"{attribute} public {s_csharpDmlTypes[field.m_type]} {MakeNiceFieldName(field.m_name)};");
                    }
                }
                
                fullBuilder.WriteLine();
                fullBuilder.WriteLine("public void Serialize(ByteBuffer buf)");
                fullBuilder.WriteLine("{");
                fullBuilder.Indent++;
                fullBuilder.WriteLine($"DMLRecordReader<{message.m_name}>.Write(buf, this);");
                fullBuilder.Indent--;
                fullBuilder.WriteLine("}");
                
                fullBuilder.WriteLine();
                fullBuilder.WriteLine("public void Deserialize(ByteBuffer buf)");
                fullBuilder.WriteLine("{");
                fullBuilder.Indent++;
                fullBuilder.WriteLine($"DMLRecordReader<{message.m_name}>.Read(buf, this);");
                fullBuilder.Indent--;
                fullBuilder.WriteLine("}");

                fullBuilder.Indent--;
                fullBuilder.WriteLine("}");
            }
        }

        private static void WriteDispatcherMethod(IndentedTextWriter fullBuilder, string handlerClassName, ProtocolInfo protocol)
        {
            fullBuilder.WriteLine($"public bool Dispatch(object handlerVoid, {nameof(INetworkMessage)} message)");
            fullBuilder.WriteLine("{");
            fullBuilder.Indent++;
            fullBuilder.WriteLine("Debug.Assert(message.GetServiceID() == c_serviceID);");
            fullBuilder.WriteLine($"var handler = ({handlerClassName})handlerVoid;");
            fullBuilder.WriteLine();
            fullBuilder.WriteLine("switch (message.GetID())");
            fullBuilder.WriteLine("{");
            fullBuilder.Indent++;
            foreach (MessageInfo message in protocol.m_messages)
            {
                fullBuilder.WriteLine($"case {message.m_name}.c_messageID:");
                fullBuilder.Indent++;
                fullBuilder.WriteLine($"return handler.{message.GetCSHandlerName()}(({message.m_name})message);");
                fullBuilder.Indent--;
            }
            fullBuilder.Indent--;
            fullBuilder.WriteLine("}");
            fullBuilder.WriteLine("return false;");
            fullBuilder.Indent--;
            fullBuilder.WriteLine("}");
        }

        private static void WriteServiceAllocatorMethod(IndentedTextWriter fullBuilder, ProtocolInfo protocolInfo)
        {
            fullBuilder.WriteLine($"public {nameof(INetworkMessage)} AllocateMessage(byte id)");
            fullBuilder.WriteLine("{");
            fullBuilder.Indent++;
            
            fullBuilder.WriteLine("switch (id)");
            fullBuilder.WriteLine("{");
            fullBuilder.Indent++;
            foreach (MessageInfo message in protocolInfo.m_messages)
            {
                fullBuilder.WriteLine($"case {message.m_name}.c_messageID: return new {message.m_name}();");
            }
            fullBuilder.Indent--;
            fullBuilder.WriteLine("}");
            
            fullBuilder.WriteLine($"throw new {nameof(ArgumentOutOfRangeException)}(nameof(id));");
            fullBuilder.Indent--;
            fullBuilder.WriteLine("}");
        }

        private static string MakeNiceFieldName(string name)
        {
            // todo: something else needed here, can't remember what
            if (name == "IP" || name == "TCPPort" || name == "UDPPort" || name.StartsWith("URL")) return $"m_{name}";
            return $"m_{char.ToLower(name[0])}{name.Substring(1, name.Length-1)}";
        }

        private static ProtocolInfo LoadProtocolXML(string fileName, Stream stream)
        {
            using var streamReader = new StreamReader(stream);
            var text = streamReader.ReadToEnd();
            if (Path.GetFileName(fileName) == "WizardMessages2.xml")
            {
                // fix malformed xml in r681117.WizardDev_1_420_1 (thx devs)
                text = text.Replace("<LastMatchStatus TYPE=\"INT\"><LastMatchStatus>", "<LastMatchStatus TYPE=\"INT\"></LastMatchStatus>");
            }
            
            XmlDocument xmlDoc = new XmlDocument(); // Create an XML document object
            xmlDoc.LoadXml(text); // Load the XML document from the specified file

            var root = xmlDoc.DocumentElement;
            var protocol = ParseProtocolInfo(root.FirstChild);
            protocol.m_name = root.Name;
            
            HashSet<string> seenMessages = new HashSet<string>();

            bool needsOrder = true;
            foreach (XmlNode element in root.ChildNodes)
            {
                if (element is XmlComment)
                {
                    continue; // aint a message
                }
                
                if (element.Name.StartsWith("_")) continue;

                var messageRecord = element.FirstChild;
                var messageInfo = DMLRecordReader<MessageInfo>.Read(messageRecord);
                messageInfo.m_elementName = element.Name;

                if (!seenMessages.Add(messageInfo.m_name))
                {
                    // it isn't clear to me what the client does when parsing, but in memory only the first definition
                    // for a message shows up. any others are discarded
                    continue;
                }

                if (messageInfo.m_order != 0)
                {
                    if (needsOrder)
                    {
                        Debug.Assert(protocol.m_messages.Count == 0);
                        needsOrder = false;
                    }
                } else
                {
                    Debug.Assert(needsOrder);
                }

                foreach (XmlNode fieldNode in messageRecord.ChildNodes)
                {
                    if (fieldNode is XmlText)
                    {
                        // malformed xml time
                        // line = <AltMusicFile TYPE="UINT"></AltMusicFile>"
                        continue;
                    }
                    
                    var noTransfer = fieldNode.Attributes.GetNamedItem("NOXFER");
                    if (noTransfer != null) continue;

                    var typeAttribute = fieldNode.Attributes.GetNamedItem("TYPE");
                    if (typeAttribute == null)
                    {
                        typeAttribute = fieldNode.Attributes.GetNamedItem("TPYE"); // ?????????? keyboard go brr
                    } 
                    if (typeAttribute == null)
                    {
                        // MSG_BATTLEGROUNDQUEUEUPDATE::Kicked
                        // added in r681117.WizardDev_1_420_1
                        typeAttribute = fieldNode.Attributes.GetNamedItem("TYP"); // ?????????? keyboard go brr
                    }
                    
                    DMLType type;
                    if (typeAttribute != null)
                    {
                        type = Enum.Parse<DMLType>(typeAttribute.Value);
                    } else
                    {
                        // why???
                        if (fieldNode.Name == "GlobalID")
                        {
                            type = DMLType.GID;
                        } else
                        {
                            throw new InvalidDataException();
                        }
                    }
                    
                    messageInfo.m_fields.Add(new MessageField
                    {
                        m_name = fieldNode.Name,
                        m_type = type
                    });
                }
                
                protocol.m_messages.Add(messageInfo);
            }

            if (needsOrder)
            {
                protocol.m_messages.Sort(
                    (left, right) => string.CompareOrdinal(left.m_elementName, right.m_elementName));

                byte i = 1;
                foreach (var message in protocol.m_messages)
                {
                    //Console.Out.WriteLine($"{i}: {message.m_name}");
                    message.m_order = i;
                    i++;
                }
            }

            protocol.m_file = fileName;
            return protocol;
        }

        public static ProtocolInfo ParseProtocolInfo(XmlNode element)
        {
            if (element.Name != "_ProtocolInfo") throw new InvalidDataException();
            var protocol = DMLRecordReader<ProtocolInfo>.Read(element.FirstChild);
            return protocol;
        }
    }
}