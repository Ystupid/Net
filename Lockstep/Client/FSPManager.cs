using Net.General.Config;
using Net.Lockstep.Client.JitterBuffer;
using Net.Lockstep.Client.Listener;
using Net.Lockstep.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Lockstep.Client
{
    public class FSPManager : IFrameListener
    {
        private bool m_IsRunning;
        public bool IsRunning => m_IsRunning;
        private FSPParm m_Param;
        private FSPClient m_Client;
        private JitterBuffer<Frame> m_JitterBuffer;

        private GameState m_State;
        public GameState State => m_State;

        private int m_CurrentFrameIndex;
        private int m_LockedFrameIndex;

        private IGameListener m_Listener;

        public FSPManager(uint playerID, ClientConfig config, IGameListener listener)
        {
            m_Listener = listener;
            m_JitterBuffer = new JitterBuffer<Frame>();
            m_Client = new FSPClient(playerID, this, config);
        }

        public void Start(FSPParm param)
        {
            m_Param = param;
            m_LockedFrameIndex = m_Param.clientFrameRateMultiple - 1;
            ChangeState(GameState.Create);
            m_Client.Connect();
            m_IsRunning = true;
        }

        public void Close()
        {
            ChangeState(GameState.None);
            m_Client?.Close();
            m_Client = null;
            m_Listener = null;
            m_IsRunning = false;
            m_JitterBuffer.Clear();
        }

        public void Send(int commandID, params int[] args)
        {
            if (!IsRunning) return;
            m_Client.Send(m_CurrentFrameIndex, commandID, args);
        }

        public void OnReceiveFrame(Frame frame)
        {
            if (frame.FrameID <= 0)
            {
                ExecuteFrame(frame);
                return;
            }

            //frame.FrameID = frame.FrameID * m_Param.clientFrameRateMultiple;
            //m_LockedFrameIndex = frame.FrameID + m_Param.clientFrameRateMultiple - 1;

            //存入缓冲池
            m_JitterBuffer.AddBuffer(frame.FrameID, frame);
        }

        private void HandleGameState()
        {

        }

        private void ExecuteFrame(Frame frame)
        {
            if (frame != null && !frame.IsEmpty)
            {
                for (int i = 0; i < frame.MessageList.Count; i++)
                {
                    var message = frame.MessageList[i];

                    switch (message.CommandID)
                    {
                        case GameCommand.GAME_BEGIN:
                            ChangeState(GameState.GameBegin);
                            m_Listener?.OnGameBegin();
                            break;
                        case GameCommand.ROUND_BEGIN:
                            ChangeState(GameState.GameBegin);
                            m_Listener?.OnRoundBegin();
                            break;
                        case GameCommand.CONTROL_START:
                            ChangeState(GameState.ControlStart);
                            m_Listener?.OnControlStart();
                            break;
                        case GameCommand.ROUND_END:
                            ChangeState(GameState.RoundEnd);
                            m_Listener?.OnRoundEnd();
                            break;
                        case GameCommand.GAME_END:
                            ChangeState(GameState.GameEnd);
                            m_Listener?.OnGameEnd();
                            break;
                        case GameCommand.GAME_EXIT:
                            ChangeState(GameState.None);
                            m_Listener?.OnGameExit();
                            break;
                    }

                }
            }

            m_Listener?.OnEnterFrame(frame);
        }

        public void Tick()
        {
            if (!IsRunning) return;

            m_Client.Tick();
            m_JitterBuffer.Tick();

            while (m_JitterBuffer.GetBuffer(out var frame))
            {
                ExecuteFrame(frame);
            }
        }

        public void ChangeState(GameState state) => m_State = state;
    }
}
