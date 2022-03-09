using Net.General.Data;
using Net.General.Log;
using Net.General.Utils;
using Net.Lockstep.Data;
using Net.Lockstep.Server.Listener;
using Net.Server.Listener;
using Net.Server.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Net.General.Data.NetProtocol;

namespace Net.Lockstep.Server
{
    public class FSPPlayer : IDisposable, ISessionListener
    {
        private uint m_ID;
        public uint ID => m_ID;

        private int m_AuthID;
        public int AuthID => m_AuthID;

        private IPlayerListener m_Listener;
        private KcpSession m_Session;

        private int m_LastFrameID;
        private Queue<Frame> m_FrameCache;

        private S2C_FSPData m_SendData;

        public FSPPlayer(uint playerID, int authID, KcpSession session, IPlayerListener listener)
        {
            m_ID = playerID;
            m_AuthID = authID;
            m_Session = session;
            m_Listener = listener;
            m_FrameCache = new Queue<Frame>();
            m_Session.Listener = this;
            m_SendData = new S2C_FSPData();
        }

        public void SendFrame(Frame frame)
        {
            if (frame != null && !m_FrameCache.Contains(frame))
            {
                m_FrameCache.Enqueue(frame);
            }

            while (m_FrameCache.Count > 0)
            {
                if (Internal_SendFrame(m_FrameCache.Peek()))
                {
                    m_FrameCache.Dequeue();
                }
            }
        }

        private bool Internal_SendFrame(Frame frame)
        {
            if (frame.FrameID != 0 && frame.FrameID <= m_LastFrameID)
                return true;

            m_SendData.Frames.Clear();
            m_SendData.Frames.Add(frame);

            var buffer = ProtoHelper.Serialize(m_SendData);

            var protocolHead = new ProtocolHead
            {
                SessionID = m_Session.ID,
                DataSize = (uint)buffer.Length
            };

            var message = new NetMessage(protocolHead, buffer);

            if (m_Session.Send(message.Serialize()))
            {
                m_LastFrameID = frame.FrameID;
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            m_FrameCache.Clear();
            m_SendData.Frames.Clear();
            m_SendData = null;
            m_Listener = null;
            m_Session?.Close();
        }

        public void OnReceive(ISession session, NetMessage message)
        {
            var data = ProtoHelper.Deserialize<C2S_FSPData>(message.Message);

            if (m_Listener == null) return;

            for (int i = 0; i < data.Messages.Count; i++)
            {
                m_Listener.OnReceiveMessage(this, data.Messages[i]);
            }
        }

        public void OnConnected(ISession session)
        {
            DeLog.Log($"Connected->" + session);
        }

        public void OnDisconnected(ISession session)
        {
            DeLog.LogError("Disconnected->" + session);
        }
    }
}
