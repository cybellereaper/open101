using Open101.IO;
using Open101.Net;
using Open101.Serializer;
using Open101.Serializer.PropertyClass;
using SerializerPlayground;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using wiz101_server.sockets;

namespace wiz101_server
{
    class Program
    {
        public static IPAddress LOGIN_IP;
        public static IPAddress GAME_IP;
        public static IPAddress LOOPBACK;

        public static ushort LOGIN_PORT;
        public static ushort GAME_PORT;

        static void Main(string[] args)
        {
            // Command line argument structure:
            //      [Game dir] [login ip] [login port] [game ip] [game port]
            // (Sorry it's so much now. Will use a config file, I promise)

            // Only used for server transfer. Server itself runs on loopback for now
            LOGIN_IP = IPAddress.Parse(args[1]);
            LOGIN_PORT = ushort.Parse(args[2]);
            
            GAME_IP = IPAddress.Parse(args[3]);
            GAME_PORT = ushort.Parse(args[4]);

            LOOPBACK = IPAddress.Parse("0.0.0.0");

            ResourceManager.SetGameDir(args[0]);
            SerializerPlayground.Program.Init();

            var login_s = new SocketManager<LoginSocket, AsyncTcpAcceptor>();
            login_s.Start(LOOPBACK, LOGIN_PORT);

            var game_s = new SocketManager<GameSocket, AsyncTcpAcceptor>();
            game_s.Start(LOOPBACK, GAME_PORT);

            LocalRealm.realm = new ServerRealm(100, 50, 10);

            bool running = true;
            while (running)
            {
                login_s.UpdateAll();
                game_s.UpdateAll();

                Thread.Sleep(250);
            }
        }
    }
}
