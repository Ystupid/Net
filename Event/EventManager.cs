using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Event
{
    public class EventManager
    {
        private Dictionary<byte, List<Delegate>> m_EventMap;

        public EventManager()
        {
            m_EventMap = new Dictionary<byte, List<Delegate>>();
        }

        public void Register(byte commandID,Delegate action)
        {

        }

        public void Trigger()
        {

        }
    }
}
