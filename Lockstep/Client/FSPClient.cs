using Net.Client.Listener;
using Net.Client.Session;
using Net.General.Config;
using Net.General.Data;
using Net.General.Log;
using Net.General.Utils;
using Net.Lockstep.Client.Listener;
using Net.Lockstep.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Net.General.Data.NetProtocol;

namespace Net.Lockstep.Client
{
    public class FSPClient : ISessionListener
    {
        private IFrameListener m_Listener;
        public IFrameListener Listener
        {
            get => m_Listener;
            set => m_Listener = value;
        }

        private KcpSession m_Session;
        private C2S_FSPData m_SendData;
        private ClientConfig m_Config;

        public FSPClient(uint sessionID, IFrameListener listener, ClientConfig config)
        {
            m_Config = config;
            m_Listener = listener;
            m_Session = new KcpSession(sessionID,this, config);
            m_SendData = new C2S_FSPData();
            m_SendData.SessionID = sessionID;
            m_SendData.Messages.Add(new Message());
        }

        public bool Connect() => m_Session.Connect(m_Config.Host, m_Config.Port);
        public Task<bool> ConnectAsync() => m_Session.ConnectAsync(m_Config.Host, m_Config.Port);

        public bool Send(int frameID, int commandID, int arg) => Send(frameID, commandID, new int[] { arg });
        public bool Send(int frameID, int commandID, int[] args)
        {
            if (!m_Session.IsConnected) return false;

            var frameMessage = m_SendData.Messages[0];
            frameMessage.CommandID = commandID;
            frameMessage.FrameID = frameID;
            frameMessage.Args = args;

            m_SendData.Messages.Clear();
            m_SendData.Messages.Add(frameMessage);

            var buffer = ProtoHelper.Serialize(m_SendData);

            var protocolHead = new ProtocolHead()
            {
                SessionID = m_Session.ID,
                CommandID = 1,
                DataSize = (uint)buffer.Length
            };

            var message = new NetMessage(protocolHead, buffer);

            return m_Session.Send(message.Serialize());
        }

        public void OnConnected(ISession session) => DeLog.LogError("Connected->" + session);
        public void OnDisconnected(ISession session) => DeLog.LogError("Disconnected->" + session);

        public void OnReceive(ISession session, NetMessage message)
        {
            //DeLog.LogError($"ReceiveMessage: Length:{message.Length} Content:{Encoding.UTF8.GetString(message.Message)}");

            var receiveData = ProtoHelper.Deserialize<S2C_FSPData>(message.Message);

            if (m_Listener == null) return;

            for (int i = 0; i < receiveData.Frames.Count; i++)
                m_Listener.OnReceiveFrame(receiveData.Frames[i]);
        }

        public void Tick() => m_Session.Tick();

        public void Close()
        {
            m_Listener = null;
            m_Session.Close();
        }
    }
}
