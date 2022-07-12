using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using DragonLib.IO;
using Open101.Serializer.PropertyClass;
using Utf8Json;

namespace Open101.Generator
{
    public class ClassDef
    {
        [DataMember(Name = "hash")] public uint m_hash;
        [DataMember(Name = "parent")] public string m_parent;
        [DataMember(Name = "properties")] public List<Property> m_properties;
        [DataMember(Name = "typeName")] public string m_typeName;

        public uint m_behaviorInstanceHash;
        public byte m_coreType;
    }

    public class BehaviorInstanceClass
    {
        [DataMember(Name = "class")] public string m_class;
        [DataMember(Name = "rtti_name")] public string m_rttiName;
    }
    
    public class Property
    {
        [DataMember(Name = "container")] public string m_container;
        [DataMember(Name = "flags")] public uint m_flags;
        [DataMember(Name = "hash")] public uint m_hash;
        [DataMember(Name = "name")] public string m_name;
        [DataMember(Name = "offset")] public uint m_offset;
        [DataMember(Name = "options")] public List<Option> m_options;
        [DataMember(Name = "size")] public uint m_size;
        [DataMember(Name = "type")] public string m_type;

        private const string DEFAULT_OPTION_KEY = "__DEFAULT";
        private const string BASECLASS_OPTION_KEY = "__BASECLASS"; // todo: what does this mean. appears on some std::string properties
        
        public bool IsVecList()
        {
            return m_container == "Vector" || m_container == "List";
        }

        public string GetDefaultValue()
        {
            if (m_options == null) return null;
            foreach (Option option in m_options)
            {
                if (option.m_name == DEFAULT_OPTION_KEY)
                {
                    //Console.Out.WriteLine($"__DEFAULT: {m_name}: {option.m_value}");
                    return PostProcessDefaultValue(option.m_value);
                }
            }
            return null;
        }

        private string PostProcessDefaultValue(string val)
        {
            if (m_type == "std::string")
            {
                if (val == string.Empty)
                {
                    return "string.Empty";
                }
                return $"\"{val}\"";
            } 
            if (m_type == "bool")
            {
                Debug.Assert(val == "0" || val == "1");
                return val == "1" ? "true" : "false";
            }
            if (m_type == "float")
            {
                return $"{val}f";
            }
            var obj = PropertyClassGenerator.GetObjectValueType(m_type);
            if (obj != null && obj.m_type == PropertyClassGenerator.UserDefinedTypeRef.Type.Enum)
            {
                return $"{obj.m_typeName}.{val}";
            }

            return val;
        }

        public IEnumerable<Option> GetOptions()
        {
            if (m_options == null) yield break;
            foreach (Option option in m_options)
            {
                if (option.m_name == DEFAULT_OPTION_KEY || option.m_name == BASECLASS_OPTION_KEY) continue;
                yield return option;
            }
        }
    }

    public class Option
    {
        [DataMember(Name = "name")] public string m_name;
        [DataMember(Name = "str_val")] public string m_value;


        private string m_unscopedName;

        public string GetUnscopedName
        {
            get
            {
                if (m_unscopedName != null) return m_unscopedName;
                
                var optionNameScope = PropertyClassGenerator.ParseScope(m_name);
                m_unscopedName = optionNameScope[^1];
                return m_unscopedName;
            }
        }
    }

    public class JsonRoot
    {
        [DataMember(Name = "classes")] public Dictionary<string, ClassDef> m_classes;
        [DataMember(Name = "core_types")] public Dictionary<byte, string> m_coreTypes;
        [DataMember(Name = "behavior_types")] public Dictionary<uint, BehaviorInstanceClass> m_behaviorTypes;
    }

    public static class PropertyClassGenerator
    {
        public static HashSet<string> s_unkTypes = new HashSet<string>();
        public static Dictionary<string, TypeOutput> s_rootTypes;
        public static Dictionary<string, TypeOutput> s_allTypes;

        public static Dictionary<uint, string> s_stringHashes;

        public const string BASE_CLASS = "PropertyClass";
        public const string OUTPUT_NAME = "Wizard";

        public struct CsTypeDef
        {
            public string m_name;
            public string m_method;

            public string m_readMethod => m_method == null ? null : $"Read{m_method}";
            public string m_writeMethod => m_method == null ? null : $"Write{m_method}";

            public CsTypeDef(string name, string method)
            {
                m_name = name;
                m_method = method;
            }
        }

        public class TypeOutput
        {
            public string m_fullName;
            public string m_localName;
            
            public List<TypeOutput> m_children;

            public ClassDef m_classDef;
            public EnumDef m_enumDef;
        }

        public class EnumDef
        {
            public List<Option> m_options;
        }
        
