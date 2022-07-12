using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using DragonLib.IO;

namespace Open101.Net
{
    public class SocketManager<TSocketType, TAcceptor> where TSocketType : NetSocket
        where TAcceptor : class, INetAcceptor, new()

    {
        private TAcceptor m_acceptor;
        private List<TSocketType> m_sockets = new List<TSocketType>();
        private ushort m_port;
        
        public virtual bool Start(IPAddress bindIp, ushort port)
        {
            m_port = port;
            
            m_acceptor = new TAcceptor();
            if (!m_acceptor.Start(bindIp, port))
            {
                Logger.Error("Net",
                    $"{nameof(SocketManager<TSocketType, TAcceptor>)}:{nameof(Start)} failed to start acceptor");
                return false;
            }

            m_acceptor.AsyncAcceptSocket(OnSocketOpen);

            return true;
        }

        public virtual void Stop()
        {
            m_acceptor.Stop();
            m_acceptor = null;
            
            lock (m_sockets)
            {
                foreach (TSocketType socket in m_sockets)
                {
                    socket.Close();
                    socket.OnSocketClosed();
                }
                m_sockets = null;
            }
        }

        private void OnSocketOpen(Socket sock)
        {
            try
            {
                TSocketType newSocket = (TSocketType) Activator.CreateInstance(typeof(TSocketType), sock);
                newSocket.Start();
                newSocket.StartListening();

                lock (m_sockets)
                {
                    m_sockets.Add(newSocket);
                }
            } catch (Exception e)
            {
                Logger.Error("SocketManager", $"unable to start socket, {e}");
                sock.Close();
            }
        }

        public void UpdateAll()
        {
            lock (m_sockets)
            {
                List<TSocketType> toRemove = new List<TSocketType>();
                foreach (TSocketType socket in m_sockets)
                {
                    socket.Update();
                    if (socket.IsClosed())
                    {
                        socket.OnSocketClosed();
                        toRemove.Add(socket);
                    }
                }

                foreach (TSocketType socket in toRemove)
                {
                    m_sockets.Remove(socket);
                }
            }
        }

        public int GetSocketCount()
        {
            lock (m_sockets)
            {
                return m_sockets.Count;
            }
        }

        public ushort GetPort() => m_port;
    }
}