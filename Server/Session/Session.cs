using Net.General.Log;
using Net.Server.Listener;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Net.Server.Session
{
    public abstract class Session : ISession
    {
        protected uint m_ID;
        public uint ID => m_ID;

        protected Socket m_Socket;
        protected IPEndPoint m_RemoteEndPoint;

        protected ISessionListener m_Listener;
        public ISessionListener Listener
        {
            get => m_Listener;
            set => m_Listener = value;
        }

        protected bool m_IsActived;
        public bool IsActived => m_IsActived;

        public virtual void Init(uint sessionID, ISessionListener listener)
        {
            m_ID = sessionID;
            m_Listener = listener;
        }

        public virtual void Active(Socket socket, IPEndPoint remoteEndPoint)
        {
            m_Socket = socket;
            m_RemoteEndPoint = remoteEndPoint;
            m_IsActived = true;
            m_Listener?.OnConnected(this);
        }

        public abstract bool Send(Memory<byte> message);
        public abstract Task<bool> SendAsync(Memory<byte> message);
        public abstract void Tick();

        public virtual void Close()
        {
            m_IsActived = false;
            if (m_Socket == null) return;

            try
            {
                m_Socket.Shutdown(SocketShutdown.Both);
                m_Socket.Close();
                m_Socket = null;
            }
            catch (Exception error)
            {
                DeLog.LogError(error);
            }
            finally
            {
                m_Socket.Close();
            }

            m_Listener?.OnDisconnected(this);
        }

        public override string ToString()
        {
            return $"SessionID:{m_ID} Host:{m_RemoteEndPoint.Address} Port:{m_RemoteEndPoint.Port}";
        }
    }
}
