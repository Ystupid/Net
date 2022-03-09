using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Lockstep.Data
{
    [ProtoContract]
    public class Message
    {
        [ProtoMember(1)] public int CommandID { get; set; }
        [ProtoMember(2)] public int[] Args { get; set; }
        [ProtoMember(3)] public int Custom { get; set; }

        public uint PlayerID
        {
            get { return (uint)Custom; }
            set { Custom = (int)value; }
        }

        public int FrameID
        {
            get { return Custom; }
            set { Custom = value; }
        }
    }
}
