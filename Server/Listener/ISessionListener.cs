using Net.General.Data;
using Net.Server.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Server.Listener
{
    public interface ISessionListener
    {
        void OnReceive(ISession session, NetMessage message);
        void OnConnected(ISession session);
        void OnDisconnected(ISession session);
    }
}
