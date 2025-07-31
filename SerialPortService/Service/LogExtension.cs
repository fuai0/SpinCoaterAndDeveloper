using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialPortService.Service
{
    public enum LogLevel
    {
        NONE,
        FATAL,
        ERROR,
        WARN,
        INFO,
        DEBUG,
    }
    public class LogExtension
    {
        public static event Action<string, string, Exception> LogServiceEvent;
        public static void Log(LogLevel level, string text, Exception exception = null)
        {
            LogServiceEvent?.Invoke(level.ToString(), text, exception);
        }

    }
}
