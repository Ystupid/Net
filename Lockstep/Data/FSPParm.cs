using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Lockstep.Data
{
    [ProtoContract]
    public class FSPParm
    {
        [ProtoMember(1)]
        public string host;
        [ProtoMember(2)]
        public int port;
        [ProtoMember(3)]
        public uint sid;
        [ProtoMember(4)]
        public int serverFrameInterval = 66;
        [ProtoMember(5)]
        public int serverTimeout = 15000;//ms
        [ProtoMember(6)]
        public int clientFrameRateMultiple = 2;
        [ProtoMember(7)]
        public int authId = 0;
        [ProtoMember(8)]
        public bool useLocal = false;
        [ProtoMember(9)]
        public int maxFrameId = 1800;

        [ProtoMember(10)]
        public bool enableSpeedUp = true;
        [ProtoMember(11)]
        public int defaultSpeed = 1;
        [ProtoMember(12)]
        public int jitterBufferSize = 0;//缓冲大小
        [ProtoMember(13)]
        public bool enableAutoBuffer = true;


    }
}
