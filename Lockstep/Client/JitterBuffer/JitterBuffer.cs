using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Net.Lockstep.Client.JitterBuffer
{
    public class JitterBuffer<T>
    {
        private Dictionary<int, T> m_BufferMap;
        private ConcurrentQueue<T> m_BufferQueue;

        private int m_AutoBufferCount;
        public int AutoBufferCount
        {
            get => m_AutoBufferCount;
            private set => Interlocked.Exchange(ref m_AutoBufferCount, value);
        }

        private int m_IsInBuffer;
        public bool IsInBuffer
        {
            get => m_IsInBuffer == 1;
            private set => Interlocked.Exchange(ref m_IsInBuffer, Convert.ToInt32(value));
        }

        private int m_CurrentFrameIndex;
        public int CurrentFrameIndex
        {
            get => m_CurrentFrameIndex;
            private set => Interlocked.Exchange(ref m_CurrentFrameIndex, value);
        }

        private int m_LockedFrameIndex;
        public int LockedFrameIndex
        {
            get => m_LockedFrameIndex;
            private set => Interlocked.Exchange(ref m_LockedFrameIndex, value);
        }


        private int m_NewestFrameIndex;
        public int NewestFrameIndex
        {
            get => m_NewestFrameIndex;
            private set => Interlocked.Exchange(ref m_NewestFrameIndex, value);
        }

        public JitterBuffer()
        {
            m_BufferMap = new Dictionary<int, T>();
            m_BufferQueue = new ConcurrentQueue<T>();
        }

        public T this[int key]
        {
            get
            {
                lock (m_BufferMap)
                {
                    return m_BufferMap.ContainsKey(key) ? m_BufferMap[key] : default(T);
                }
            }
        }

        public void AddBuffer(int key, T value)
        {
            NewestFrameIndex = key;

            lock (m_BufferMap)
            {
                m_BufferMap.Add(key, value);
            }

            m_BufferQueue.Enqueue(value);
        }

        public void Tick()
        {

        }

        public bool GetBuffer(out T buffer) => m_BufferQueue.TryDequeue(out buffer);

        public void Clear() { }
    }
}