        public static Dictionary<string, CsTypeDef> s_basicTypes = new Dictionary<string, CsTypeDef>
        {
            {"bool", new CsTypeDef("bool", "Bool")},
            
            {"char", new CsTypeDef("sbyte", "Int8")}, // todo: idk
            {"unsigned char", new CsTypeDef("byte", "UInt8")},
            {"wchar_t", new CsTypeDef("char", null)},

            {"short", new CsTypeDef("short", "Int16")},
            {"unsigned short", new CsTypeDef("ushort", "UInt16")},
            
            {"int", new CsTypeDef("int", "Int32")},
            {"unsigned int", new CsTypeDef("uint", "UInt32")},
            
            {"long", new CsTypeDef("int", "Int32")}, // c long = 4 bytes
            {"unsigned long", new CsTypeDef("uint", "UInt32")}, // c long = 4 bytes
            
            {"unsigned __int64", new CsTypeDef("ulong", "UInt64")},

            {"float", new CsTypeDef("float", "Float")},
            {"double", new CsTypeDef("double", "Double")},
            
            {"std::string", new CsTypeDef("string", "String")},
            {"std::wstring", new CsTypeDef("string", "WString")},
            
            {"class Vector3D", new CsTypeDef("Vector3", "Vector3")},
            {"class Euler", new CsTypeDef("Vector3", "Vector3")},
            {"class Quaternion", new CsTypeDef("Quaternion", null)},
            {"class Matrix3x3", new CsTypeDef("Matrix3x3", null)},
            {"class Color", new CsTypeDef("Color", "Color")},
            {"class Rect<float>", new CsTypeDef("RectangleF", "RectangleF")},
            {"class Rect<int>", new CsTypeDef("Rectangle", "Rectangle")},
            {"class Point<float>", new CsTypeDef("Vector2", "Vector2")},
            {"class Point<int>", new CsTypeDef("Point", "Point")},
            {"class Size<int>", new CsTypeDef("Point", "Point")}, // todo: pls don't break anything
            
            {"class SerializedBuffer", new CsTypeDef(null, null)},
            {"struct SimpleVert", new CsTypeDef(null, null)},
            {"struct SimpleFace", new CsTypeDef(null, null)},
            {"gid", new CsTypeDef("GID", "GID")},
            {"bi6", new CsTypeDef(null, null)},
            {"bui2", new CsTypeDef("byte", null)},
            {"bui4", new CsTypeDef("byte", null)},
            {"bui5", new CsTypeDef("byte", null)},
            {"bui7", new CsTypeDef("byte", null)},
            {"s24", new CsTypeDef(null, null)}
        };

        public static Dictionary<string, string> s_buiMethods = new Dictionary<string, string>
        {
            {"bui2", "BitUInt2"},
            {"bui4", "BitUInt4"},
            {"bui5", "BitUInt5"},
            {"bui7", "BitUInt7"}
        };

        private static string FlattenScope(string name)
        {
            return name.Replace("::", "_");
        }

        public static string[] ParseScope(string name)
        {
            return name.Split("::");
        }
        
        private static void HandleScopedType(string name, TypeOutput output)
        {
            var scope = ParseScope(name);
            if (scope.Length == 1)
            {
                // no scope
                output.m_localName = name;
                output.m_fullName = name;
                s_rootTypes[name] = output;
            } else
            {
                var cppScope = string.Join("::", scope.Take(scope.Length - 1));
                
                if (s_allTypes.TryGetValue(cppScope, out var parent))
                {
                    Debug.Assert(parent.m_fullName != null); // ensure parent scope stuff has been handled
                    
                    // parent exists, we can be inside it
                    
                    output.m_localName = scope[^1];
                    output.m_fullName = parent.m_fullName + "." + output.m_localName;
                    parent.m_children.Add(output);
                } else
                {
                    // parent isn't a generated class, so put this in the global scope
                    
                    output.m_localName = FlattenScope(name);
                    output.m_fullName = output.m_localName;
                    s_rootTypes[name] = output;
                }
            }
        }

        private static void DefineMadlibT(JsonRoot json, string name, string convertedName)
        {
            var typeOutput = new TypeOutput
            {
                m_classDef = json.m_classes[name],
                m_fullName = convertedName,
                m_localName = convertedName
            };
            s_allTypes[name] = typeOutput;
            s_rootTypes[name] = typeOutput;
        }

        private static void FixBehaviorName(BehaviorInstanceClass behaviorType, string name, uint behaviorHash)
        {
            string typeName = $"class {name}";
            s_allTypes[name] = new TypeOutput
            {
                m_classDef = new ClassDef
                {
                    m_typeName = typeName,
                    m_hash = SerializerFile.HashString(typeName),
                    m_parent = "BehaviorInstance",
                    m_properties = new List<Property>(),
                    m_coreType = 0,
                    m_behaviorInstanceHash = behaviorHash
                },
                m_children = null
            };
            behaviorType.m_class = name;
        }
        
