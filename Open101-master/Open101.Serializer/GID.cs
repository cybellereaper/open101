namespace Open101.Serializer
{
    public struct GID
    {
        public ulong m_value;

        public GID(ulong val)
        {
            m_value = val;
        }
        
        public static implicit operator ulong(GID gid) {
            return gid.m_value;
        }
        
        public static explicit operator GID(ulong gid) {
            return new GID(gid);
        }
    }
}