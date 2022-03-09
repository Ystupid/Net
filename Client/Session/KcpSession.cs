using Net.Client.Listener;
using Net.General.Config;
using Net.General.Data;
using Net.General.Extenstions;
using Net.General.Log;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net.Sockets.Kcp;
using System.Text;
using System.Threading.Tasks;

namespace Net.Client.Session
{
    public class KcpSession : UdpSession, IKcpCallback
    {
        private PoolSegManager.Kcp m_Kcp;
        private DateTime m_NextUpdateTime;

        protected MemoryStream m_ReceiveStream;
        protected BinaryReader m_ReceiveReader;
        protected BinaryWriter m_ReceiveWriter;

        private new ConcurrentQueue<byte[]> m_ReceiveQueue;

        public KcpSession(ISessionListener listener, ClientConfig config) : base(listener, config)
        {
            m_ReceiveStream = new MemoryStream(config.ReceiveBufferSize);
            m_ReceiveReader = new BinaryReader(m_ReceiveStream);
            m_ReceiveWriter = new BinaryWriter(m_ReceiveStream);
            m_ReceiveQueue = new ConcurrentQueue<byte[]>();
        }

        public KcpSession(uint sessionID,ISessionListener listener, ClientConfig config) : this(listener, config)
        {
            ID = sessionID;
            ResetKCP(sessionID);
        }

        private void ResetKCP(uint sessionID)
        {
            m_Kcp = new PoolSegManager.Kcp(sessionID, this);
            m_Kcp.NoDelay(1, 10, 2, 1);
            m_Kcp.WndSize(64, 64);
            m_Kcp.SetMtu(512);
        }

        protected override void OnConnected()
        {
            ResetKCP(ID);
            base.OnConnected();
        }

        public override bool Send(Memory<byte> message)
        {
            if (!IsConnected) return false;

            var result = m_Kcp.Send(message.Span);

            return result == 0;
        }

        public override Task<bool> SendAsync(Memory<byte> message) => Task.Run(() => Send(message));

        public void Output(IMemoryOwner<byte> buffer, int avalidLength)
        {
            if (!IsConnected) return;

            try
            {
                m_Socket.SendTo(buffer.Memory.Slice(0, avalidLength).ToArray(), SocketFlags.None, m_RemoteEndPoint);
            }
            catch (Exception error)
            {
                DeLog.LogError(error);
                Close();
            }
        }

        protected override Task ReceiveAsync()
        {
            return Task.Run(async () =>
            {
                while (IsConnected)
                {
                    var result = default(SocketReceiveFromResult);

                    try
                    {
                        result = await m_Socket.ReceiveFromAsync(m_ReceiveBuffer, SocketFlags.None, m_RemoteEndPoint);
                    }
                    catch (Exception error)
                    {
                        DeLog.LogError(error);
                        Close();
                    }

                    if (result.ReceivedBytes <= 0) continue;

                    if (!m_RemoteEndPoint.Equals(result.RemoteEndPoint))
                    {
                        DeLog.LogWarning($"ReceiveError:{result.RemoteEndPoint}");
                        continue;
                    }

                    m_Stream.Position = m_Stream.Length;
                    m_Writer.Write(m_ReceiveBuffer.AsSpan(0, result.ReceivedBytes));

                    m_Stream.Position = 0;
                    var sessionID = m_Reader.ReadUInt32();

                    if (sessionID == 0)
                    {
                        DeLog.LogWarning($"SessionError:{sessionID}");
                    }

                    m_ReceiveQueue.Enqueue(new Span<byte>(m_Stream.GetBuffer(), 0, (int)m_Stream.Length).ToArray());

                    m_Stream.SetLength(0);
                }
            });
        }

        public override void Tick()
        {
            if (!IsConnected) return;

            var currentTime = DateTime.UtcNow;

            if (currentTime >= m_NextUpdateTime)
            {
                m_Kcp.Update(currentTime);
                m_NextUpdateTime = m_Kcp.Check(currentTime);
            }

            while (m_ReceiveQueue.TryDequeue(out var buffer))
            {
                var result = m_Kcp.Input(buffer);
                //DeLog.Log("KcpInput:" + result);
            }

            for (int size = m_Kcp.PeekSize(); size > 0; size = m_Kcp.PeekSize())
            {
                m_ReceiveStream.SetLength(size);

                var length = m_Kcp.Recv(new Span<byte>(m_ReceiveStream.GetBuffer(), 0, size));

                var message = NetMessage.Deserialize(m_ReceiveReader);

                if (message == null) continue;

                if (message.ProtocolHead.SessionID != ID)
                    ID = message.ProtocolHead.SessionID;

                m_Listener?.OnReceive(this, message);

                //DeLog.Log("KcpRecv:" + length);
            }
        }

        public override void Close()
        {
            m_ReceiveStream.Dispose();
            base.Close();
        }
    }
}
