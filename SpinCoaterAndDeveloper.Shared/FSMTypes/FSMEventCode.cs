using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared
{
    /// <summary>
    /// 定义设备事件
    /// </summary>
    public class FSMEventCode
    {
        public const string StartUp = "启动按钮事件";
        public const string Reset = "复位按钮事件";
        public const string GlobleResetSuccess = "全局复位完成事件";
        public const string GlobleResetFail = "全局复位失败事件";
        public const string Pause = "暂停按钮事件";
        public const string EmergencyStop = "急停按钮事件";
        public const string Stop = "停止按钮事件";
        public const string EnterBurnIn = "启动老化测试事件";
        public const string LeaveBurnIn = "结束老化测试事件";
        public const string Alarm = "报警事件";
    }
}
