using Net.Lockstep.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Lockstep.Server.Listener
{
    public interface IPlayerListener
    {
        void OnReceiveMessage(FSPPlayer player, Message message);
    }
}
