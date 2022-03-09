using Net.General;
using Net.General.Config;
using Net.General.Data;
using Net.General.Log;
using Net.General.Utils;
using Net.Server.Gateway;
using Net.Server.Listener;
using Net.Server.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Server
{
    public class NetManager : ISessionListener
    {
        private IGateway m_Gateway;
        private ServerConfig m_Config;

        public NetManager(ServerConfig config)
        {
            m_Config = config;
            switch (config.ConnectionType)
            {
                case ConnectionType.TCP: m_Gateway = new TcpGateway(this, config); break;
                case ConnectionType.UDP: m_Gateway = new UdpGateway(this, config); break;
                case ConnectionType.KCP: m_Gateway = new KcpGateway(this, config); break;
                default: m_Gateway = new TcpGateway(this, config); break;
            }
        }

        public void Start()
        {
            m_Gateway.Start();
            DeLog.Log($"服务器启动-> \n{m_Config}");
        }

        public void Tick() => m_Gateway.Tick();

        public bool Send<T>(ISession session, T message)
        {
            var buffer = ProtoHelper.Serialize(message);
            return session.Send(buffer);
        }

        public Task<bool> SendAsync<T>(ISession session, T message)
        {
            var buffer = ProtoHelper.Serialize(message);
            return session.SendAsync(buffer);
        }

        public void OnConnected(ISession session)
        {
            DeLog.Log($"Accept->" + session);
        }

        public void OnDisconnected(ISession session)
        {
            DeLog.LogError("Disconnected->" + session);
        }

        public void OnReceive(ISession session, NetMessage message)
        {
            DeLog.LogError($"ReceiveMessage: Length:{message.Length} Content:{Encoding.UTF8.GetString(message.Message)}");
            var protocolHead = message.ProtocolHead;
            protocolHead.SessionID = session.ID;
            message.ProtocolHead = protocolHead;
            session.SendAsync(message.Serialize());
        }

        public void Close()
        {
            m_Gateway.Close();
        }
    }
}
