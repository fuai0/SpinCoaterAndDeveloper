using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared
{
    /// <summary>
    /// 定义设备的状态
    /// </summary>
    public class FSMStateCode
    {
        public const string PowerUpping = "设备上电状态";
        public const string GlobleResetting = "设备全局复位中状态";
        public const string Idling = "设备待机状态";
        public const string Running = "设备运行中状态";
        public const string Pausing = "设备暂停中状态";
        public const string EmergencyStopping = "设备急停中状态";
        public const string Alarming = "设备报警状态";
        public const string BurnInTesting = "空跑测试状态";
        public const string BurnInAlarming = "空跑报警状态";
        public const string BurnInPausing = "空跑暂停状态";
    }
}
