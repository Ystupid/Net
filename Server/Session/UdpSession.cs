using Net.General.Data;
using Net.General.Log;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Net.Server.Session
{
    public class UdpSession : Session
    {
        protected ConcurrentQueue<byte[]> m_SendQueue;
        protected ConcurrentQueue<NetMessage> m_ReceiveQueue;

        public UdpSession()
        {
            m_SendQueue = new ConcurrentQueue<byte[]>();
            m_ReceiveQueue = new ConcurrentQueue<NetMessage>();
        }

        public virtual void ReceiveFormGateway(NetMessage message)
        {
            m_ReceiveQueue.Enqueue(message);
        }

        protected virtual void SliceMessage(Memory<byte> buffer, int sliceLength = 1024)
        {
            var remainingLength = buffer.Length;
            var position = 0;

            while (remainingLength > sliceLength)
            {
                remainingLength -= sliceLength;

                var splitBuffer = buffer.Slice(position, position += sliceLength);

                m_SendQueue.Enqueue(splitBuffer.ToArray());
            }

            if (remainingLength > 0)
            {
                var remainingBuffer = buffer.Slice(position, remainingLength);

                m_SendQueue.Enqueue(remainingBuffer.ToArray());
            }
        }

        public override bool Send(Memory<byte> message)
        {
            if (!IsActived) return false;

            SliceMessage(message);

            while (m_SendQueue.TryDequeue(out var buffer))
            {
                try
                {
                    m_Socket.SendTo(buffer, m_RemoteEndPoint);
                }
                catch (Exception error)
                {
                    DeLog.LogError(error);
                    Close();
                    return false;
                }
            }

            return true;
        }

        public override Task<bool> SendAsync(Memory<byte> message)
        {
            return Task.Run(async () =>
            {
                if (!IsActived) return false;

                SliceMessage(message);

                while (m_SendQueue.TryDequeue(out var buffer))
                {
                    try
                    {
                        await m_Socket.SendToAsync(new ArraySegment<byte>(buffer), SocketFlags.None, m_RemoteEndPoint);
                    }
                    catch (Exception error)
                    {
                        DeLog.LogError(error);
                        Close();
                        return false;
                    }
                }

                return true;
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
            m_IsActived = false;

            if (m_Socket == null) return;
            m_Socket = null;

            m_Listener?.OnDisconnected(this);
        }
    }
}
