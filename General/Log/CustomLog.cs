using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.General.Log
{
    public abstract class CustomLog
    {
        public string StickNote(string message)
        {
            var content = "> ";

            if (DeLog.EnableDate)
                content += $"{DateTime.Now:yyyy.MM.dd} ";

            if (DeLog.EnableTime)
                content += $"[{DateTime.Now:HH:mm:ss}] ";

            content += message;

            return content;
        }

        public abstract void Log(string message);
        public abstract void Log(object message);

        public abstract void LogWarning(string message);
        public abstract void LogWarning(object message);

        public abstract void LogError(string message);
        public abstract void LogError(object message);
    }
}
