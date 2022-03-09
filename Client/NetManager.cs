using Net.Client.Listener;
using Net.Client.Session;
using Net.General;
using Net.General.Config;
using Net.General.Data;
using Net.General.Log;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Net.Client
{
    public class NetManager : ISessionListener
    {
        private ClientConfig m_Config;
        private ISession m_Session;

        public NetManager(ClientConfig config)
        {
            m_Config = config;
            switch (config.ConnectionType)
            {
                case ConnectionType.TCP: m_Session = new TcpSession(this, config); break;
                case ConnectionType.UDP: m_Session = new UdpSession(this, config); break;
                case ConnectionType.KCP: m_Session = new KcpSession(this, config); break;
                default: m_Session = new TcpSession(this, config); break;
            }
        }

        public bool Connect()
        {
            DeLog.Log($"ConnectServer-> Host:{m_Config.Host} Port:{m_Config.Port}");

            return m_Session.Connect(m_Config.Host, m_Config.Port);
        }

        public Task<bool> ConnectAsync()
        {
            DeLog.Log($"ConnectServer-> Host:{m_Config.Host} Port:{m_Config.Port}");

            return m_Session.ConnectAsync(m_Config.Host, m_Config.Port);
        }

        public void Send(in byte[] buffer)
        {
            var message = new NetMessage();
            message.Message = buffer;
            message.ProtocolHead = new ProtocolHead
            {
                DataSize = (uint)message.Message.Length,
                SessionID = m_Session.ID
            };

            m_Session.Send(message.Serialize());
        }

        public void SendAsync(in byte[] buffer)
        {
            var message = new NetMessage();
            message.Message = buffer;
            message.ProtocolHead = new ProtocolHead
            {
                DataSize = (uint)message.Message.Length,
                SessionID = m_Session.ID
            };

            m_Session.SendAsync(message.Serialize());
        }

        public void Tick() => m_Session.Tick();

        public void OnDisconnected(ISession session)
        {
            DeLog.LogError("Disconnected->" + session);
        }

        public void OnReceive(ISession session, NetMessage message)
        {
            DeLog.LogError($"ReceiveMessage: Length:{message.Length} Content:{Encoding.UTF8.GetString(message.Message)}");

            session.SendAsync(message.Serialize());
        }

        public void OnConnected(ISession session)
        {
            DeLog.LogError("Connected->" + session);
        }

        public void Close() => m_Session.Close();
    }
}
