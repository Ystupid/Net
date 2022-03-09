using Net.General.Config;
using Net.General.Log;
using Net.Server.Listener;
using Net.Server.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Net.Server.Gateway
{
    public class TcpGateway : Gateway<TcpSession>
    {
        public TcpGateway(ISessionListener listener, ServerConfig config = default) : base(listener, config) => m_Config = config;

        public override void Start()
        {
            if (m_Socket != null) Close();

            try
            {
                m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                m_Socket.Bind(new IPEndPoint(IPAddress.Any, m_Config.Port));
                m_Socket.Listen(m_Config.BackLog);
                m_Socket.NoDelay = true;
            }
            catch (Exception error)
            {
                DeLog.LogError(error);
                return;
            }

            m_IsRunning = true;

            AcceptAsync();
        }

        protected override Task AcceptAsync()
        {
            return Task.Run(async () =>
            {
                while (m_IsRunning && m_Socket != null)
                {
                    var client = default(Socket);
                    try
                    {
                        client = await m_Socket.AcceptAsync();
                    }
                    catch (Exception error)
                    {
                        DeLog.LogError(error);
                        await Task.Delay(10);
                    }

                    if (client == null) continue;

                    var session = CreateSession(m_Listener);
                    session.Active(client, client.RemoteEndPoint as IPEndPoint);
                }
            });
        }
    }
}