        public static void Run()
        {
            s_stringHashes = new Dictionary<uint, string>();
            foreach (string line in File.ReadAllLines("dumped_class_strings.txt"))
            {
                var str = line.Split('\t')[3];
                var h = SerializerFile.HashString(str);
                s_stringHashes[h] = str;
            }
            
            var json = JsonSerializer.Deserialize<JsonRoot>(File.ReadAllBytes(@"D:\re\wiz101\classes.txt"));

            int anonEnumIdx = 1;
            
            s_allTypes = new Dictionary<string, TypeOutput>();
            foreach (KeyValuePair<string, ClassDef> classPair in json.m_classes)
            {
                if (IsDisallowedType(classPair.Key))
                {
                    Logger.Warn("Gen", $"Skipping auto-registration of dangerous type {classPair.Key}");
                    
                    // can't generate template classes e.g MadlibArgT
                    continue;
                }

                s_allTypes[classPair.Key] = new TypeOutput
                {
                    m_children = new List<TypeOutput>(),
                    m_classDef = classPair.Value
                };
                
                foreach (Property property in classPair.Value.m_properties)
                {
                    if ((property.m_flags & PropertyClass.PROPERTY_FLAGS_C_ENUM) != 0)
                    {
                        string anonScope = string.Empty;
                        var firstOptionScope = ParseScope(property.GetOptions().First().m_name);
                        if (firstOptionScope.Length > 1)
                        {
                            anonScope = string.Join("::", firstOptionScope.Take(firstOptionScope.Length - 1)) + "::";
                        }
                        
                        //Console.Out.WriteLine($"{property.m_name} - c enum - \"{property.m_type}\" - {property.m_size}");
                        property.m_type = $"enum {anonScope}AnonEnum{anonEnumIdx}";
                        anonEnumIdx++;
                    }
                    if (property.m_type == "std::string" && property.GetOptions().Any())
                    {
                        Console.Out.WriteLine($"{classPair.Key}::{property.m_name}: std::string with options");
                        foreach (Option option in property.GetOptions())
                        {
                            Console.Out.WriteLine($"    \"{option.m_name}\": \"{option.m_value}\"");
                        }
                    }

                    if (property.m_name.EndsWith(".m_full"))
                    {
                        Debug.Assert(property.m_type == "unsigned __int64");
                        
                        // todo: what is this type? a gid?
                        property.m_name = property.m_name.Substring(0, property.m_name.Length-".m_full".Length);
                        property.m_type = "gid";
                    }

                    if (property.m_type.StartsWith("enum "))
                    {
                        var enumName = property.m_type.Substring("enum ".Length);

                        if (s_allTypes.TryGetValue(enumName, out var existingEnum))
                        {
                            // enum AudioCategory is weird
                            // one goes up to 7
                            // one goes up to 3
                            // one is a "subclass"
                            
                            foreach (Option option in property.GetOptions())
                            {
                                var foundOption = existingEnum.m_enumDef.m_options.Find(x => x.GetUnscopedName == option.GetUnscopedName);
                                if (foundOption != null)
                                {
                                    Debug.Assert(foundOption.m_value == option.m_value);
                                    continue;
                                }
                                
                                Logger.Info("Gen", $"Adding missing option {option.m_name} to enum {enumName}");
                                existingEnum.m_enumDef.m_options.Add(option);
                            }
                        } else
                        {
                            var e = new TypeOutput
                            {
                                m_children = null,
                                m_enumDef = new EnumDef
                                {
                                    m_options = new List<Option>()
                                }
                            };
                            foreach (Option option in property.GetOptions())
                            {
                                var foundOption = e.m_enumDef.m_options.Find(x => x.GetUnscopedName == option.GetUnscopedName);
                                if (foundOption != null)
                                {
                                    Debug.Assert(foundOption.m_value == option.m_value);
                                    Logger.Warn("Gen", $"Skipping adding existing enum option {foundOption.m_name} to {enumName}");
                                    continue;
                                }
                                e.m_enumDef.m_options.Add(option);
                            }
                            
                            s_allTypes[enumName] = e;
                        }
                    }
                }
            }

            foreach (var behaviorInstPair in json.m_behaviorTypes)
            {
                var behaviorType = behaviorInstPair.Value;
                if (behaviorType.m_class == "BehaviorInstance")
                {
                    if (behaviorType.m_rttiName != "BehaviorInstance")
                    {
                        FixBehaviorName(behaviorType, behaviorType.m_rttiName, behaviorInstPair.Key);
                    } else if (s_stringHashes.TryGetValue(behaviorInstPair.Key, out var behaviorName))
                    {
                        FixBehaviorName(behaviorType, behaviorName, behaviorInstPair.Key);
                    } else
                    {
                        throw new InvalidDataException();
                    }
                } else
                {
                    var actualType = s_allTypes[behaviorType.m_class];
                    Debug.Assert(actualType.m_classDef.m_behaviorInstanceHash == 0);
                    actualType.m_classDef.m_behaviorInstanceHash = behaviorInstPair.Key;
                }
            }
            
            HashSet<string> defined = new HashSet<string>();
            void DefineRecursive(string name)
            {
                if (defined.Add(name))
                {
                    var type = s_allTypes[name];
                    
                    var scope = ParseScope(name);
                    var cppScope = string.Join("::", scope.Take(scope.Length - 1));
                    
                    if (s_allTypes.ContainsKey(cppScope))
                    {
                        DefineRecursive(cppScope);
                    }
                    
                    HandleScopedType(name, type);
                }
            }
            
            s_rootTypes = new Dictionary<string, TypeOutput>();
            foreach (KeyValuePair<string,TypeOutput> typePair in s_allTypes)
            {
                DefineRecursive(typePair.Key);
            }

            DefineMadlibT(json, "MadlibArgT<double>", "MadlibArgT_double");
            DefineMadlibT(json, "MadlibArgT<float>", "MadlibArgT_float");
            DefineMadlibT(json, "MadlibArgT<int>", "MadlibArgT_int");
            DefineMadlibT(json, "MadlibArgT<std::string>", "MadlibArgT_string");
            DefineMadlibT(json, "MadlibArgT<std::string const>", "MadlibArgT_stringConst");
            DefineMadlibT(json, "MadlibArgT<std::wstring>", "MadlibArgT_wstring");
            DefineMadlibT(json, "MadlibArgT<unsigned int>", "MadlibArgT_uint");

            foreach (KeyValuePair<byte, string> coreTypePair in json.m_coreTypes)
            {
                var actualType = s_allTypes[coreTypePair.Value];
                actualType.m_classDef.m_coreType = coreTypePair.Key;
            }

            IndentedTextWriter fullBuilder = new IndentedTextWriter(new StringWriter(), "    ");
            fullBuilder.Indent++;
            foreach (KeyValuePair<string,TypeOutput> typeOutput in s_rootTypes)
            {
                AddToOutput(fullBuilder, typeOutput.Value);
                fullBuilder.WriteLine();
            }
            
            fullBuilder.WriteLine($"public class {OUTPUT_NAME}PropertyClassRegistry : IPropertyClassRegistryInst");
            fullBuilder.WriteLine("{");

            // AllocateObject
            {
                fullBuilder.WriteLine($"    public virtual {BASE_CLASS} AllocateObject(uint hash)");
                fullBuilder.WriteLine("    {");
                fullBuilder.WriteLine("        switch (hash)");
                fullBuilder.WriteLine("        {");
                foreach (var classPair in s_allTypes)
                {
                    if (classPair.Value.m_classDef == null) continue;
                    fullBuilder.WriteLine(
                        $"            case 0x{classPair.Value.m_classDef.m_hash:X8}: return new {classPair.Value.m_fullName}();");
                }

                fullBuilder.WriteLine("        }");
                fullBuilder.WriteLine("        return null;");
                fullBuilder.WriteLine("    }");
            }

            // AllocateCoreObject
            {
                fullBuilder.WriteLine();
                fullBuilder.WriteLine($"    public virtual {BASE_CLASS} AllocateCoreObject(uint num)");
                fullBuilder.WriteLine("    {");
                fullBuilder.WriteLine("        switch (num)");
                fullBuilder.WriteLine("        {");
                foreach (var coreTypePair in json.m_coreTypes.OrderBy(x => x.Key))
                {
                    var actualType = s_allTypes[coreTypePair.Value];
                    fullBuilder.WriteLine(
                        $"            case {coreTypePair.Key}: return new {actualType.m_fullName}();");
                }

                fullBuilder.WriteLine("        }");
                fullBuilder.WriteLine("        return null;");
                fullBuilder.WriteLine("    }");
            }

            // AllocateBehavior
            {
                fullBuilder.WriteLine();
                fullBuilder.WriteLine($"    public virtual {BASE_CLASS} AllocateBehavior(uint hash)");
                fullBuilder.WriteLine("    {");
                fullBuilder.WriteLine("        switch (hash)");
                fullBuilder.WriteLine("        {");
                foreach (var behaviorInstPair in json.m_behaviorTypes.OrderBy(x => x.Value.m_class))
                {
                    var actualType = s_allTypes[behaviorInstPair.Value.m_class];
                    fullBuilder.WriteLine(
                        $"            case 0x{behaviorInstPair.Key:X8}: return new {actualType.m_fullName}();");
                }

                fullBuilder.WriteLine("        }");
                fullBuilder.WriteLine("        return null;");
                fullBuilder.WriteLine("    }");
            }
            
            fullBuilder.Write("}"); // registry end
        }

