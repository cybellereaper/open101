using System;
using System.Net;
using System.Net.Sockets;
using DragonLib.IO;

namespace Open101.Net
{
    public class AsyncTcpAcceptor : INetAcceptor
    {
        private TcpListener m_listener;
        private volatile bool m_stopped;
        
        public bool Start(IPAddress ip, ushort port)
        {
            try
            {
                m_listener = new TcpListener(ip, port);
                m_listener.Start();
            }
            catch (SocketException ex)
            {
                Logger.Error("AsyncTcpAcceptor", $"Error starting listener: {ex}");
                return false;
            }

            return true;
        }

        public async void AsyncAcceptSocket(SocketAcceptDelegate mgrHandler)
        {
            while (!m_stopped)
            {
                try
                {
                    var socket = await m_listener.AcceptSocketAsync();
                    if (socket != null)
                    {
                        mgrHandler(socket);
                    }
                }
                catch (ObjectDisposedException)
                { }
            }
        }

        public void Stop()
        {
            if (m_stopped)
                return;

            m_stopped = true;
            m_listener.Stop();
        }
    }
}