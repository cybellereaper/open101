using System;
using System.Collections.Generic;
using System.Net.Sockets;
using DragonLib.IO;
using Open101.IO;
using Open101.Serializer.DML;

namespace Open101.Net
{
    public class KISocket : NetSocket
    {
        public KIPacketHandler m_handler;

        public const int c_maxPacketSize = 1380;
        
        public KISocket(Socket socket) : base(socket)
        { }

        public override void Start()
        {
        }

        public override void Update()
        {
        }

        public override void ReadHandler(SocketAsyncEventArgs args)
        {
            try
            {
                if (!m_handler.ConsumeBuffer(args.Buffer, args.BytesTransferred))
                {
                    Close();
                }
            } catch (Exception e)
            {
                OnException(e);
            }
        }

        public virtual void OnException(Exception e)
        {
            Logger.Error("KISocket", $"Exception in ConsumeBuffer: {e}");
            Close();
        }
        
        public void WriteSplit(byte[] data)
        {
            var dataSpan = new Span<byte>(data);
            
            int size = data.Length;
            
            int remainingSize = size;
            for (int i = 0; i < size; i+=c_maxPacketSize)
            {
                int packetSize = Math.Min(c_maxPacketSize, remainingSize);
                AsyncWrite(dataSpan.Slice(i, packetSize).ToArray());
                remainingSize -= packetSize;
            }
        }

        public void Send(params INetworkMessage[] messages)
        {
            Send((IReadOnlyCollection<INetworkMessage>)messages);
        }
        
        public void Send(IReadOnlyCollection<INetworkMessage> messages)
        {
            using var buf = new ByteBuffer();
            foreach (INetworkMessage message in messages)
            {
                KIPacketHandler.Serialize(buf, message);
            }
            WriteSplit(buf.GetData());
        }
    }
}