        private static void AddToOutput(IndentedTextWriter builder, TypeOutput output)
        {
            if (output.m_fullName == BASE_CLASS) return;
            if (output.m_classDef != null)
            {
                WriteClass(builder, output);
            } else if (output.m_enumDef != null)
            {
                builder.WriteLine($"public enum {output.m_localName}");
                builder.WriteLine("{");
                builder.Indent++;
                foreach (Option option in output.m_enumDef.m_options.OrderBy(x => long.Parse(x.m_value)))
                {
                    builder.WriteLine($"{SerializerFile.CleanOptionName(option.GetUnscopedName)} = {option.m_value},");
                }
                builder.Indent--;
                builder.WriteLine("}");
            }
        }

        private static void WriteClass(IndentedTextWriter builder, TypeOutput output)
        {
            builder.Write($"public class {output.m_localName}");
            if (output.m_fullName != BASE_CLASS)
            {
                string parent = BASE_CLASS;
                if (output.m_classDef.m_parent != null)
                {
                    parent = s_allTypes[output.m_classDef.m_parent].m_fullName;
                }
                builder.Write($" : {parent}");
            }
            builder.WriteLine();
            builder.WriteLine("{");
            builder.Indent++;
            
            builder.WriteLine($"public override uint GetHash() => 0x{output.m_classDef.m_hash:X8};");
            if (output.m_classDef.m_behaviorInstanceHash != 0)
            {
                builder.WriteLine($"public override uint GetBehaviorInstanceHash() => 0x{output.m_classDef.m_behaviorInstanceHash:X8};");
            } else
            {
                builder.WriteLine("public override uint GetBehaviorInstanceHash() => 0;");
            }
            if (output.m_classDef.m_coreType != 0)
            {
                builder.WriteLine($"public override byte GetCoreType() => {output.m_classDef.m_coreType};");
            } else
            {
                builder.WriteLine($"public override byte GetCoreType() => 0;");
            }
            
            if (output.m_children != null && output.m_children.Count > 0)
            {
                foreach (TypeOutput child in output.m_children)
                {
                    builder.WriteLine();
                    AddToOutput(builder, child);
                }
            }

            WriteCustomFields(builder, output);
            
            HashSet<string> writtenProps;
            
            if (output.m_classDef.m_properties.Count > 0)
            {
                builder.WriteLine();
                
                writtenProps = WriteFields(builder, output);
                
                WriteCustomMethods(builder, output);
                
                builder.WriteLine();
                WriteBinaryMethod(builder, output, writtenProps);
            
                builder.WriteLine();
                WriteFlatBinaryMethod(builder, output, writtenProps);

                builder.WriteLine();
                WriteXMLMethod(builder, output, writtenProps);
                
                builder.WriteLine();
                WriteSerializeBinaryMethod(builder, output, writtenProps);
            } else
            {
                writtenProps = new HashSet<string>();
                
                WriteCustomMethods(builder, output);
            }
            
            builder.WriteLine();
            WriteCopyThis(builder, output, writtenProps);

            builder.Indent--;
            builder.WriteLine("}");
        }

