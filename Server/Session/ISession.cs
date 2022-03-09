using Net.Server.Listener;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Net.Server.Session
{
    public interface ISession
    {
        uint ID { get; }

        bool IsActived { get; }

        void Init(uint sessionID, ISessionListener listener);
        void Active(Socket socket, IPEndPoint remoteEndPoint);

        bool Send(Memory<byte> message);
        Task<bool> SendAsync(Memory<byte> message);

        void Tick();

        void Close();
    }
}
