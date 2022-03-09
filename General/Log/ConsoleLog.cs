using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.General.Log
{
    public class ConsoleLog : CustomLog
    {
        public override void Log(string message)
        {
            Console.WriteLine(StickNote(message));
        }

        public override void Log(object message)
        {
            Console.WriteLine(StickNote(message.ToString()));
        }

        public override void LogError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(StickNote(message));
            Console.ResetColor();
        }

        public override void LogError(object message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(StickNote(message.ToString()));
            Console.ResetColor();
        }

        public override void LogWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(StickNote(message));
            Console.ResetColor();
        }

        public override void LogWarning(object message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(StickNote(message.ToString()));
            Console.ResetColor();
        }
    }
}