        private static void WriteCopyThis(IndentedTextWriter builder, TypeOutput output, HashSet<string> writtenProps)
        {
            builder.WriteLine("public override PropertyClass CopyThis()");
            builder.WriteLine("{");
            builder.Indent++; 
            
            // builder.WriteLine($"if (GetHash() != 0x{output.m_classDef.m_hash:X8}) return null;");  // a hack... do not allow calling from a derived type
            builder.WriteLine($"if (GetType() != typeof({output.m_localName})) return null;");  // a hack... do not allow calling from a derived type
            
            builder.WriteLine($"var obj = new {output.m_localName}();");
            builder.WriteLine("CopyTo(obj);");
            builder.WriteLine("return obj;");
            builder.Indent--;
            builder.WriteLine("}");

            if (writtenProps.Count > 0)
            {
                builder.WriteLine();
                builder.WriteLine("protected override void CopyTo(PropertyClass obj)");
                builder.WriteLine("{");
                builder.Indent++;
                builder.WriteLine($"base.CopyTo(obj);");
                builder.WriteLine($"var obj1 = ({output.m_localName})obj;");

                foreach (var prop in output.m_classDef.m_properties)
                {
                    bool missingBasic = false;
                    if (s_basicTypes.TryGetValue(prop.m_type, out var csType))
                    {
                        missingBasic = csType.m_name == null;
                        // todo: why does this need special handling
                    }

                    if (!writtenProps.Contains(prop.m_name) || missingBasic)
                    {
                        builder.WriteLine($"// {prop.m_name} missing");
                        continue;
                    }

                    var objType = GetObjectValueType(prop.m_type);
                    var canCopyEz = objType == null || objType.m_type == UserDefinedTypeRef.Type.Enum;
                    string castToType = null;
                    if (!canCopyEz)
                    {
                        castToType = objType.m_typeName;
                    }

                    if (prop.IsVecList())
                    {
                        builder.WriteLine($"foreach (var val in {prop.m_name})");
                        builder.WriteLine("{");
                        builder.Indent++;

                        if (canCopyEz) builder.WriteLine($"obj1.{prop.m_name}.Add(val);");
                        else builder.WriteLine($"obj1.{prop.m_name}.Add(({castToType})val?.CopyThis());");

                        builder.Indent--;
                        builder.WriteLine("}");
                    } else
                    {
                        if (canCopyEz) builder.WriteLine($"obj1.{prop.m_name} = {prop.m_name};");
                        else builder.WriteLine($"obj1.{prop.m_name} = ({castToType}){prop.m_name}?.CopyThis();");
                    }
                }

                if (output.m_localName == "BehaviorInstance")
                {
                    builder.WriteLine($"obj1.m_behaviorTemplate = m_behaviorTemplate;");
                }
                if (output.m_localName == "CoreObject")
                {
                    builder.WriteLine($"obj1.m_template = m_template;");
                }

                builder.Indent--;
                builder.WriteLine("}");
            }
        }

