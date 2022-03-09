using Net.General.Extenstions;
using Net.General.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Net.General.Utils
{
    public static class NetUtils
    {
        /// <summary>
        /// 获取本地Tcp监听端口信息
        /// </summary>
        /// <returns></returns>
        public static IPEndPoint[] GetLocalTcpListeners()
        {
            var globalProperties = IPGlobalProperties.GetIPGlobalProperties();
            return globalProperties.GetActiveTcpListeners();
        }

        /// <summary>
        /// 获取本地Udp监听端口信息
        /// </summary>
        /// <returns></returns>
        public static IPEndPoint[] GetLocalUdpListeners()
        {
            var globalProperties = IPGlobalProperties.GetIPGlobalProperties();
            return globalProperties.GetActiveUdpListeners();
        }

        /// <summary>
        /// 检测端口是否占用
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static bool CheckPortIsUse(int port)
        {
            var listeners = GetLocalTcpListeners();

            for (int i = 0; i < listeners.Length; i++)
                if (listeners[i].Port == port)
                    return true;

            return false;
        }

        private static Dictionary<int, IPEndPoint> m_EndPointMap = new Dictionary<int, IPEndPoint>();
        public static int GetRandomEndPoint()
        {
            m_EndPointMap.Clear();

            var udpEndPoints = GetLocalUdpListeners();
            var tcpEndPoints = GetLocalTcpListeners();

            for (int i = 0; i < udpEndPoints.Length; i++)
                m_EndPointMap.Add(udpEndPoints[i].Port,udpEndPoints[i]);

            for (int i = 0; i < tcpEndPoints.Length; i++)
                m_EndPointMap.Add(tcpEndPoints[i].Port, tcpEndPoints[i]);

            Random random = new Random();

            while (true)
            {
                var port = random.Next(IPEndPoint.MinPort, IPEndPoint.MaxPort);

                if (m_EndPointMap.ContainsKey(port))
                    continue;

                return port;
            }
        }

        /// <summary>
        /// 获取主机名
        /// </summary>
        /// <returns></returns>
        public static string GetHostName() => Dns.GetHostName();

        /// <summary>
        /// 获取本机IP地址信息
        /// </summary>
        /// <returns></returns>
        public static IPHostEntry GetLocalHostEntry()
        {
            var hostName = GetHostName();
            return Dns.GetHostEntry(hostName);
        }

        /// <summary>
        /// 异步获取本机IP地址信息
        /// </summary>
        /// <returns></returns>
        public static Task<IPHostEntry> GetLocalHostEntryAsync()
        {
            var hostName = GetHostName();
            return Dns.GetHostEntryAsync(hostName);
        }

        /// <summary>
        /// 异步Ping指定主机
        /// </summary>
        /// <param name="host"></param>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        public static Task<PingReply> PingHostAsync(string host, int timeOut = 5000)
        {
            return Task.Run(async () =>
            {
                using (var ping = new Ping())
                {
                    var address = IPAddress.Parse(host);
                    var reply = await ping.SendPingAsync(address, timeOut);
                    return reply;
                }
            });
        }

        /// <summary>
        /// 异步Ping指定主机
        /// </summary>
        /// <param name="host"></param>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        public static Task<PingReply> PingHostAsync(IPAddress host, int timeOut = 5000)
        {
            return Task.Run(async () =>
            {
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(host, timeOut);
                    return reply;
                }
            });
        }

        /// <summary>
        /// 获取所有网络接口
        /// </summary>
        /// <returns></returns>
        public static NetworkInterface[] GetNetworkInterfaces() => NetworkInterface.GetAllNetworkInterfaces();

        /// <summary>
        /// 扫描本地主机
        /// </summary>
        /// <param name="gateway"></param>
        /// <param name="minHost"></param>
        /// <param name="maxHost"></param>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        public static Task<List<IPAddress>> ScanLocalHostFromPing(int gateway, int minHost, int maxHost, int timeOut) => ScanLocalHostFromPing(gateway, gateway + 1, minHost, maxHost, timeOut);
        public static Task<List<IPAddress>> ScanLocalHostFromPing(int minGateway = 0, int maxGateway = 255, int minHost = 1, int maxHost = 255, int timeOut = 5000)
        {
            minGateway = MathExtenstion.Clamp(minGateway, 0, 255);
            maxGateway = MathExtenstion.Clamp(maxGateway, 0, 255);

            minHost = MathExtenstion.Clamp(minHost, 1, 255);
            maxHost = MathExtenstion.Clamp(maxHost, 1, 255);

            var taskArray = new Task[(maxGateway - minGateway) * (maxHost - minHost)];

            var addressList = new List<IPAddress>();

            var count = taskArray.Length;

            Task PingHost(IPAddress host)
            {
                return Task.Run(async () =>
                {
                    var pingReply = await PingHostAsync(host, timeOut);

                    DeLog.Log(Interlocked.Decrement(ref count));
                    if (pingReply != null && pingReply.Status == IPStatus.Success)
                    {
                        lock (addressList)
                        {
                            addressList.Add(pingReply.Address);
                        }
                    }
                });
            }

            return Task.Run(() =>
            {
                var index = taskArray.Length - 1;
                for (int i = minGateway; i < maxGateway; i++)
                {
                    for (int j = minHost; j < maxHost; j++)
                    {
                        var task = PingHost(IPAddress.Parse($"192.168.{i}.{j}"));

                        taskArray[index--] = task;
                    }
                }

                try
                {
                    Task.WaitAll(taskArray);
                }
                catch (Exception error)
                {
                    Console.WriteLine(error);
                }
                return addressList;
            });
        }
    }
}
