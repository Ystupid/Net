using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.General.Config
{
    public class ClientConfig : ConfigBase
    {
        public static readonly ClientConfig DefaultConfig = new ClientConfig
        {
            ConnectionType = ConnectionType.TCP,
            Host = "127.0.0.1",
            Port = 0,
            SendBufferSize = 1024,
            ReceiveBufferSize = 1024,
        };

        public ConnectionType ConnectionType { get; set; }
        public string Host { get; set; }
        public int SendBufferSize { get; set; }
        public int ReceiveBufferSize { get; set; }
    }
}
