using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Server.Session
{
    public static class SessionFactory
    {
        private readonly static Dictionary<Type, Stack<ISession>> m_SessionPool = new Dictionary<Type, Stack<ISession>>();

        public static T Genereate<T>() where T : ISession, new()
        {
            lock (m_SessionPool)
            {
                var type = typeof(T);

                if (!m_SessionPool.ContainsKey(type))
                    m_SessionPool.Add(type, new Stack<ISession>());

                var stack = m_SessionPool[type];

                if (stack.Count <= 0)
                    return new T();

                return (T)stack.Pop();
            }
        }

        public static void Release<T>(in T session) where T : ISession
        {
            lock (m_SessionPool)
            {
                var type = typeof(T);

                if (!m_SessionPool.ContainsKey(type))
                    m_SessionPool.Add(type, new Stack<ISession>());

                var stack = m_SessionPool[type];

                stack.Push(session);
            }
        }
    }
}
