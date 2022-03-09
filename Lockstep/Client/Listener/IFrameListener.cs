using Net.Lockstep.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Lockstep.Client.Listener
{
    public interface IFrameListener
    {
        void OnReceiveFrame(Frame frame);
    }
}
