using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.General.Data
{
    public class NetMessage : IDisposable
    {
        public static readonly byte[] Default_Connect = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 1 };

        private MemoryStream m_Stream;
        private BinaryWriter m_Writer;

        public ProtocolHead ProtocolHead { get; set; }
        public byte[] Message { get; set; }

        public uint Length => ProtocolHead.DataSize + ProtocolHead.Length;

        public NetMessage()
        {
            m_Stream = new MemoryStream();
            m_Writer = new BinaryWriter(m_Stream);
        }

        public NetMessage(ProtocolHead protocolHead, byte[] message) : this()
        {
            ProtocolHead = protocolHead;
            Message = message;
        }

        /// <summary>
        /// 序列化
        /// </summary>
        public Memory<byte> Serialize()
        {
            ProtocolHead.Serialize(m_Writer);
            if (Message != null)
                m_Writer.Write(Message);
            return new Memory<byte>(m_Stream.GetBuffer(), 0, (int)m_Stream.Length);
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static NetMessage Deserialize(in BinaryReader reader)
        {
            reader.BaseStream.Position = 0;

            var protocolHead = ProtocolHead.Deserialize(reader);

            if (protocolHead == null) return null;

            if (reader.BaseStream.Length < protocolHead.Value.DataSize) return null;

            var message = new NetMessage();

            message.ProtocolHead = protocolHead.Value;

            var dataSize = (int)protocolHead.Value.DataSize;
            message.Message = dataSize <= 0 ? null : reader.ReadBytes(dataSize);

            return message;
        }

        public void Dispose()
        {
            m_Writer.Dispose();
        }
    }
}
