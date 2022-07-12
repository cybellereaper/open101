using System;
using System.Buffers.Binary;
using Open101.IO;

namespace Open101.Net
{
    public class FoodFrameHandler
    {
        private int m_lengthRecon = -1;
        private int m_recvOffset = 0;
        
        private byte[] m_recvBuffer;
        private int m_recvBufferLength;

        private const int c_foodHeaderSize = 4;
        private readonly byte[] m_foodHeader = new byte[c_foodHeaderSize];
        private byte m_foodWaitingCount = c_foodHeaderSize;

        public void StartRecon(int length)
        {
            if (length > m_recvBufferLength)
            {
                m_recvBuffer = new byte[length];
                m_recvBufferLength = length;
            }
        }
        
        public bool ConsumeBuffer(byte[] buffer, int length)
        {
            int thisOffset = 0;
            while (thisOffset < length)
            {
                while (m_foodWaitingCount > 0) // the header can be split across multiple packets, but we need it all before we start reading
                {
                    m_foodHeader[c_foodHeaderSize-m_foodWaitingCount] = buffer[thisOffset];
                    m_foodWaitingCount--;
                    thisOffset++;
                    if (thisOffset >= length) return true;
                }

                if (m_lengthRecon == -1)
                {
                    var foodSpan = new Span<byte>(m_foodHeader);
                    var header = BinaryPrimitives.ReadUInt16LittleEndian(foodSpan.Slice(0, 2));
                    if (header != 0xF00D)
                    {
                        m_lengthRecon = -1;
                        m_recvOffset = 0;
                        m_foodWaitingCount = c_foodHeaderSize;
                        //File.WriteAllBytes("not_food.bin", buffer);
                        return false;
                    }
                    
                    m_lengthRecon = BinaryPrimitives.ReadUInt16LittleEndian(foodSpan.Slice(2, 2));
                    
                    if (m_lengthRecon > m_recvBufferLength)
                    {
                        m_recvBuffer = new byte[m_lengthRecon];
                        m_recvBufferLength = m_lengthRecon;
                    }
                }

                int copyBytes = length - thisOffset;
                if (copyBytes > m_lengthRecon-m_recvOffset)
                {
                    copyBytes = m_lengthRecon-m_recvOffset;
                }
                Array.Copy(buffer, thisOffset, m_recvBuffer, m_recvOffset, copyBytes);
                m_recvOffset += copyBytes;
                thisOffset += copyBytes;

                if (m_recvOffset == m_lengthRecon)
                {
                    HandleFullMessage(m_recvBuffer, m_lengthRecon);
                    m_lengthRecon = -1;
                    m_recvOffset = 0;
                    m_foodWaitingCount = c_foodHeaderSize;

                    // for debug
                    /*for (int i = 0; i < m_recvBufferLength; i++)
                    {
                        m_recvBuffer[i] = 0;
                    }*/
                }
            }
            return true;
        }

        protected static void SerializeFoodFrame(ByteBuffer output, ByteBuffer payload)
        {
            var payloadBytes = payload.GetData();
            if (payloadBytes.Length > ushort.MaxValue) throw new ArgumentException($"payloadBytes.Length > ushort.MaxValue ({payloadBytes.Length})");
            output.WriteUInt16((ushort)0xF00Du);
            output.WriteUInt16((ushort)payloadBytes.Length);
            output.WriteBytes(payloadBytes);
        }

        protected virtual void HandleFullMessage(byte[] buffer, int length)
        {
            
        }
    }
}