using Net.General.Config;
using Net.General.Extenstions;
using Net.General.Log;
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
    public class KcpGateway : Gateway<KcpSession>
    {
        private UdpListener m_SocketListener;

        private ArraySegment<byte> m_ReceiveBuffer;
        private MemoryStream m_Stream;
        private BinaryReader m_Reader;
        private BinaryWriter m_Writer;

        private IPEndPoint m_RemoteEndPoint;
        private IPEndPoint m_LocalEndPoint;
        private IPEndPoint m_ListenerEndPoint;

        public KcpGateway(ISessionListener listener, ServerConfig config = default) : base(listener, config)
        {
            m_Stream = new MemoryStream(1024);
            m_Reader = new BinaryReader(m_Stream);
            m_Writer = new BinaryWriter(m_Stream);
            m_ReceiveBuffer = new ArraySegment<byte>(new byte[1024]);
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

                    m_Stream.Position = m_Stream.Length;
                    m_Writer.Write(m_ReceiveBuffer.AsSpan(0, result.ReceivedBytes));

                    m_Stream.Position = 0;
                    var sessionID = m_Reader.ReadUInt32();

                    var session = this[sessionID];
                    if (session == null)
                    {
                        DeLog.LogWarning($"Session:{sessionID}不存在");
                        m_Stream.SetLength(0);
                        continue;
                    }

                    session.ReceiveFormGateway(new Span<byte>(m_Stream.GetBuffer(), 0, (int)m_Stream.Length));

                    m_Stream.SetLength(0);
                }
            });
        }

        protected override void ClearDisableSession() { }
    }
}
