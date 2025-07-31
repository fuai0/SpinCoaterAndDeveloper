using MotionControlActuation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.Models.CylinderModels
{
    public class CylinderInfo
    {
        /// <summary>
        /// 数据库映射Id
        /// </summary>
        public int Id { private get; set; }
        public int GetId() => Id;
        /// <summary>
        /// 气缸编号
        /// </summary>
        public string Number { private get; set; }
        public string GetNumber() => Number;
        /// <summary>
        /// 气缸名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 中文名称
        /// </summary>
        public string CNName { private get; set; }
        public string GetCNName() => CNName;
        /// <summary>
        /// 英文名称
        /// </summary>
        public string ENName { private get; set; }
        public string GetENName() => ENName;
        /// <summary>
        /// 越语名称
        /// </summary>
        public string VNName { private get; set; }
        public string GetVNName() => VNName;
        /// <summary>
        /// 预留语言名称
        /// </summary>
        public string XXName { private get; set; }
        public string GetXXName() => XXName;
        /// <summary>
        /// 备注
        /// </summary>
        public string Backup { private get; set; }
        public string GetBackup() => Backup;
        /// <summary>
        /// 分组
        /// </summary>
        public string Group { private get; set; }
        public string GetGroup() => Group;
        /// <summary>
        /// Tag
        /// </summary>
        public string Tag { private get; set; }
        public string GetTag() => Tag;
        /// <summary>
        /// 原点超时时间ms
        /// </summary>
        public double OriginPointTimeout { get; set; }
        /// <summary>
        /// 动点超时时间ms
        /// </summary>
        public double MovingPointTimeout { get; set; }
        /// <summary>
        /// 控制电磁阀类型:双头/单头
        /// </summary>
        public ValveType ValveType { get; set; }
        /// <summary>
        /// 单头电磁阀控制输出关联IO表的Id
        /// </summary>
        public int SingleValveOutputId { private get; set; }
        public int GetSingleValveOutputId() => SingleValveOutputId;
        /// <summary>
        /// 单头电磁阀控制输出关联IO的IO信息
        /// </summary>
        public IOOutputInfo SingleValveOutputInfo { get; set; }
        /// <summary>
        /// 双头电磁阀原点控制输出关联IO表的Id
        /// </summary>
        public int DualValveOriginOutputId { private get; set; }
        public int GetDualValveOriginOutputId() => DualValveOriginOutputId;
        /// <summary>
        /// 双头电磁阀原点控制输出关联IO的IO信息
        /// </summary>
        public IOOutputInfo DualValveOriginOutputInfo { get; set; }
        /// <summary>
        /// 双头电磁阀动点控制输出关联IO表的Id
        /// </summary>
        public int DualValveMovingOutputId { private get; set; }
        public int GetDualValveMovingOutputId() => DualValveMovingOutputId;
        /// <summary>
        /// 双头电磁阀动点控制输出关联IO的IO信息
        /// </summary>
        public IOOutputInfo DualValveMovingOutputInfo { get; set; }
        /// <summary>
        /// 气缸上的传感器数量类型:无传感器/单原点传感器/单动点传感器/双传感器
        /// </summary>
        public SensorType SensorType { get; set; }
        /// <summary>
        /// 无传感器时,延时时间
        /// </summary>
        public double DelayTime { get; set; }
        /// <summary>
        /// 传感器原点输入IO关联的Id
        /// </summary>
        public int SensorOriginInputId { private get; set; }
        public int GetSensorOriginInputId() => SensorOriginInputId;
        /// <summary>
        /// 传感器原点输入IO的信息
        /// </summary>
        public IOInputInfo SensorOriginInputInfo { get; set; }
        /// <summary>
        /// 传感器动点输入IO关联的Id
        /// </summary>
        public int SensorMovingInputId { private get; set; }
        public int GetSensorMovingInputId() => SensorMovingInputId;
        /// <summary>
        /// 传感器动点输入IO的信息
        /// </summary>
        public IOInputInfo SensorMovingInputInfo { get; set; }
        /// <summary>
        /// 屏蔽原点传感器
        /// </summary>
        public bool ShiedSensorOriginInput { get; set; }
        /// <summary>
        /// 屏蔽原点传感器延时
        /// </summary>
        public double ShiedSensorOriginInputDelayTime { get; set; }
        /// <summary>
        /// 屏蔽动点传感器
        /// </summary>
        public bool ShiedSensorMovingInput { get; set; }
        /// <summary>
        /// 屏蔽动点传感器延时
        /// </summary>
        public double ShiedSensorMovingInputDelayTime { get; set; }
    }
}
