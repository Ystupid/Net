using Net.General.Data;
using Net.General.Log;
using Net.Server.Listener;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.Sockets.Kcp;
using System.Text;
using System.Threading.Tasks;

namespace Net.Server.Session
{
    public class KcpSession : Session, IKcpCallback
    {
        private PoolSegManager.Kcp m_Kcp;
        private ConcurrentQueue<byte[]> m_ReceiveQueue;
        private DateTime m_NextUpdateTime;

        protected MemoryStream m_Stream;
        protected BinaryReader m_Reader;

        public KcpSession()
        {
            m_Stream = new MemoryStream();
            m_Reader = new BinaryReader(m_Stream);
            m_ReceiveQueue = new ConcurrentQueue<byte[]>();
        }

        public override void Init(uint sessionID, ISessionListener listener)
        {
            base.Init(sessionID, listener);
            m_Kcp = new PoolSegManager.Kcp(ID, this);
            m_Kcp.NoDelay(1, 10, 2, 1);
            m_Kcp.WndSize(64, 64);
            m_Kcp.SetMtu(512);
            m_NextUpdateTime = m_Kcp.Check(DateTime.UtcNow);
        }

        public override void Active(Socket socket, IPEndPoint remoteEndPoint)
        {
            base.Active(socket, remoteEndPoint);
        }

        public void Output(IMemoryOwner<byte> buffer, int avalidLength)
        {
            if (!IsActived) return;

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

        public void ReceiveFormGateway(Span<byte> span)
        {
            m_ReceiveQueue.Enqueue(span.ToArray());
        }

        public override bool Send(Memory<byte> message)
        {
            if (!IsActived) return false;

            var result = m_Kcp.Send(message.Span);

            DeLog.Log("KcpSend:" + result);

            return result == 0;
        }

        public override Task<bool> SendAsync(Memory<byte> message) => Task.Run(() => Send(message));

        public override void Tick()
        {
            if (!IsActived) return;

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
                m_Stream.SetLength(size);

                var length = m_Kcp.Recv(new Span<byte>(m_Stream.GetBuffer(), 0, size));

                var message = NetMessage.Deserialize(m_Reader);

                if (message == null) continue;

                m_Listener?.OnReceive(this, message);

                DeLog.Log("KcpRecv:" + length);
            }
        }

        public override void Close()
        {
            m_IsActived = false;

            m_Stream.SetLength(0);

            if (m_Socket == null) return;
            m_Socket = null;

            m_Listener?.OnDisconnected(this);
        }
    }
}
