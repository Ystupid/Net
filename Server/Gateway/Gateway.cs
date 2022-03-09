using Net.General.Config;
using Net.General.Log;
using Net.General.Utils;
using Net.Server.Listener;
using Net.Server.Session;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Net.Server.Gateway
{
    public abstract class Gateway<T> : IGateway where T : class, ISession, new()
    {
        protected Socket m_Socket;

        protected long m_SessionID;
        public uint SessionID => (uint)Interlocked.Increment(ref m_SessionID);

        protected ServerConfig m_Config;
        public ServerConfig Config => m_Config;

        protected bool m_IsRunning;
        public bool IsRunning => m_IsRunning;

        protected uint m_SessionTickInterval = 3;
        protected uint m_LastSessionTickTime = 0;

        protected ISessionListener m_Listener;

        protected Queue<T> m_DisableQueue;
        protected Dictionary<long, T> m_SessionMap;

        public virtual T this[long sessionID]
        {
            get
            {
                lock (m_SessionMap)
                {
                    return m_SessionMap.ContainsKey(sessionID) ? m_SessionMap[sessionID] : null;
                }
            }
        }

        public Gateway(ISessionListener listener, ServerConfig config)
        {
            m_Config = config;
            m_Listener = listener;
            m_DisableQueue = new Queue<T>();
            m_SessionMap = new Dictionary<long, T>();
        }

        public virtual T CreateSession(ISessionListener listener)
        {
            var session = SessionFactory.Genereate<T>();
            session.Init(SessionID, listener);
            lock (m_SessionMap) m_SessionMap.Add(session.ID, session);
            return session;
        }

        public virtual void Close()
        {
            m_IsRunning = false;

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
        }

        public abstract void Start();
        protected abstract Task AcceptAsync();

        public virtual void ReStart()
        {
            Close();
            Start();
        }

        public virtual void Tick()
        {
            if (!m_IsRunning) return;

            uint current = TimeUtils.ConvertTimeToUInt(DateTime.Now.ToLocalTime());

            if (current - m_LastSessionTickTime > m_SessionTickInterval * 1000)
            {
                m_LastSessionTickTime = current;
                ClearDisableSession();
                GC.Collect();
            }

            lock (m_SessionMap)
            {
                foreach (var session in m_SessionMap.Values)
                {
                    session.Tick();

                    if (current == m_LastSessionTickTime && !session.IsActived)
                        m_DisableQueue.Enqueue(session);
                }
            }
        }

        protected virtual void ClearDisableSession()
        {
            while (m_DisableQueue.Count > 0)
            {
                var session = m_DisableQueue.Dequeue();

                m_SessionMap.Remove(session.ID);

                session.Close();

                SessionFactory.Release(session);
            }
        }
    }
}
