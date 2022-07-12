using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using DragonLib.IO;

namespace Open101.Net
{
    public abstract class NetSocket : IDisposable
    {
        protected Socket m_socket;
        protected byte[] m_buffer;
        private volatile bool m_closed;

        private const int c_bufferLength = 1380;
        private static readonly ArrayPool<byte> s_pool = ArrayPool<byte>.Create(c_bufferLength, 100); // todo: does this mean max 100 clients?
        
        protected NetSocket(Socket socket)
        {
            m_socket = socket;
            if (m_socket != null)
            {
                m_closed = false;
                m_buffer = s_pool.Rent(c_bufferLength);
            } else
            {
                m_closed = true;
            }
        }
        
        private delegate void SocketReadCallback(SocketAsyncEventArgs args);
        private void AsyncReadWithCallback(SocketReadCallback callback)
        {
            if (!IsOpen())
                return;

            try
            {
                using (var socketEventArgs = new SocketAsyncEventArgs())
                {
                    socketEventArgs.SetBuffer(m_buffer, 0, m_buffer.Length);
                    socketEventArgs.Completed += (sender, args) => callback(args);
                    socketEventArgs.UserToken = m_socket;
                    socketEventArgs.SocketFlags = SocketFlags.None;
                    if (!m_socket.ReceiveAsync(socketEventArgs))
                        callback(socketEventArgs);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("NetSocket", $"exception on read {ex}");
                Close();
                //Log.outException(ex);
            }
        }

        private void ReadHandlerInternal(SocketAsyncEventArgs args)
        {
            if (args.BytesTransferred == 0 || args.SocketError != SocketError.Success)
            {
                Close();
                return;
            }

            try
            {
                ReadHandler(args);
            } catch (Exception e)
            {
                Close();
                throw;
            }
            
            AsyncReadWithCallback(ReadHandlerInternal);
        }

        public void StartListening()
        {
            AsyncReadWithCallback(ReadHandlerInternal);
        }
        
        public abstract void Start();
        public abstract void Update();

        public virtual void OnSocketClosed()
        {
        }
        
        public abstract void ReadHandler(SocketAsyncEventArgs args);

        protected void AsyncWrite(byte[] data)
        {
            if (!IsOpen())
                return;

            using (var socketEventArgs = new SocketAsyncEventArgs())
            {
                socketEventArgs.SetBuffer(data, 0, data.Length);
                socketEventArgs.RemoteEndPoint = m_socket.RemoteEndPoint;
                socketEventArgs.UserToken = m_socket;
                socketEventArgs.SocketFlags = SocketFlags.None;

                m_socket.SendAsync(socketEventArgs);
            }
        }

        public bool IsOpen() => !m_closed;
        public bool IsClosed() => m_closed;

        public void Close()
        {
            if (m_socket == null)
                return;

            m_closed = true;
            
            try
            {
                m_socket.Shutdown(SocketShutdown.Both);
                m_socket.Close();
            }
            catch (Exception ex)
            {
                // Log.outDebug(LogFilter.Network, "WorldSocket.CloseSocket: {0} errored when shutting down socket: {1}", GetRemoteIpAddress().ToString(), ex.Message);
            }

            Dispose();
        }

        public void Dispose()
        {
            if (m_buffer != null)
            {
                s_pool.Return(m_buffer);
                m_buffer = null;
            }

            if (m_socket != null)
            {
                m_socket.Dispose();
                m_socket = null;
            }
        }

        public EndPoint GetRemoteEndPoint()
        {
            return m_socket.RemoteEndPoint;
        }
    }
}