        private static void WriteCustomFields(IndentedTextWriter builder, TypeOutput output)
        {
            if (output.m_localName == "CoreObject")
            {
                builder.WriteLine();
                builder.WriteLine("public CoreTemplate m_template;"); // 56
            }
            if (output.m_localName == "BehaviorInstance")
            {
                builder.WriteLine();
                builder.WriteLine("public BehaviorTemplate m_behaviorTemplate;");
            }
        }

        private static void WriteCustomMethods(IndentedTextWriter builder, TypeOutput output)
        {
            if (output.m_localName == "BehaviorInstance")
            {
                builder.WriteLine();
                builder.WriteLine("public virtual void SetTemplate(BehaviorTemplate behaviorTemplate)");
                builder.WriteLine("{");
                builder.Indent++;
                builder.WriteLine("m_behaviorTemplate = behaviorTemplate;");
                builder.WriteLine("if (behaviorTemplate != null) m_behaviorTemplateNameID = SerializerFile.HashString(behaviorTemplate.m_behaviorName);");
                builder.Indent--;
                builder.WriteLine("}");
            }
        }
        
        private static HashSet<string> WriteFields(TextWriter builder, TypeOutput output)
        {
            HashSet<string> writtenProps = new HashSet<string>();
            foreach (Property property in output.m_classDef.m_properties)
            {
                if (IsDisallowedType(property.m_type))
                {
                    if (s_unkTypes.Add(property.m_type))
                    {
                        Console.Out.WriteLine($"disallowing property {property.m_name} with type {property.m_type}");
                    }
                    continue;
                }

                var defaultValue = property.GetDefaultValue();
                
                if (s_basicTypes.TryGetValue(property.m_type, out var csType))
                {
                    if (csType.m_name != null) // don't write field if no type. read exception will still be thrown
                    {
                        DefineField(builder, property, csType.m_name, defaultValue);
                    }
                    writtenProps.Add(property.m_name);
                    continue;
                }

                var objType = GetObjectValueType(property.m_type);
                if (objType != null)
                {
                    if (objType.m_type != UserDefinedTypeRef.Type.Enum)
                    {
                        Debug.Assert(defaultValue == null);
                    }
                    DefineField(builder, property, objType.m_typeName, defaultValue);
                    writtenProps.Add(property.m_name);
                } else if (s_unkTypes.Add(property.m_type))
                {
                    Console.Out.WriteLine($"unknown type \"{property.m_type}\"");
                }
            }
            return writtenProps;
        }

        private static void DefineField(TextWriter builder, Property property, string type, string defaultValue)
        {
            if (property.IsVecList())
            {
                Debug.Assert(defaultValue == null);
                builder.WriteLine($"public List<{type}> {property.m_name} = new List<{type}>();");
            } else
            {
                if (defaultValue != null)
                {
                    builder.WriteLine($"public {type} {property.m_name} = {defaultValue};");
                } else
                {
                    builder.WriteLine($"public {type} {property.m_name};");
                }
            }
        }
        
        private static void WriteBinaryMethod(IndentedTextWriter builder, TypeOutput output, HashSet<string> writtenProps)
        {
            builder.WriteLine("public override bool DeserializeBinaryField(SerializerBinaryInstance serializer, uint hash)");
            builder.WriteLine("{");
            builder.Indent++;
            builder.WriteLine("if (base.DeserializeBinaryField(serializer, hash)) return true;");
            
            if (writtenProps.Count == 0)
            {
                goto END;
            }
            
            WriteDeserializePropertySwitch(builder, writtenProps, new SwitchConfig
            {
                m_class = output.m_classDef,
                m_deserializerSymbol = "serializer",
                m_passThroughParam = null,
                m_switchVar = "hash",
                GetCaseLabel = CaseLabel_Hash,
                m_overrideBasicTypeMethods = s_buiMethods,
                m_read = true
            });

            END:
            builder.WriteLine("return false;");
            builder.Indent--;
            builder.WriteLine("}");
        }

        private static void WriteXMLMethod(IndentedTextWriter builder, TypeOutput output, HashSet<string> writtenProps)
        {
            builder.WriteLine("public override bool DeserializeXMLField(XmlNode node)");
            builder.WriteLine("{");
            builder.Indent++;
            builder.WriteLine("if (base.DeserializeXMLField(node)) return true;");
            
            if (writtenProps.Count == 0)
            {
                goto END;
            }

            WriteDeserializePropertySwitch(builder, writtenProps, new SwitchConfig
            {
                m_class = output.m_classDef,
                m_deserializerSymbol = "SerializerXML",
                m_passThroughParam = "node",
                m_switchVar = "node.Name",
                GetCaseLabel = CaseLabel_Name,
                m_read = true
            });

            END:
            builder.WriteLine("return false;");
            builder.Indent--;
            builder.WriteLine("}");
        }

