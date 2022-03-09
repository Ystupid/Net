using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Net.Client.Session
{
    public interface ISession
    {
        uint ID { get; }

        IPEndPoint LocalEndPoint { get; }
        IPEndPoint RemoteEndPoint { get; }

        bool IsConnected { get; }

        bool Connect(string host, int port);
        Task<bool> ConnectAsync(string host, int port);

        void Close();
        void Tick();

        bool Send(Memory<byte> message);
        Task<bool> SendAsync(Memory<byte> message);
    }
}
