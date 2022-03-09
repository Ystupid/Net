using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Net.General.Log
{
    public class UnityLog : CustomLog
    {
        public override void Log(string message) => Debug.Log(message);
        public override void Log(object message) => Debug.Log(message);
        public override void LogError(string message) => Debug.LogError(message);
        public override void LogError(object message) => Debug.LogError(message);
        public override void LogWarning(string message) => Debug.LogWarning(message);
        public override void LogWarning(object message) => Debug.LogWarning(message);
    }
}