        private static void WriteFlatFlagOptimisation(IndentedTextWriter builder, TypeOutput output, string prefix)
        {
            HashSet<uint> doneFlags = new HashSet<uint>();
            foreach (Property property in output.m_classDef.m_properties.OrderBy(x => x.m_flags))
            {
                if (doneFlags.Add(property.m_flags))
                {
                    builder.WriteLine($"var {prefix}{property.m_flags:X} = (serializer.m_allowedPropertyFlags & 0x{property.m_flags:X}) == serializer.m_allowedPropertyFlags;");
                    doneFlags.Add(property.m_flags);
                }
            }
        }

        private static void WriteFlatBinaryMethod(IndentedTextWriter builder, TypeOutput output, HashSet<string> writtenProps)
        {
            builder.WriteLine("public override void DeserializeBinaryFlat(SerializerBinaryInstance serializer)");
            builder.WriteLine("{");
            builder.Indent++;
            builder.WriteLine("base.DeserializeBinaryFlat(serializer);");

            var writerConfig = new PropertyWriterConfig
            {
                m_class = output.m_classDef,
                m_deserializerSymbol = "serializer",
                m_passThroughParam = null,
                m_overrideBasicTypeMethods = s_buiMethods,
                m_read = true
            };

            if (writtenProps.Count != output.m_classDef.m_properties.Count)
            {
                // won't work :(
                builder.WriteLine("throw new NotImplementedException(\"missing fields\");");
            } else
            {
                WriteFlatFlagOptimisation(builder, output, "read");
                
                foreach (var property in output.m_classDef.m_properties)
                {
                    if ((property.m_flags & PropertyClass.PROPERTY_FLAGS_OPTIONAL) != 0)
                    {
                        builder.Write($"if (read{property.m_flags:X} && serializer.ReadBool()) ");
                    } else
                    {
                        builder.Write($"if (read{property.m_flags:X}) ");
                    }
                    WritePropertyDeserializer(builder, property, writerConfig);
                }
            }

            builder.Indent--;
            builder.WriteLine("}");
        }

        private static void WriteSerializeBinaryMethod(IndentedTextWriter builder, TypeOutput output, HashSet<string> writtenProps)
        {
            builder.WriteLine("public override void SerializeBinary(SerializerBinaryInstance serializer)");
            builder.WriteLine("{");
            builder.Indent++;
            builder.WriteLine("base.SerializeBinary(serializer);");
            
            var writerConfig = new PropertyWriterConfig
            {
                m_class = output.m_classDef,
                m_deserializerSymbol = "serializer",
                m_passThroughParam = null,
                m_overrideBasicTypeMethods = s_buiMethods,
                m_read = false
            };
            
            if (writtenProps.Count != output.m_classDef.m_properties.Count)
            {
                // won't work :(
                builder.WriteLine("throw new NotImplementedException(\"missing fields\");");
            } else
            {
                WriteFlatFlagOptimisation(builder, output, "write");
                foreach (var property in output.m_classDef.m_properties)
                {
                    builder.WriteLine($"if (write{property.m_flags:X}) serializer.WriteProperty(0x{property.m_hash:X8}, 0x{property.m_flags:X}, () => {{");
                    builder.Indent++;
                    WritePropertyDeserializer(builder, property, writerConfig);
                    builder.Indent--;
                    builder.WriteLine("});");
                }
            }

            builder.Indent--;
            builder.WriteLine("}");
        }

        public class PropertyWriterConfig
        {
            public ClassDef m_class;
            public string m_deserializerSymbol;
            public string m_passThroughParam;

            public Dictionary<string, string> m_overrideBasicTypeMethods;
            //public string m_overrideBasicTypeDeserializer;

            public bool m_read;
        }
        
        public class SwitchConfig : PropertyWriterConfig
        {
            public Func<Property, string> GetCaseLabel;
            public string m_switchVar;
        }

        private static string CaseLabel_Hash(Property property) => $"0x{property.m_hash:X8}";
        private static string CaseLabel_Name(Property property) => $"\"{property.m_name}\"";

