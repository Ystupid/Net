using Net.Client.Listener;
using Net.General.Config;
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

namespace Net.Client.Session
{
    public class TcpSession : Session
    {
        private MemoryStream m_Stream;
        private BinaryReader m_Reader;
        private BinaryWriter m_Writer;

        private ArraySegment<byte> m_ReceiveBuffer;

        protected ConcurrentQueue<NetMessage> m_ReceiveQueue;

        public TcpSession(ISessionListener listener, ClientConfig config) : base(listener, config)
        {
            m_Stream = new MemoryStream(config.ReceiveBufferSize);
            m_Reader = new BinaryReader(m_Stream);
            m_Writer = new BinaryWriter(m_Stream);

            m_ReceiveBuffer = new ArraySegment<byte>(new byte[config.ReceiveBufferSize]);
            m_ReceiveQueue = new ConcurrentQueue<NetMessage>();
        }

        public override bool Connect(string host, int port)
        {
            var result = IPAddress.TryParse(host, out var address);
            if (!result) return false;

            m_RemoteEndPoint = new IPEndPoint(address, port);

            try
            {
                m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                m_Socket.Connect(m_RemoteEndPoint);
            }
            catch (Exception error)
            {
                DeLog.LogError(error);
                return m_IsConnected = false;
            }

            m_IsConnected = true;

            ReceiveAsync();

            m_Listener?.OnConnected(this);

            return m_IsConnected;
        }

        public override Task<bool> ConnectAsync(string host, int port)
        {
            return Task.Run(async () =>
            {
                var result = IPAddress.TryParse(host, out var address);
                if (!result) return false;

                m_RemoteEndPoint = new IPEndPoint(address, port);

                try
                {
                    m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    await m_Socket.ConnectAsync(m_RemoteEndPoint);
                }
                catch (SocketException socketError)
                {
                    DeLog.LogError(socketError);
                    return m_IsConnected = false;
                }

                m_IsConnected = true;

                _ = ReceiveAsync();

                m_Listener?.OnConnected(this);

                return m_IsConnected;
            });
        }

        public override bool Send(Memory<byte> message)
        {
            if (!IsConnected) return false;

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
                if (!IsConnected) return false;

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
                while (IsConnected)
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

                    lock (m_ReceiveQueue) m_ReceiveQueue.Enqueue(message);

                    var remainingLength = m_Stream.Length - m_Stream.Position;

                    var remainingBuffer = new ReadOnlyMemory<byte>(m_Stream.GetBuffer(), (int)m_Stream.Position, (int)remainingLength);

                    m_Stream.SetLength(0);

                    m_Writer.Write(remainingBuffer.Span);
                }
            });
        }

        public override void Tick()
        {
            if (!IsConnected) return;

            while (m_ReceiveQueue.TryDequeue(out var message))
            {
                m_Listener?.OnReceive(this, message);
            }
        }

        public override void Close()
        {
            m_Stream.Dispose();
            base.Close();
        }
    }
}
