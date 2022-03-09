using Net.Lockstep.Data;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Net.General.Data
{
    public class NetProtocol
    {
        [ProtoContract]
        public struct S2C_Connect
        {
            [ProtoMember(1)] public string Host { get; set; }
            [ProtoMember(2)] public int Port { get; set; }

            public IPEndPoint RemoteEndPoint
            {
                get => new IPEndPoint(IPAddress.Parse(Host), Port);
                set
                {
                    Host = value.Address.ToString();
                    Port = value.Port;
                }
            }
        }

        [ProtoContract]
        public class C2S_FSPData
        {
            [ProtoMember(1)] public uint SessionID { get; set; }
            [ProtoMember(2)] public List<Message> Messages { get; set; }

            public C2S_FSPData() => Messages = new List<Message>();
        }

        [ProtoContract]
        public class S2C_FSPData
        {
            [ProtoMember(1)] public List<Frame> Frames { get; set; }

            public S2C_FSPData() => Frames = new List<Frame>();
        }
    }
}
