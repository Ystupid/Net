using Net.Client.Listener;
using Net.General.Config;
using Net.General.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Net.Client.Session
{
    public abstract class Session : ISession
    {
        private long m_ID;
        public uint ID
        {
            get => (uint)m_ID;
            protected set => Interlocked.Exchange(ref m_ID, value);
        }

        protected bool m_IsConnected;
        public bool IsConnected => m_IsConnected;

        protected ClientConfig m_Config;
        public ClientConfig Config => m_Config;

        protected ISessionListener m_Listener;

        protected IPEndPoint m_LocalEndPoint;
        public IPEndPoint LocalEndPoint => m_LocalEndPoint;

        protected IPEndPoint m_RemoteEndPoint;
        public IPEndPoint RemoteEndPoint => m_RemoteEndPoint;

        protected Socket m_Socket;

        public Session(ISessionListener listener, ClientConfig config)
        {
            m_Listener = listener;
            m_Config = config;
        }

        public abstract bool Connect(string host, int port);
        public abstract Task<bool> ConnectAsync(string host, int port);

        public abstract bool Send(Memory<byte> message);
        public abstract Task<bool> SendAsync(Memory<byte> message);
        public abstract void Tick();

        public virtual void Close()
        {
            m_IsConnected = false;
            if (m_Socket == null) return;

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

            m_Listener?.OnDisconnected(this);
        }

        public override string ToString()
        {
            return $"SessionID:{ID} Host:{m_RemoteEndPoint.Address} Port:{m_RemoteEndPoint.Port}";
        }
    }
}
