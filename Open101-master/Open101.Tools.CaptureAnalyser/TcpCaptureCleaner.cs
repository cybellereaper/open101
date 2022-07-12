using System;
using System.Collections.Generic;
using Open101.Net;
using PacketDotNet;

namespace Open101.Tools.CaptureAnalyser
{
    /*
     *  FROM https://github.com/Bochozkar/tera-watcher
     */
    
    public class PriorityQueue<T>
    {
        private readonly SortedList<uint, T> _list;
        public int Count => _list.Count;

        public PriorityQueue()
        {
            _list = new SortedList<uint, T>();
        }

        public void Clear()
        {
            _list.Clear();
        }

        public void Enqueue(T item, uint priority)
        {
            _list[priority] = item; // assume new data is better
        }

        public T Dequeue()
        {
            T item = Peek();
            _list.RemoveAt(0);
            return item;
        }

        public T Peek()
        {
            if (Count <= 0) throw new InvalidOperationException();
            return _list[_list.Keys[0]];
        }
    }
    
    public class TcpCaptureCleaner
    {
        private uint m_seqN;
        private PriorityQueue<TcpPacket> m_tcpBuffer;
        public LoggingKIPacketHandler m_handler;

        public TcpCaptureCleaner()
        {
            Reset();
        }

        private void Reset()
        {
            m_seqN = 0;
            m_tcpBuffer = new PriorityQueue<TcpPacket>();
        }

        public bool ProcessPacket(TcpPacket tcpPacket)
        {
            var data = tcpPacket.PayloadData;
            var length = data.Length;

            // handle TCP SYN (connection started)
            if (tcpPacket.Synchronize)
            {
                m_seqN = tcpPacket.SequenceNumber + 1;
                return true;
            }

            // handle TCP FIN (connection terminated)
            if (tcpPacket.Finished)
            {
                Console.WriteLine("<connection terminated>");
                //Reset();
                return false;
            }

            // early exit if no data
            if (length == 0) return true;

            while (tcpPacket != null)
            {
                if (tcpPacket.SequenceNumber > m_seqN)
                {
                    Console.WriteLine("out-of-order packet {0} (expected {1}) :: queue -> {2}",
                        tcpPacket.SequenceNumber, m_seqN, m_tcpBuffer.Count + 1);
                    m_tcpBuffer.Enqueue(tcpPacket, tcpPacket.SequenceNumber);
                    return true;
                }

                data = tcpPacket.PayloadData;
                length = data.Length;

                // check for old seq
                int rewind = (int) (m_seqN - tcpPacket.SequenceNumber);
                if (rewind > 0)
                {
                    // seq in past?
                    if (length - rewind <= 0)
                    {
                        // no additional data?
                        Console.Write("duplicate packet {0}, dropping :: queue -> {1}", tcpPacket.SequenceNumber,
                            m_tcpBuffer.Count);
                        if (m_tcpBuffer.Count > 0) Console.Write(", next = {0}", m_tcpBuffer.Peek().SequenceNumber);
                        Console.WriteLine();

                        tcpPacket = (m_tcpBuffer.Count > 0) ? m_tcpBuffer.Dequeue() : null;
                        continue;
                    }

                    // catch up
                    length -= rewind;
                    Console.Write("catching up, +{0} bytes :: queue -> {1}", length, m_tcpBuffer.Count);
                    if (m_tcpBuffer.Count > 0) Console.Write(", next = {0}", m_tcpBuffer.Peek().SequenceNumber);
                    Console.WriteLine();

                    Array.Copy(data, rewind, data, 0, length);
                    Array.Resize(ref data, length);
                }

                // advance expected sequence number
                m_seqN += (uint) length;

                // get next packet in queue
                tcpPacket = (m_tcpBuffer.Count > 0) ? m_tcpBuffer.Dequeue() : null;
                if (tcpPacket != null)
                {
                    Console.Write("fetching buffered packet :: queue -> {0}", m_tcpBuffer.Count);
                    if (m_tcpBuffer.Count > 0) Console.Write(", next = {0}", m_tcpBuffer.Peek().SequenceNumber);
                    Console.WriteLine();
                }
                ProcessData(data);
            }

            return true;
        }

        private void ProcessData(byte[] data)
        {
            m_handler.ConsumeBuffer(data, data.Length);
        }
    }
}