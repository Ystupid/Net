using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.Server.Gateway
{
    public interface IGateway
    {
        void Start();
        void ReStart();
        void Close();
        void Tick();
    }
}
