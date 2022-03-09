using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.General.Data
{
    public struct ProtocolHead
    {
        public const uint Length = 9;

        public uint SessionID { get; set; }
        public uint DataSize { get; set; }
        public byte CommandID { get; set; }

        public void Serialize(in BinaryWriter writer)
        {
            writer.Write(SessionID);
            writer.Write(DataSize);
            writer.Write(CommandID);
        }

        public static void Serialize(ProtocolHead protocolHead, in BinaryWriter writer)
        {
            writer.Write(protocolHead.DataSize);
        }

        public static ProtocolHead? Deserialize(in BinaryReader reader)
        {
            if (reader.BaseStream.Length < Length) return null;

            var protocolHead = new ProtocolHead();
            protocolHead.SessionID = reader.ReadUInt32();
            protocolHead.DataSize = reader.ReadUInt32();
            protocolHead.CommandID = reader.ReadByte();
            return protocolHead;
        }

        public override string ToString()
        {
            return $"SessionID:{SessionID} CommandID:{CommandID} DataSize:{DataSize}";
        }
    }
}
