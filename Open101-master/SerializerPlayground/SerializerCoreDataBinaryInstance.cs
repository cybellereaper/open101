using System.Collections.Generic;
using System.Linq;
using Open101.IO;
using Open101.Serializer;
using Open101.Serializer.PropertyClass;

namespace SerializerPlayground
{
    public class SerializerCoreDataBinaryInstance : SerializerBinaryInstance
    {
        public SerializerCoreDataBinaryInstance(ByteBuffer buffer, uint allowedPropertyFlags=DEFAULT_ALLOWED_PROP_FLAGS) : base(buffer, allowedPropertyFlags)
        {
            m_useFlat = true;
            m_flagBinaryEnums = true;
            m_flagFixedCountSize = true;
        }

        public static void InitCoreObjectFromTemplate(CoreObject coreObject)
        {
            InitCoreObjectFromTemplateID(coreObject, (uint) (coreObject.m_templateID.m_value & 0xFFFFFFFF));
        }

        public static void InitCoreObjectFromTemplateID(CoreObject coreObject, uint templateID)
        {
            coreObject.m_template = GetTemplate(templateID);
            InitCoreObjectBehaviors(coreObject);
        }

        public static CoreTemplate GetTemplate(ulong templateID)
        {
            var templateLocation = Program.s_templateManifest.GetLocation((uint)(templateID & 0xFFFFFFFF));
            CoreTemplate template = SerializerFile.OpenClass<CoreTemplate>(templateLocation.m_filename);
            return template;
        }

        public static void InitCoreObjectBehaviors(CoreObject coreObject)
        {
            var behaviors = coreObject.m_template.m_behaviors.ToList(); // todo: avoid alloc?
            if (coreObject is WizClientLeashedObject)
            {
                // hmmmmmmmmm. pls check
                behaviors.Add(new BehaviorTemplate
                {
                    m_behaviorName = "LeashBehavior"
                });
            }
            
            coreObject.m_inactiveBehaviors = new List<BehaviorInstance>(behaviors.Count);
            foreach (BehaviorTemplate behavior in behaviors)
            {
                if (behavior == null)
                {
                    coreObject.m_inactiveBehaviors.Add(null);
                } else
                {
                    var behaviorInstance =
                        (BehaviorInstance) PropertyClassRegistry.s_inst.AllocateBehavior(
                            SerializerFile.HashString(behavior.m_behaviorName));
                    // can be null for server types
                    behaviorInstance?.SetTemplate(behavior);
                    coreObject.m_inactiveBehaviors.Add(behaviorInstance);
                }
            }
        }
        
        public override bool PreLoadObject(ref PropertyClass propertyClass)
        {
            var classID = m_buffer.ReadUInt8();
            var unknown2 = m_buffer.ReadUInt8();
            var templateOrTypeHash = m_buffer.ReadUInt32();

            if (classID == 0 && templateOrTypeHash == 0)
            {
                propertyClass = null;
                return false;
            }

            if (propertyClass == null)
            {
                if (classID != 0)
                {
                    var coreObject = (CoreObject) PropertyClassRegistry.s_inst.AllocateCoreObject(classID);
                    InitCoreObjectFromTemplateID(coreObject, templateOrTypeHash);
                    propertyClass = coreObject;
                    return true;
                }
                else
                {
                    propertyClass = AllocatePropertyClass(templateOrTypeHash);
                    return true;
                }
            }
            // propertyclass is already created - ez clap
            return true;
        }

        public override bool PreWriteObject(PropertyClass propertyClass)
        {
            byte classID = 0;
            byte unk2 = 0;
            uint templateOrTypeHash = 0;
            bool result;

            if (propertyClass == null)
            {
                result = false;
            } else
            {
                // todo: what is unk2
                
                var coreType = propertyClass.GetCoreType();
                var behaviorType = propertyClass.GetBehaviorInstanceHash();
                if (coreType != 0)
                {
                    classID = coreType;
                    var coreObject = (CoreObject) propertyClass;
                    templateOrTypeHash = (uint)(coreObject.m_templateID.m_value & 0xFFFFFFFF); // todo: low = template id??
                } else if (behaviorType != 0)
                {
                    // wont be read
                    templateOrTypeHash = behaviorType;
                } else
                {
                    templateOrTypeHash = propertyClass.GetHash();
                }
                if (propertyClass.GetHash() == 0) return false; // intentionally do not serialize
                result = true;
            }
            
            m_buffer.WriteUInt8(classID);
            m_buffer.WriteUInt8(unk2);
            m_buffer.WriteUInt32(templateOrTypeHash);

            return result;
        }
    }
}