        private static bool WritePropertyDeserializer(IndentedTextWriter builder, Property property, PropertyWriterConfig config)
        {
            bool read = config.m_read;
            
            string passThruStr = string.Empty;
            string passThruStrCom = string.Empty;
            if (config.m_passThroughParam != null)
            {
                passThruStr = $"{config.m_passThroughParam}";
                passThruStrCom = $"{passThruStr}, ";
            }

            string readMethod;
            string deserializer = config.m_deserializerSymbol;
            if (s_basicTypes.TryGetValue(property.m_type, out var csType))
            {
                if (config.m_read) readMethod = csType.m_readMethod;
                else readMethod = csType.m_writeMethod;

                if (config.m_overrideBasicTypeMethods != null &&
                    config.m_overrideBasicTypeMethods.TryGetValue(property.m_type, out var basicTypeOverride))
                {
                    //if (config.m_overrideBasicTypeDeserializer != null) deserializer = config.m_overrideBasicTypeDeserializer;
                    readMethod = basicTypeOverride;

                    if (read) readMethod = $"Read{readMethod}";
                    else readMethod = $"Write{readMethod}";
                }
                
                if (readMethod == null)
                {
                    builder.WriteLine(
                        $"throw new {nameof(NotImplementedException)}(\"{property.m_type}\");");
                    return false; // suppress return
                }
            } else
            {
                var objType = GetObjectValueType(property.m_type);
                if (objType == null) throw new InvalidDataException();
                
                if (objType.m_type == UserDefinedTypeRef.Type.Enum)
                {
                    if (read) readMethod = "ReadEnum";
                    else readMethod = "WriteEnum";

                    passThruStr = passThruStrCom + $"{property.m_size}";
                    passThruStrCom = passThruStr + ", ";

                    if (!property.IsVecList() && read)
                    {
                        // needs out
                        builder.WriteLine($"{deserializer}.{readMethod}({passThruStr}, out {property.m_name});");
                        return true;
                    }
                } else
                {
                    if (read) readMethod = "ReadObject";
                    else readMethod = "WriteObject";
                    
                    if (!property.IsVecList() && read)
                    {
                        // needs cast
                        builder.WriteLine($"{property.m_name} = ({objType.m_typeName}){deserializer}.{readMethod}({passThruStr});");
                        return true;
                    }
                }
            }
            
            if (property.IsVecList())
            {
                string refString = string.Empty;
                if (read)
                {
                    // todo: not using ref for writing
                    refString = "ref ";
                }
                builder.WriteLine($"{deserializer}.{readMethod}Vector({passThruStrCom}{refString}{property.m_name});");
            } else
            {
                if (read)
                {
                    builder.WriteLine($"{property.m_name} = {deserializer}.{readMethod}({passThruStr});");
                } else
                {
                    builder.WriteLine($"{deserializer}.{readMethod}({passThruStrCom}{property.m_name});");
                }
            }
            
            return true;
        }
        
        private static void WriteDeserializePropertySwitch(IndentedTextWriter builder, HashSet<string> writtenProps, SwitchConfig config)
        {
            builder.WriteLine();
            builder.WriteLine($"switch ({config.m_switchVar})");
            builder.WriteLine("{");
            builder.Indent++;
            
            foreach (string writtenProp in writtenProps)
            {
                var property = config.m_class.m_properties.Find(x => x.m_name == writtenProp);

                builder.WriteLine($"case {config.GetCaseLabel(property)}:");
                builder.Indent++;
                
                bool result = WritePropertyDeserializer(builder, property, config);
                if (result)
                {
                    builder.WriteLine("return true;");
                }
                builder.Indent--;
            }
            
            builder.Indent--;
            builder.WriteLine("}");
            builder.WriteLine();
        }

        public static bool IsDisallowedType(string type)
        {
            if (type.Contains("class SharedPointer")) return false; // hack override
            foreach (KeyValuePair<string,CsTypeDef> basicType in s_basicTypes)
            {
                if (type == basicType.Key)
                {
                    return false;
                }
            }

            bool result = type.Contains("<") || type.Contains(">");
            //if (result == true)
            //{
            //    // for debugging :)
            //}
            return result;
        }

        public class UserDefinedTypeRef
        {
            public string m_typeName;
            public bool m_isPtr;
            public bool m_isSharedPtr;
            public Type m_type;

            public enum Type
            {
                Class,
                Struct,
                Enum
            }

            public UserDefinedTypeRef(string type, Type typeType)
            {
                var typeObj = s_allTypes[type];
                m_typeName = typeObj.m_fullName;
                m_type = typeType;
            }
        }
        
        private static readonly Regex s_sharedPtrRegex = new Regex("^SharedPointer<(.*)>$");
        
        public static UserDefinedTypeRef GetObjectValueType(string type)
        {
            UserDefinedTypeRef.Type typeType;
            if (type.StartsWith("enum "))
            {
                typeType = UserDefinedTypeRef.Type.Enum;
            } else if (type.StartsWith("struct "))
            {
                typeType = UserDefinedTypeRef.Type.Struct;
            } else if (type.StartsWith("class "))
            {
                typeType = UserDefinedTypeRef.Type.Class;
            } else
            {
                return null;
            }
            
            type = type.Replace("class ", string.Empty);
            type = type.Replace("struct ", string.Empty);
            type = type.Replace("enum ", string.Empty);

            if (type.StartsWith("SharedPointer"))
            {
                var match = s_sharedPtrRegex.Match(type);
                if (!match.Success) throw new Exception();

                return new UserDefinedTypeRef(match.Groups[1].Value, typeType)
                {
                    m_isSharedPtr = true
                };
            }
            if (type.EndsWith("*"))
            {
                return new UserDefinedTypeRef(type.Substring(0, type.Length - 1), typeType)
                {
                    m_isPtr = true
                };
            }
            if (s_allTypes.ContainsKey(type))
            {
                return new UserDefinedTypeRef(type, typeType);
            }

            return null;
        }
    }
}