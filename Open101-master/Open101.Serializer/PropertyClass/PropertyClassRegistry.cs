namespace Open101.Serializer.PropertyClass
{
    public interface IPropertyClassRegistryInst
    {
        public PropertyClass AllocateObject(uint hash);
        public PropertyClass AllocateCoreObject(uint num);
        public PropertyClass AllocateBehavior(uint hash);
    }
    
    public static class PropertyClassRegistry
    {
        public static IPropertyClassRegistryInst s_inst;

        public static void SetRegistry(IPropertyClassRegistryInst inst)
        {
            s_inst = inst;
        }

        public static PropertyClass AllocateObject(uint hash)
        {
            return s_inst.AllocateObject(hash);
        }
    }
}