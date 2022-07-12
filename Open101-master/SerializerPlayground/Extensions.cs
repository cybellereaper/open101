using System.Linq;

namespace SerializerPlayground
{
    public static class Extensions
    {
        public static TemplateLocation GetLocation(this TemplateManifest manifest, uint templateID)
        {
            return manifest.m_serializedTemplates.FirstOrDefault(x => x.m_id == templateID);
        }
    }
}