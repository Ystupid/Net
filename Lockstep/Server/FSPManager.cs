using Net.General;
using Net.General.Config;
using Net.General.Log;
using Net.Server.Gateway;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Lockstep.Server
{
    public class FSPManager
    {
        private long m_LastTickTime;
        private ServerConfig m_Config;
        private KcpGateway m_Gateway;
        private Dictionary<uint, FSPGame> m_GameMap;

        public FSPGame this[uint gameID]
        {
            get
            {
                lock (m_GameMap)
                {
                    return m_GameMap[gameID];
                }
            }
        }

        public FSPManager(ServerConfig config)
        {
            m_Config = config;
            m_Gateway = new KcpGateway(null, m_Config);
            m_GameMap = new Dictionary<uint, FSPGame>();
        }

        public void Start()
        {
            m_Gateway.Start();
            DeLog.Log($"帧同步服务器启动:{m_Config}");
        }

        public void Close()
        {
            if (m_Gateway == null) return;

            m_Gateway.Close();
            m_Gateway = null;
        }

        public FSPGame CreateGame(uint gameID, int authID)
        {
            var game = new FSPGame(gameID, authID);

            m_GameMap.Add(gameID, game);

            return game;
        }

        public void ReleaseGame(uint gameID)
        {
            if (!m_GameMap.ContainsKey(gameID)) return;

            using (var game = m_GameMap[gameID])
            {
                m_GameMap.Remove(gameID);
            }
        }

        public FSPPlayer AddPlayer(uint gameID, uint playerID)
        {
            var game = this[gameID];
            var session = m_Gateway.CreateSession(null);
            return game.AddPlayer(playerID, session);
        }

        public void Tick()
        {
            m_Gateway.Tick();

            var currentTick = DateTime.UtcNow.Ticks;
            var interval = currentTick - m_LastTickTime;
            var frameInterval = 66 * 10000;
            if (interval < frameInterval) return;

            m_LastTickTime = currentTick - (currentTick % frameInterval);

            foreach (var game in m_GameMap.Values)
                game.Tick();
        }
    }
}
