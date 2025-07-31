using FSM;
using LogServiceInterface;
using SpinCoaterAndDeveloper.Shared.Apple;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared
{
    public enum AppleView
    {
        Home,
        Alarm,
        Config,
        Data,
        Vision,
        Setting,
    }

    public partial class GlobalValues
    {
        /// <summary>
        /// Hive日志写入队列
        /// </summary>
        public static ConcurrentQueue<LogMsgEventArgs> AppleLogQueue { get; set; } = new ConcurrentQueue<LogMsgEventArgs>();
    }
}
