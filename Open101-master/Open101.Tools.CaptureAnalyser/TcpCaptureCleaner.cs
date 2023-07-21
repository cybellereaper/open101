using System;
using System.Collections.Generic;
using Open101.Net;
using PacketDotNet;

namespace Open101.Tools.CaptureAnalyser
{
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
        private uint currentSequenceNumber;
        private PriorityQueue<TcpPacket> tcpPacketBuffer;
        public LoggingKIPacketHandler m_handler;

        public TcpCaptureCleaner()
        {
            Reset();
        }

        private void Reset()
        {
            currentSequenceNumber = 0;
            tcpPacketBuffer = new PriorityQueue<TcpPacket>();
        }

        public bool ProcessPacket(TcpPacket tcpPacket)
        {
            var data = tcpPacket.PayloadData;
            var length = data.Length;

            // Handle TCP SYN (connection started)
            if (tcpPacket.Synchronize)
            {
                currentSequenceNumber = tcpPacket.SequenceNumber + 1;
                return true;
            }

            // Handle TCP FIN (connection terminated)
            if (tcpPacket.Finished)
            {
                Console.WriteLine("<connection terminated>");
                // Reset();
                return false;
            }

            // Early exit if no data
            if (length == 0) return true;

            while (tcpPacket != null)
            {
                if (tcpPacket.SequenceNumber > currentSequenceNumber)
                {
                    Console.WriteLine("out-of-order packet {0} (expected {1}) :: queue -> {2}",
                        tcpPacket.SequenceNumber, currentSequenceNumber, tcpPacketBuffer.Count + 1);
                    tcpPacketBuffer.Enqueue(tcpPacket, tcpPacket.SequenceNumber);
                    return true;
                }

                data = tcpPacket.PayloadData;
                length = data.Length;

                // Check for old sequence number
                int rewind = (int)(currentSequenceNumber - tcpPacket.SequenceNumber);
                if (rewind > 0)
                {
                    // Sequence in the past?
                    if (length - rewind <= 0)
                    {
                        // No additional data?
                        Console.Write("duplicate packet {0}, dropping :: queue -> {1}", tcpPacket.SequenceNumber,
                            tcpPacketBuffer.Count);
                        if (tcpPacketBuffer.Count > 0) Console.Write(", next = {0}", tcpPacketBuffer.Peek().SequenceNumber);
                        Console.WriteLine();

                        tcpPacket = (tcpPacketBuffer.Count > 0) ? tcpPacketBuffer.Dequeue() : null;
                        continue;
                    }

                    // Catch up
                    length -= rewind;
                    Console.Write("catching up, +{0} bytes :: queue -> {1}", length, tcpPacketBuffer.Count);
                    if (tcpPacketBuffer.Count > 0) Console.Write(", next = {0}", tcpPacketBuffer.Peek().SequenceNumber);
                    Console.WriteLine();

                    Array.Copy(data, rewind, data, 0, length);
                    Array.Resize(ref data, length);
                }

                // Advance expected sequence number
                currentSequenceNumber += (uint)length;

                // Get the next packet in the queue
                tcpPacket = (tcpPacketBuffer.Count > 0) ? tcpPacketBuffer.Dequeue() : null;
                if (tcpPacket != null)
                {
                    Console.Write("fetching buffered packet :: queue -> {0}", tcpPacketBuffer.Count);
                    if (tcpPacketBuffer.Count > 0) Console.Write(", next = {0}", tcpPacketBuffer.Peek().SequenceNumber);
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
