using System;
using System.Linq;
using System.IO;
using Open101.IO;
using Open101.Serializer.PropertyClass;

namespace SerializerPlayground
{
    public static class Program
    {
        public static TemplateManifest s_templateManifest;

        public static void Init()
        {
            //PropertyClassRegistry.SetRegistry(new ExtendedWizardPropertyClassRegistry());
            PropertyClassRegistry.SetRegistry(new WizardPropertyClassRegistry());
            s_templateManifest = SerializerFile.OpenClass<TemplateManifest>("TemplateManifest.xml");
        }

        private static void LoadZone()
        {
            //var zoneWad = ResourceManager.GetWad("WizardCity-WC_Streets-WC_Unicorn");
            //var zoneWad = ResourceManager.GetWad("WizardCity-WC_Hub");
            //var zoneWad = ResourceManager.GetWad("WizardCity-WC_Ravenwood");
            //var zoneWad = ResourceManager.GetWad("Krokotopia-KT_HubFlyingShip_ToIsland");
            //var zoneWad = ResourceManager.GetWad("Krokotopia-KT_WorldTeleporter");
            var zoneWad = ResourceManager.GetWad("Krokotopia-KT_Hub");
            //var zoneWad = ResourceManager.GetWad("Krokotopia-KT_Hub_Crypt");
            //var zoneWad = ResourceManager.GetWad("WizardCity-Interiors-WC_Headmistress_House");

            WizZoneData zoneData;
            using (Stream flatFile = zoneWad.OpenFile("gamedata.bin"))
            {
                var buf = new ByteBuffer(flatFile);
                var inst = new SerializerBinaryInstance(buf, 7)
                {
                    m_flagBinaryEnums = true, m_flagFixedCountSize = true
                };
                zoneData = (WizZoneData)SerializerBinary.ReadObject(inst);
                
                using var writeBuffer = new ByteBuffer();
                var newInst = new SerializerBinaryInstance(writeBuffer, 7)
                {
                    m_flagBinaryEnums = true, m_flagFixedCountSize = true
                };
                newInst.WriteObject(zoneData);
                
                writeBuffer.SwapToRead();
                writeBuffer.GetCurrentStream().Position = 0;
                var newReInst = new SerializerBinaryInstance(writeBuffer, 7)
                {
                    m_flagBinaryEnums = true, m_flagFixedCountSize = true
                };
                var propClass2 = (WizZoneData)SerializerBinary.ReadObject(newReInst);

                //buf.Dispose();
            }
            var zoneSpawnData = SerializerFile.OpenClass<SpawnManager>(zoneWad, "spawnData.xml");
            var zoneClientSpawnData = SerializerFile.OpenClass<SpawnManager>(zoneWad, "clientSpawnData.xml");
            var zoneTriggers = SerializerFile.OpenClass<TriggerList>(zoneWad, "triggers.xml");
            var zoneVolumes = SerializerFile.OpenClass<TriggerVolumeList>(zoneWad, "volumes.xml");
            
            
            //var elixir = SerializerFile.OpenClass<PropertyClass>(@"ObjectData\Elixirs\Elixir-Level50.xml");
            //var elixir2 = SerializerFile.OpenClass<PropertyClass>(@"ObjectData\Elixirs\Elixir-AddCharacterSlot.xml");
            //var elixir3 = SerializerFile.OpenClass<PropertyClass>(@"ObjectData\Elixirs\Elixir-JoelTest.xml");
            //var elixir4 = SerializerFile.OpenClass<PropertyClass>(@"ObjectData\Elixirs\Elixir-PrepaidTime-1-Month.xml");
            //var test = ((LevelUpElixirBehaviorTemplate)((WizItemTemplate) elixir).m_behaviors[0]).m_allSchoolData
            //    .m_propertyRegistryEntires.Select(x => $"{x.m_registryEntryName} - {x.m_value} - {x.m_questRegistry} - {x.m_questName}").ToArray();
        }
        
        public static void Main(string[] args)
        {
            var bat = SerializerFile.HashString("Bat");
            
            Init();
            LoadZone();

            using (Stream coreObjectFile = File.OpenRead(@"D:\re\wiz101\OpenWizard101\TempServer\bin\Debug\netcoreapp3.1\player_data_mod.bin"))
            {
                var buf = new ByteBuffer(coreObjectFile);
                var inst = new SerializerCoreDataBinaryInstance(buf);

                var propClass = (ClientObject)SerializerBinary.ReadObject(inst);
                //buf.Dispose();

                using var writeBuffer = new ByteBuffer();
                var newInst = new SerializerCoreDataBinaryInstance(writeBuffer);
                newInst.WriteObject(propClass);
                
                writeBuffer.SwapToRead();
                writeBuffer.GetCurrentStream().Position = 0;
                var newReInst = new SerializerCoreDataBinaryInstance(writeBuffer);
                var propClass2 = (ClientObject)SerializerBinary.ReadObject(newReInst);
                
                Console.Out.WriteLine($"{buf.GetCurrentStream().Length} {writeBuffer.GetCurrentStream().Length}");
            }

            //var npcServices = SerializerFile.OpenClass<NPCService>("NPCServices.xml");
            //var fishingXP = SerializerFile.OpenClass<PropertyClass>("FishingXPConfig.xml");
            //var classSelectGUI = SerializerFile.OpenClass<Window>("GUI/CreationClassSelect.gui");
            //var worldHubZones = SerializerFile.OpenClass<WorldHubZoneMapper>("WorldHubZones.xml");
        }
    }
}