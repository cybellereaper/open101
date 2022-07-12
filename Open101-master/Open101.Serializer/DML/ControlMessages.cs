using System.Diagnostics;
using Open101.IO;

namespace Open101.Serializer.DML
{
    public class ControlNetworkService : INetworkService
    {
        public const byte c_serviceID = 0;
        public byte GetID() => c_serviceID;

        public class SessionOffer : INetworkMessage
        {
            public const byte c_messageID = 0;
            public byte GetServiceID() => c_serviceID;

            public byte GetID() => c_messageID;

            [DMLField("SessionID", DMLType.USHRT)] public ushort m_sessionID;
            [DMLField("Unknown1", DMLType.UINT)] public uint m_unknown1;
            [DMLField("Timestamp", DMLType.UINT)] public uint m_timestamp;
            [DMLField("Milliseconds", DMLType.UINT)] public uint m_milliseconds;
            
            public void Serialize(ByteBuffer buf)
            {
                DMLRecordReader<SessionOffer>.Write(buf, this);
            }
            
            public void Deserialize(ByteBuffer buf)
            {
                DMLRecordReader<SessionOffer>.Read(buf, this);
            }
        }
    
        public class KeepAlive : INetworkMessage
        {
            public const byte c_messageID = 3;
            public byte GetServiceID() => c_serviceID;

            public byte GetID() => c_messageID;
        
            [DMLField("SessionID", DMLType.USHRT)] public ushort m_sessionID;
            [DMLField("Milliseconds", DMLType.USHRT)] public ushort m_milliseconds;
            [DMLField("Minutes", DMLType.USHRT)] public ushort m_minutes;
            
            public void Serialize(ByteBuffer buf)
            {
                DMLRecordReader<KeepAlive>.Write(buf, this);
            }
            
            public void Deserialize(ByteBuffer buf)
            {
                DMLRecordReader<KeepAlive>.Read(buf, this);
            }
        }
    
        public class KeepAliveResponse : INetworkMessage
        {
            public const byte c_messageID = 4;
            public byte GetServiceID() => c_serviceID;
            public byte GetID() => c_messageID;
        
            [DMLField("Unknown1", DMLType.USHRT)] public ushort m_unknown1;
            [DMLField("Timestamp", DMLType.UINT)] public uint m_timestamp;
            
            public void Serialize(ByteBuffer buf)
            {
                DMLRecordReader<KeepAliveResponse>.Write(buf, this);
            }
            
            public void Deserialize(ByteBuffer buf)
            {
                DMLRecordReader<KeepAliveResponse>.Read(buf, this);
            }
        }
    
        public class SessionAccept : INetworkMessage
        {
            public const byte c_messageID = 5;
            public byte GetServiceID() => c_serviceID;
            public byte GetID() => c_messageID;

            [DMLField("Unknown1", DMLType.USHRT)] public ushort m_unknown1;
            [DMLField("Unknown2", DMLType.UINT)] public uint m_unknown2;
            [DMLField("Timestamp", DMLType.UINT)] public uint m_timestamp;
            [DMLField("Milliseconds", DMLType.UINT)] public uint m_milliseconds;
            [DMLField("SessionID", DMLType.USHRT)] public ushort m_sessionID;
            
            public void Serialize(ByteBuffer buf)
            {
                DMLRecordReader<SessionAccept>.Write(buf, this);
            }
            
            public void Deserialize(ByteBuffer buf)
            {
                DMLRecordReader<SessionAccept>.Read(buf, this);
            }
        }
        
        public INetworkMessage AllocateMessage(byte id)
        {
            switch (id)
            {
                case 0: return new SessionOffer();
                case 3: return new KeepAlive();
                case 4: return new KeepAliveResponse();
                case 5: return new SessionAccept();
                default: return null;
            }
        }

        public bool Dispatch(object handlerVoid, INetworkMessage message)
        {
            Debug.Assert(message.GetServiceID() == c_serviceID);
            var handler = (Handler)handlerVoid;

            switch (message.GetID())
            {
                case SessionOffer.c_messageID:
                    return handler.NetHandleSessionOffer((SessionOffer) message);
                case KeepAlive.c_messageID:
                    return handler.NetHandleKeepAlive((KeepAlive) message);
                case KeepAliveResponse.c_messageID:
                    return handler.NetHandleKeepAliveResponse((KeepAliveResponse) message);
                case SessionAccept.c_messageID:
                    return handler.NetHandleSessionAccept((SessionAccept) message);
            }
            return false;
        }

        public interface Handler
        {
            bool NetHandleSessionOffer(SessionOffer msg) => false;
            bool NetHandleKeepAlive(KeepAlive msg) => false;
            bool NetHandleKeepAliveResponse(KeepAliveResponse msg) => false;
            bool NetHandleSessionAccept(SessionAccept msg) => false;
        }
    }
}