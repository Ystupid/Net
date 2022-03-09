using Net.General.Config;
using Net.General.Data;
using Net.General.Extenstions;
using Net.General.Log;
using Net.General.Utils;
using Net.Server.Listener;
using Net.Server.Session;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Net.Server.Gateway
{
    public class UdpGateway : Gateway<UdpSession>
    {
        private UdpListener m_SocketListener;

        private ArraySegment<byte> m_ReceiveBuffer;
        private MemoryStream m_Stream;
        private BinaryReader m_Reader;
        private BinaryWriter m_Writer;

        private new ServerConfig m_Config;

        private IPEndPoint m_RemoteEndPoint;
        private IPEndPoint m_LocalEndPoint;
        private IPEndPoint m_ListenerEndPoint;

        public UdpGateway(ISessionListener listener, ServerConfig config) : base(listener, config)
        {
            m_Config = config;
            m_Stream = new MemoryStream(m_Config.ReceiveBufferSize);
            m_Reader = new BinaryReader(m_Stream);
            m_Writer = new BinaryWriter(m_Stream);
            m_ReceiveBuffer = new ArraySegment<byte>(new byte[m_Config.ReceiveBufferSize]);
        }

        public override void Start()
        {
            if (m_Socket != null) Close();

            m_RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            m_LocalEndPoint = new IPEndPoint(IPAddress.Any, m_Config.Port - 1);
            m_ListenerEndPoint = new IPEndPoint(IPAddress.Any, m_Config.Port);

            try
            {
                m_SocketListener = new UdpListener();
                m_SocketListener.Bind(m_ListenerEndPoint, m_LocalEndPoint);
                m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                m_Socket.Bind(m_LocalEndPoint);
            }
            catch (Exception error)
            {
                DeLog.LogError(error);
                return;
            }

            m_IsRunning = true;

            AcceptAsync();
            ReceiveAsync();
        }

        protected override Task AcceptAsync()
        {
            return Task.Run(async () =>
            {
                while (IsRunning && m_SocketListener != null)
                {
                    var remoteEndPoint = await m_SocketListener.AcceptAsync();

                    if (remoteEndPoint == null) continue;

                    var session = CreateSession(m_Listener);
                    session.Active(m_Socket, remoteEndPoint);

                    await m_SocketListener.AcceptComplete(session.ID, remoteEndPoint);
                }
            });
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

        private Task ReceiveAsync()
        {
            return Task.Run(async () =>
            {
                while (IsRunning && m_Socket != null)
                {
                    var result = default(SocketReceiveFromResult);

                    try
                    {
                        result = await m_Socket.ReceiveFromAsync(m_ReceiveBuffer, SocketFlags.None, m_RemoteEndPoint);
                    }
                    catch (Exception error)
                    {
                        DeLog.LogError(error);
                        await Task.Delay(10);
                    }

                    if (result.ReceivedBytes <= 0) continue;

                    var message = StickPacket(result.ReceivedBytes);

                    if (message == null) continue;

                    var session = this[message.ProtocolHead.SessionID];
                    if (session == null)
                    {
                        DeLog.LogWarning($"Session is Null! {message.ProtocolHead}");
                        continue;
                    }

                    session.ReceiveFormGateway(message);
                }
            });
        }

        public override void Close()
        {
            m_Stream.SetLength(0);
            base.Close();
        }
    }
}
