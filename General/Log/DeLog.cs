//#define UNITY
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.General.Log
{
    public static class DeLog
    {
        public static bool EnableDate = false;
        public static bool EnableTime = true;

#if UNITY
        private static UnityLog LogObject = new UnityLog();
#else
        private static CustomLog LogObject = new ConsoleLog();
#endif

        private static CustomLog CurrentLog
        {
            get
            {
                lock (LogObject)
                {
                    return LogObject;
                }
            }
        }

        public static void Log(string message) => CurrentLog.Log(message);
        public static void Log(object message) => CurrentLog.Log(message);

        public static void LogWarning(string message) => CurrentLog.LogWarning(message);
        public static void LogWarning(object message) => CurrentLog.LogWarning(message);

        public static void LogError(string message) => CurrentLog.LogError(message);
        public static void LogError(object message) => CurrentLog.LogError(message);
    }
}
