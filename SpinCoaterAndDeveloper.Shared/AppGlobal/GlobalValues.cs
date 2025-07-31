using FSM;
using Prism.Mvvm;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using SpinCoaterAndDeveloper.Shared.Models.CylinderModels;
using SpinCoaterAndDeveloper.Shared.Models.MotionControlModels;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared
{
    public partial class GlobalValues : BindableBase
    {
        /// <summary>
        /// 屏蔽报警声音
        /// </summary>
        public static bool SilenceAlarm { get; set; }
        /// <summary>
        /// 联锁检查失败后进入暂停时的报警
        /// </summary>
        public static bool InterlockPauseWithAlarm { get; set; }
        /// <summary>
        /// 当前设备状态
        /// </summary>
        public static string MachineStatus { get; set; } = FSMStateCode.PowerUpping;
        /// <summary>
        /// 当前生产产品,只有一个产品是默认为Default
        /// </summary>
        public static string CurrentProduct { get; set; }
        /// <summary>
        /// 全局速度百分比
        /// </summary>
        public static double GlobalVelPercentage { get; set; }
        /// <summary>
        /// 运动参数集合
        /// </summary>
        public static Dictionary<string, ParmeterInfo> MCParmeterDicCollection { get; set; } = new Dictionary<string, ParmeterInfo>();
        /// <summary>
        /// 运动点位集合
        /// </summary>
        public static Dictionary<string, MovementPointInfo> MCPointDicCollection { get; set; } = new Dictionary<string, MovementPointInfo>();
        /// <summary>
        /// 气缸集合
        /// </summary>
        public static Dictionary<string, CylinderInfo> CylinderDicCollection { get; set; } = new Dictionary<string, CylinderInfo>();
        /// <summary>
        /// 设备状态流转队列
        /// </summary>
        public static ConcurrentQueue<FSMEvent> OperationCommandQueue { get; set; } = new ConcurrentQueue<FSMEvent>();
        /// <summary>
        /// 插补路径数据集合
        /// </summary>
        public static Dictionary<string, InterpolationPathCoordinateEntity> InterpolationPaths { get; set; } = new Dictionary<string, InterpolationPathCoordinateEntity>();
        /// <summary>
        /// 功能屏蔽集合
        /// </summary>
        public static Dictionary<string, FunctionShieldInfo> MCFunctionShieldDicCollection { get; set; } = new Dictionary<string, FunctionShieldInfo>();
    }
}
