using Net.General;
using Net.General.Config;
using Net.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net
{
    class Program
    {
        static void Server()
        {
            var config = ServerConfig.DefaultConfig;
            config.Port = 6666;
            config.ConnectionType = ConnectionType.KCP;
            var netManager = new NetManager(config);
            netManager.Start();

            Task.Run(async () =>
            {
                while (true)
                {
                    netManager.Tick();

                    await Task.Delay(10);
                }
            });
            
        }

        static void Client()
        {
            var config = ClientConfig.DefaultConfig;
            config.Port = 6666;
            config.ConnectionType = ConnectionType.KCP;
            var netManager = new Client.NetManager(config);
            netManager.ConnectAsync();

            Task.Run(async () =>
            {
                while (true)
                {
                    netManager.Tick();

                    await Task.Delay(10);
                }
            });

            Task.Run(async () =>
            {
                while (true)
                {
                    netManager.SendAsync(Encoding.UTF8.GetBytes("呵呵"));

                    await Task.Delay(1000);
                }
            });
        }

        static void Main(string[] args)
        {

            Server();

            Console.ReadKey();

            //for (int i = 0; i < 1000; i++)
            //{
                Client();
            //}

            Console.ReadKey();
        }
    }
}
