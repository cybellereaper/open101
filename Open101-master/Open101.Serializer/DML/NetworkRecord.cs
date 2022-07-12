using Open101.IO;

namespace Open101.Serializer.DML
{
    public interface INetworkMessage
    {
        byte GetID();
        byte GetServiceID();
        void Serialize(ByteBuffer buf);
        void Deserialize(ByteBuffer buf);
    }

    public interface INetworkService
    {
        byte GetID();
        INetworkMessage AllocateMessage(byte id);
        bool Dispatch(object handlerVoid, INetworkMessage message);
    }
}