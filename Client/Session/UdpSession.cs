using Net.Client.Listener;
using Net.General.Config;
using Net.General.Data;
using Net.General.Extenstions;
using Net.General.Log;
using Net.General.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static Net.General.Data.NetProtocol;

namespace Net.Client.Session
{
    public class UdpSession : Session
    {
        protected ArraySegment<byte> m_ConnectSendBuffer;
        protected ArraySegment<byte> m_ConnectReceiveBuffer;

        protected ArraySegment<byte> m_ReceiveBuffer;

        protected MemoryStream m_Stream;
        protected BinaryReader m_Reader;
        protected BinaryWriter m_Writer;

        protected ConcurrentQueue<byte[]> m_SendQueue;
        protected ConcurrentQueue<NetMessage> m_ReceiveQueue;

        public UdpSession(ISessionListener listener, ClientConfig config) : base(listener, config)
        {
            m_Stream = new MemoryStream(config.ReceiveBufferSize);
            m_Reader = new BinaryReader(m_Stream);
            m_Writer = new BinaryWriter(m_Stream);
            m_SendQueue = new ConcurrentQueue<byte[]>();
            m_ReceiveQueue = new ConcurrentQueue<NetMessage>();

            m_ConnectSendBuffer = new ArraySegment<byte>(NetMessage.Default_Connect);
            m_ConnectReceiveBuffer = new ArraySegment<byte>(new byte[64]);

            m_ReceiveBuffer = new ArraySegment<byte>(new byte[config.ReceiveBufferSize]);
        }

        public override bool Connect(string host, int port)
        {
            var result = IPAddress.TryParse(host, out var address);
            if (!result) return false;

            m_RemoteEndPoint = new IPEndPoint(address, port);
            m_LocalEndPoint = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                m_Socket.Bind(m_LocalEndPoint);
                m_Socket.SendTo(m_ConnectSendBuffer.Array, SocketFlags.None, m_RemoteEndPoint);

                EndPoint remoteEndPoint = new IPEndPoint(address, port);
                var receiveLength = m_Socket.ReceiveFrom(m_ConnectReceiveBuffer.Array, SocketFlags.None, ref remoteEndPoint);

                if (receiveLength <= 0) return m_IsConnected = false;

                m_Writer.Write(m_ConnectReceiveBuffer.AsSpan(0, receiveLength));

                var netMessage = NetMessage.Deserialize(m_Reader);

                if (netMessage == null) return m_IsConnected = false;

                ID = netMessage.ProtocolHead.SessionID;
                var connectData = ProtoHelper.Deserialize<S2C_Connect>(netMessage.Message);
                m_RemoteEndPoint.Port = connectData.Port;

                netMessage.Dispose();
                m_Stream.SetLength(0);

            }
            catch (Exception error)
            {
                DeLog.LogError(error);
                return m_IsConnected = false;
            }

            OnConnected();

            return m_IsConnected;
        }

        public override Task<bool> ConnectAsync(string host, int port)
        {
            return Task.Run(async () =>
            {
                var result = IPAddress.TryParse(host, out var address);
                if (!result) return false;

                m_RemoteEndPoint = new IPEndPoint(address, port);
                m_LocalEndPoint = new IPEndPoint(IPAddress.Any, 0);

                try
                {
                    m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    m_Socket.Bind(m_LocalEndPoint);

                    await m_Socket.SendToAsync(m_ConnectSendBuffer, SocketFlags.None, m_RemoteEndPoint);

                    var receiveResult = await m_Socket.ReceiveFromAsync(m_ConnectReceiveBuffer, SocketFlags.None, m_RemoteEndPoint);

                    if (receiveResult.ReceivedBytes <= 0) return m_IsConnected = false;

                    m_Writer.Write(m_ConnectReceiveBuffer.AsSpan(0, receiveResult.ReceivedBytes));

                    var netMessage = NetMessage.Deserialize(m_Reader);

                    if (netMessage == null) return m_IsConnected = false;

                    ID = netMessage.ProtocolHead.SessionID;
                    var connectData = ProtoHelper.Deserialize<S2C_Connect>(netMessage.Message);
                    m_RemoteEndPoint.Port = connectData.Port;

                    netMessage.Dispose();
                    m_Stream.SetLength(0);
                }
                catch (Exception error)
                {
                    DeLog.LogError(error);
                    return m_IsConnected = false;
                }

                OnConnected();

                return m_IsConnected;
            });
        }

        protected virtual void OnConnected()
        {
            m_IsConnected = true;
            ReceiveAsync();
            m_Listener.OnConnected(this);
        }

        private NetMessage StickPacket(int receivedBytes)
        {
            m_Stream.Position = m_Stream.Length;

            m_Writer.Write(m_ReceiveBuffer.AsSpan(0, receivedBytes));

            var message = NetMessage.Deserialize(m_Reader);
            if (message == null) return null;

            var remainingLength = m_Stream.Length - m_Stream.Position;

            var remainingBuffer = new ReadOnlyMemory<byte>(m_Stream.GetBuffer(), (int)m_Stream.Position, (int)remainingLength);

            m_Stream.SetLength(0);

            m_Writer.Write(remainingBuffer.Span);

            return message;
        }

        protected virtual Task ReceiveAsync()
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

                    var message = StickPacket(result.ReceivedBytes);

                    if (message == null) continue;

                    m_ReceiveQueue.Enqueue(message);
                }
            });
        }

        private void SliceMessage(Memory<byte> buffer, int sliceLength = 1024)
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
            if (!IsConnected) return false;

            SliceMessage(message, Config.SendBufferSize);

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
                if (!IsConnected) return false;

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
