using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.General.Config
{
    public class ServerConfig : ConfigBase
    {
        public static readonly ServerConfig DefaultConfig = new ServerConfig
        {
            ConnectionType = ConnectionType.TCP,
            Port = 0,
            BackLog = 1000,
            SendBufferSize = 1024,
            ReceiveBufferSize = 1024,
        };

        public ConnectionType ConnectionType { get; set; }
        public int BackLog { get; set; }
        public int SendBufferSize { get; set; }
        public int ReceiveBufferSize { get; set; }

        public override string ToString()
        {
            var content = "";

            var type = typeof(ServerConfig);

            foreach (var propertie in type.GetProperties())
            {
                content += $"{propertie.Name}:{propertie.GetValue(this)}\n";
            }

            return content;
        }
    }
}
