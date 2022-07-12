using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading;
using Open101.Net;
using Open101.Serializer;

namespace TempServer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            SerializerPlayground.Program.Init();
            
            var login = new SocketManager<LoginSocket, AsyncTcpAcceptor>();
            login.Start(IPAddress.Parse("0.0.0.0"), 12001);
            
            var zone = new SocketManager<GameSocket, AsyncTcpAcceptor>();
            zone.Start(IPAddress.Parse("0.0.0.0"), 12002);

            while (true)
            {
                login.UpdateAll();
                zone.UpdateAll();
                
                Thread.Sleep(250);
            }
        }

        public static ByteString HexStr(string hex)
        {
            List<byte> outEww = new List<byte>();
            foreach (string b in hex.Split())
            {
                outEww.Add(byte.Parse(b, NumberStyles.HexNumber));
            }
            return outEww.ToArray();
        }
    }
}