using Net.Lockstep.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Lockstep.Client.Listener
{
    public interface IGameListener
    {
        void OnGameBegin();
        void OnRoundBegin();
        void OnControlStart();
        void OnGameEnd();
        void OnRoundEnd();
        void OnGameExit();

        void OnEnterFrame(Frame frame);
    }
}
