using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Lockstep.Data
{
    [ProtoContract]
    public class Frame
    {
        [ProtoMember(1)] public int FrameID { get; set; }
        [ProtoMember(2)] public List<Message> MessageList { get; set; }

        public Frame() => MessageList = new List<Message>();

        public bool IsEmpty => MessageList == null || MessageList.Count < 0;

        public override string ToString()
        {
            return $"FrameID:{FrameID} MessageCount:{MessageList.Count}";
        }
    }
}
