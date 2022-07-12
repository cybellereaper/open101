using System.Net;
using System.Net.Sockets;

namespace Open101.Net
{
    public delegate void SocketAcceptDelegate(Socket newSocket);
    
    public interface INetAcceptor
    {
        bool Start(IPAddress address, ushort port);
        void Stop();

        void AsyncAcceptSocket(SocketAcceptDelegate mgrHandler);
    }
}