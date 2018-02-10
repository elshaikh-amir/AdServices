using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdServices___Server.Utils
{
    class Log
    {
        private static System.IO.TextWriter _textWriter;
        private static volatile object _oLock;
        private static Boolean enabled;

        public static void TurnLog(bool b)
        {
            if (!b)
                SYS("Command Log, Log Turned: Off");
            else
                SYS("Command Log, Log Truned: On");
            enabled = b;
        }

        public static void Initialize()
        {
            _textWriter = Console.Out;
            _oLock = new object();
            enabled = true;
        }

        public static void Write(string format, params object[] pParams)
        {
            var final = string.Format("[{0}] - {1} - ", DateTime.Now, new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name);
            var str = string.Format(format, pParams);
            lock (_oLock)
            {
                System.IO.File.AppendAllText("ALog.txt", final + str + "\n");
                if (!enabled)
                    return;
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                _textWriter.Write(final);
                Console.ForegroundColor = ConsoleColor.Gray;
                _textWriter.WriteLine(str);
                _textWriter.Flush();
            }
        }

        public static void SYS(string format, params object[] pParams)
        {
            var final = string.Format("[{0}] - SYSTEM - ", DateTime.Now);
            var str = string.Format(format, pParams);
            lock (_oLock)
            {
                System.IO.File.AppendAllText("ALog.txt", final + str + "\n");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                _textWriter.Write(final);
                Console.ForegroundColor = ConsoleColor.Gray;
                _textWriter.WriteLine(str);
                _textWriter.Flush();
            }
        }

        public static void SYS_NOLINE(string format, params object[] pParams)
        {
            var final = string.Format("[{0}] - SYSTEM - ", DateTime.Now);
            var str = string.Format(format, pParams);
            lock (_oLock)
            {
                System.IO.File.AppendAllText("ALog.txt", final + str + "\n");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                _textWriter.Write(final);
                Console.ForegroundColor = ConsoleColor.Gray;
                _textWriter.Write(str);
                _textWriter.Flush();
            }
        }

    }
}
