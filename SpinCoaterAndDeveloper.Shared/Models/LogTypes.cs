using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared
{
    [Flags]
    public enum LogKeyWord
    {
        //记录进数据库
        DB = 1,
        //记录进HiveLog的MachineError
        AppleHiveMachineError = 2,
        //记录进HiveLog的MachineData
        AppleHiveMachieData = 4,
        //记录进HiveLog的MachineState
        AppleHiveMachineState = 8,
        //记录进Apple要求的Machine Log
        AppleMachineLog = 16,
        //气缸
        AxisError = 32,
        //轴
        CylinderError = 64,
        //传感器
        SensorError = 128,
        //视觉
        VisionError = 256,
        //软件
        SoftwareError = 512,
        //其他
        OthersError = 1024,
    }
    /// <summary>
    /// 日志关键字类,可任何或进行组合,以实现不同的存储与分类
    /// </summary>
    public class LogTypes
    {
        /// <summary>
        /// 需要将日志存储进数据库时需要携带DB关键字
        /// </summary>
        [LogStatisticAttr(false)]
        public static LogKeyWord DB { get; set; } = LogKeyWord.DB;
        /// <summary>
        /// 用于存储进Hive日志的MachineError
        /// </summary>
        [LogStatisticAttr(false)]
        public static LogKeyWord AppleHiveMachineError { get; set; } = LogKeyWord.DB | LogKeyWord.AppleHiveMachineError;
        /// <summary>
        /// 用于存储进Hive日志的MachineMachineData
        /// </summary>
        [LogStatisticAttr(false)]
        public static LogKeyWord AppleHiveMachineData { get; set; } = LogKeyWord.DB | LogKeyWord.AppleHiveMachieData;
        /// <summary>
        /// 用于存储进Hive日志的MachineMachieState
        /// </summary>
        [LogStatisticAttr(false)]
        public static LogKeyWord AppleHiveMachieState { get; set; } = LogKeyWord.DB | LogKeyWord.AppleHiveMachineState;
        /// <summary>
        /// 用于存储进Hive日志的MachineMachineLog
        /// </summary>
        [LogStatisticAttr(false)]
        public static LogKeyWord AppleMachineLog { get; set; } = LogKeyWord.DB | LogKeyWord.AppleMachineLog;
        /// <summary>
        /// 用于统计使用的流程中的轴错误,运动出错
        /// </summary>
        [LogStatisticAttr(true, LogKeyWord.AxisError)]
        public static LogKeyWord ProcessingAxisError { get; set; } = LogKeyWord.DB | LogKeyWord.AxisError;
        /// <summary>
        /// 用于统计使用的流程中的气缸出错
        /// </summary>
        [LogStatisticAttr(true, LogKeyWord.CylinderError)]
        public static LogKeyWord ProcessingCylinderError { get; set; } = LogKeyWord.DB | LogKeyWord.CylinderError;
        /// <summary>
        /// 用于统计使用的流程中的传感器出错
        /// </summary>
        [LogStatisticAttr(true, LogKeyWord.SensorError)]
        public static LogKeyWord ProcessingSensorError { get; set; } = LogKeyWord.DB | LogKeyWord.SensorError;
        /// <summary>
        /// 用于统计使用的流程中的视觉错误
        /// </summary>
        [LogStatisticAttr(true, LogKeyWord.VisionError)]
        public static LogKeyWord ProcessingVisionError { get; set; } = LogKeyWord.DB | LogKeyWord.VisionError;
        /// <summary>
        /// 用于统计使用的流程中的软件错误
        /// </summary>
        [LogStatisticAttr(true, LogKeyWord.SoftwareError)]
        public static LogKeyWord ProcessingSoftwareError { get; set; } = LogKeyWord.DB | LogKeyWord.SoftwareError;
        /// <summary>
        /// 用于统计使用的流程中的其他错误
        /// </summary>
        [LogStatisticAttr(true, LogKeyWord.OthersError)]
        public static LogKeyWord ProcessingOthersError { get; set; } = LogKeyWord.DB | LogKeyWord.OthersError;
    }
    /// <summary>
    /// Hive日志保存路径
    /// </summary>
    public class AppleHiveLogPath
    {
        public const string HiveMcahineError = ".\\HIVE Log\\Machine Error\\";
        public const string HiveMcahineData = ".\\HIVE Log\\Machine Data\\";
        public const string HiveMcahineState = ".\\HIVE Log\\Machine State";
        //Remote Qualification
        public const string HiveMachineErrorWithRemoteQualification = ".\\HIVE Log\\Remote Qualification\\Machine Error\\";
        public const string HiveMachineDataWithRemoteQualification = ".\\HIVE Log\\Remote Qualification\\Machine Data\\";
        public const string HiveMachineStateWithRemoteQualification = ".\\HIVE Log\\Remote Qualification\\Machine State";

        public const string AppleMachineLog = ".\\Machine Log\\";
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class LogStatisticAttr : Attribute
    {
        private bool _EnableStatistic;

        public bool EnableStatistic
        {
            get { return _EnableStatistic; }
            set { _EnableStatistic = value; }
        }

        private LogKeyWord _Category;

        public LogKeyWord Category
        {
            get { return _Category; }
            set { _Category = value; }
        }
        public LogStatisticAttr(bool enableStatistic, LogKeyWord category = default)
        {
            this.EnableStatistic = enableStatistic;
            this.Category = category;
        }
    }
}
