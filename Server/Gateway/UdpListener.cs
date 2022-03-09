using Net.General.Data;
using Net.General.Extenstions;
using Net.General.Log;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Net.General.Utils;
using static Net.General.Data.NetProtocol;

namespace Net.Server.Gateway
{
    public class UdpListener
    {
        protected Socket m_Socket;

        protected IPEndPoint m_ListenerEndPoint;
        public IPEndPoint ListenerEndPoint => m_ListenerEndPoint;

        protected IPEndPoint m_LocalEndPoint;
        public IPEndPoint LocalEndPoint => m_LocalEndPoint;

        protected IPEndPoint m_RemoteEndPoint;
        public IPEndPoint RemoteEndPoint => m_RemoteEndPoint;

        protected MemoryStream m_Stream;
        protected BinaryReader m_Reader;
        protected BinaryWriter m_Writer;

        private ArraySegment<byte> m_ReceiveBuffer;
        private ArraySegment<byte> m_SendBuffer;

        public UdpListener(int bufferSize = 64)
        {
            m_Stream = new MemoryStream(bufferSize);
            m_Reader = new BinaryReader(m_Stream);
            m_Writer = new BinaryWriter(m_Stream);
            m_ReceiveBuffer = new ArraySegment<byte>(new byte[bufferSize]);
            m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        public virtual void Bind(EndPoint listenerEndPoint, EndPoint localEndPoint)
        {
            m_ListenerEndPoint = listenerEndPoint as IPEndPoint;
            m_LocalEndPoint = localEndPoint as IPEndPoint;
            m_RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            m_Socket.Bind(m_ListenerEndPoint);

            InitSendBuffer();
        }

        protected virtual void InitSendBuffer()
        {
            var connectData = new S2C_Connect
            {
                RemoteEndPoint = LocalEndPoint
            };

            var buffer = ProtoHelper.Serialize(connectData);

            var protocolHead = new ProtocolHead
            {
                SessionID = 0,
                CommandID = NetCommand.Connect,
                DataSize = (uint)buffer.Length
            };

            using (var netMessage = new NetMessage(protocolHead, buffer))
            {
                m_SendBuffer = new ArraySegment<byte>(netMessage.Serialize().ToArray());
            }
        }

        public virtual Task<IPEndPoint> AcceptAsync()
        {
            return Task.Run(async () =>
            {
                var result = default(SocketReceiveFromResult);
                var remoteEndPoint = default(IPEndPoint);

                try
                {
                    result = await m_Socket.ReceiveFromAsync(m_ReceiveBuffer, SocketFlags.None, m_RemoteEndPoint);
                }
                catch (Exception error)
                {
                    DeLog.LogError(error);
                }

                if (result.ReceivedBytes <= 0) return remoteEndPoint;

                m_Stream.SetLength(0);

                m_Writer.Write(m_ReceiveBuffer.AsSpan(0, result.ReceivedBytes));

                var message = NetMessage.Deserialize(m_Reader);

                if (message == null) return remoteEndPoint;

                if (message.ProtocolHead.CommandID != NetCommand.Connect) return remoteEndPoint;

                remoteEndPoint = result.RemoteEndPoint as IPEndPoint;

                return remoteEndPoint;
            });
        }

        protected virtual void ResetSession(uint sessionID)
        {
            var array = m_SendBuffer.Array;
            array[0] = (byte)sessionID;
            array[1] = (byte)(sessionID >> 8);
            array[2] = (byte)(sessionID >> 16);
            array[3] = (byte)(sessionID >> 24);
        }

        public virtual Task AcceptComplete(uint sessionID, IPEndPoint remoteEndPoint)
        {
            return Task.Run(async () =>
            {
                ResetSession(sessionID);
                await m_Socket.SendToAsync(m_SendBuffer, SocketFlags.None, remoteEndPoint);
            });
        }

        public virtual void Close()
        {
            m_Writer.Dispose();

            try
            {
                m_Socket.Shutdown(SocketShutdown.Both);
            }
            catch (Exception error)
            {
                DeLog.LogError(error);
            }
            finally
            {
                m_Socket.Close();
                m_Socket = null;
            }
        }
    }
}
