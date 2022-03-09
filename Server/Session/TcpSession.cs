using Net.General.Data;
using Net.General.Extenstions;
using Net.General.Log;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Net.Server.Session
{
    public class TcpSession : Session
    {
        private MemoryStream m_Stream;
        private BinaryReader m_Reader;
        private BinaryWriter m_Writer;

        private ArraySegment<byte> m_ReceiveBuffer;

        protected ConcurrentQueue<NetMessage> m_ReceiveQueue;

        public TcpSession() : base()
        {
            m_Stream = new MemoryStream(1024);
            m_Reader = new BinaryReader(m_Stream);
            m_Writer = new BinaryWriter(m_Stream);

            m_ReceiveBuffer = new ArraySegment<byte>(new byte[1024]);
            m_ReceiveQueue = new ConcurrentQueue<NetMessage>();
        }

        public override void Active(Socket socket, IPEndPoint remoteEndPoint)
        {
            base.Active(socket, remoteEndPoint);
            ReceiveAsync();
        }

        public override bool Send(Memory<byte> message)
        {
            if (!IsActived) return false;

            try
            {
                m_Socket.Send(message.ToArray());
            }
            catch (Exception error)
            {
                DeLog.LogError(error);
                Close();
                return false;
            }
            return true;
        }

        public override Task<bool> SendAsync(Memory<byte> message)
        {
            return Task.Run(async () =>
            {
                if (!IsActived) return false;

                try
                {
                    await m_Socket.SendAsync(new ArraySegment<byte>(message.ToArray()), SocketFlags.None);
                }
                catch (Exception error)
                {
                    DeLog.LogError(error);
                    Close();
                    return false;
                }

                return true;
            });
        }

        protected Task ReceiveAsync()
        {
            return Task.Run(async () =>
            {
                while (IsActived)
                {
                    var receiveLength = 0;

                    try
                    {
                        receiveLength = await m_Socket.ReceiveAsync(m_ReceiveBuffer, SocketFlags.None);
                    }
                    catch (Exception error)
                    {
                        DeLog.LogError(error);
                        Close();
                    }

                    if (receiveLength <= 0) continue;

                    var receiveBuffer = m_ReceiveBuffer.AsMemory(0, receiveLength);

                    m_Stream.Position = m_Stream.Length;

                    m_Writer.Write(receiveBuffer.Span);

                    var message = NetMessage.Deserialize(m_Reader);
                    if (message == null) continue;

                    m_ReceiveQueue.Enqueue(message);

                    var remainingLength = m_Stream.Length - m_Stream.Position;

                    var remainingBuffer = new ReadOnlyMemory<byte>(m_Stream.GetBuffer(), (int)m_Stream.Position, (int)remainingLength);

                    m_Stream.SetLength(0);

                    m_Writer.Write(remainingBuffer.Span);
                }
            });
        }

        public override void Tick()
        {
            if (!IsActived) return;

            while (m_ReceiveQueue.TryDequeue(out var message))
            {
                m_Listener?.OnReceive(this, message);
            }
        }

        public override void Close()
        {
            m_Stream.SetLength(0);
            base.Close();
        }
    }
}
