using Net.General.Flag;
using Net.General.Log;
using Net.Lockstep.Data;
using Net.Lockstep.Server.Listener;
using Net.Server.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Lockstep.Server
{
    public class FSPGame : IDisposable, IPlayerListener
    {
        private uint m_GameID;
        private int m_AuthID;

        private List<FSPPlayer> m_PlayerList;

        public int PlayerCount => m_PlayerList.Count;
        public FSPPlayer this[int index] => m_PlayerList[index];

        private GameState m_State;
        public GameState State => m_State;

        private int m_FrameID;
        private Frame m_LockedFrame;

        private MultipleFlag m_StateFlag;

        public FSPGame(uint gameID, int authID)
        {
            m_GameID = gameID;
            m_AuthID = authID;
            m_LockedFrame = new Frame();
            m_PlayerList = new List<FSPPlayer>();
            m_StateFlag = new MultipleFlag(5);
            ChangeState(GameState.Create);
        }

        private CustomFlag this[GameState state] => m_StateFlag[(int)state - 1];

        public FSPPlayer AddPlayer(uint playerID, KcpSession session)
        {
            var findIndex = m_PlayerList.FindIndex(matchPlayer => matchPlayer.ID == playerID);

            var player = new FSPPlayer(playerID, m_AuthID, session, this);

            if (findIndex != -1)
            {
                using (var findPlayer = m_PlayerList[findIndex])
                {
                    m_PlayerList[findIndex] = player;
                }
            }
            else m_PlayerList.Add(player);

            return player;
        }

        public FSPPlayer FindPlayer(uint playerID)
        {
            var findIndex = m_PlayerList.FindIndex(player => player.ID == playerID);
            return findIndex == -1 ? null : m_PlayerList[findIndex];
        }

        public void ChangeState(GameState state)
        {
            m_State = state;
        }

        public void Dispose()
        {
            ChangeState(GameState.None);

            var playerCount = m_PlayerList.Count;
            while ((playerCount -= 1) >= 0)
            {
                m_PlayerList[playerCount].Dispose();
                m_PlayerList.RemoveAt(playerCount);
            }
        }

        public void OnReceiveMessage(FSPPlayer player, Message message)
        {
            switch (message.CommandID)
            {
                case GameCommand.AUTH: break;
                case GameCommand.GAME_BEGIN: m_StateFlag[0].Active((int)player.ID); break;
                case GameCommand.ROUND_BEGIN: m_StateFlag[1].Active((int)player.ID); break;
                case GameCommand.CONTROL_START: m_StateFlag[2].Active((int)player.ID); break;
                case GameCommand.ROUND_END: m_StateFlag[3].Active((int)player.ID); break;
                case GameCommand.GAME_END: m_StateFlag[4].Active((int)player.ID); break;
                case GameCommand.GAME_EXIT: DeLog.Log("GameExit"); break;
                default: EnterMessage(player.ID, message); break;
            }
        }

        public void EnterMessage(uint playerID, Message message)
        {
            message.PlayerID = playerID;
            m_LockedFrame.MessageList.Add(message);
        }

        public void EnterCommand(int commandID, int arg = 0)
        {
            var message = new Message();
            message.CommandID = commandID;
            message.Args = new[] { arg };
            EnterMessage(0, message);
        }

        private void EnterNextState()
        {
            if (!CheckState(State))
                return;

            int flag = -1;

            switch (State)
            {
                case GameState.None: break;
                case GameState.Create: flag = 1; EnterCommand(GameCommand.GAME_BEGIN); break;
                case GameState.GameBegin: flag = 1; EnterCommand(GameCommand.ROUND_BEGIN); break;
                case GameState.RoundBegin: flag = 1; EnterCommand(GameCommand.CONTROL_START); break;
                case GameState.ControlStart: flag = 1; EnterCommand(GameCommand.ROUND_END); break;
                case GameState.RoundEnd: flag = 1; EnterCommand(GameCommand.GAME_END); break;
                case GameState.GameEnd: break;
                default: break;
            }

            if (flag != -1)
                ChangeState((GameState)((int)State + 1));
        }

        private bool CheckState(GameState state)
        {
            if (state == GameState.None) return false;

            var customFlag = this[state];
            for (int i = 0; i < m_PlayerList.Count; i++)
                if (!customFlag.IsActive((int)m_PlayerList[i].ID))
                    return false;

            return true;
        }

        public void Tick()
        {
            EnterNextState();

            if (m_LockedFrame.FrameID != 0)
            {
                for (int i = 0; i < m_PlayerList.Count; i++)
                {
                    var player = m_PlayerList[i];
                    player.SendFrame(m_LockedFrame);
                }
            }

            if (m_LockedFrame.FrameID == 0)
                m_LockedFrame = new Frame();

            if (m_State == GameState.RoundBegin || m_State == GameState.ControlStart)
            {
                m_FrameID++;
                m_LockedFrame = new Frame();
                m_LockedFrame.FrameID = m_FrameID;
            }
        }
    }
}
