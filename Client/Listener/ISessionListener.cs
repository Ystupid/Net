using Net.Client.Session;
using Net.General.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Client.Listener
{
    public interface ISessionListener
    {
        void OnReceive(ISession session, NetMessage message);
        void OnConnected(ISession session);
        void OnDisconnected(ISession session);
    }
}
