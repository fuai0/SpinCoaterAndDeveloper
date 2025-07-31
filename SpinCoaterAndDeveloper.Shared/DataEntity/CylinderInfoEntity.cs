using SpinCoaterAndDeveloper.Shared.Models.CylinderModels;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.DataEntity
{
    [SugarTable("cylinder_info")]
    public class CylinderInfoEntity
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "主键")]
        public int Id { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "编号,用于排序")]
        public string Number { get; set; }

        [SugarColumn(ColumnDescription = "名称,不可重复,用于程序调用时的名字,一般与CNName相同")]
        public string Name { get; set; }

        [SugarColumn(ColumnDescription = "中文名称,UI上显示名字", IsNullable = true)]
        public string CNName { get; set; }

        [SugarColumn(ColumnDescription = "英文名称,UI上显示名字", IsNullable = true)]
        public string ENName { get; set; }

        [SugarColumn(ColumnDescription = "越南名称,UI上显示名字", IsNullable = true)]
        public string VNName { get; set; }

        [SugarColumn(ColumnDescription = "预留语言名称,UI上显示名字", IsNullable = true)]
        public string XXName { get; set; }

        [SugarColumn(ColumnDescription = "备注", IsNullable = true)]
        public string Backup { get; set; }

        [SugarColumn(ColumnDescription = "分组", IsNullable = true)]
        public string Group { get; set; }

        [SugarColumn(ColumnDescription = "Tag", IsNullable = true)]
        public string Tag { get; set; }

        [SugarColumn(ColumnDescription = "原点超时时间ms", IsNullable = true)]
        public double OriginPointTimeout { get; set; }

        [SugarColumn(ColumnDescription = "动点超时时间ms", IsNullable = true)]
        public double MovingPointTimeout { get; set; }



        [SugarColumn(ColumnDescription = "电磁阀类型")]
        public ValveType ValveType { get; set; }


        [SugarColumn(ColumnDescription = "单头电磁阀控制输出IO", IsNullable = true)]
        public int SingleValveOutputId { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(SingleValveOutputId))]
        public IOOutputInfoEntity SingleValveOutputInfo { get; set; }


        [SugarColumn(ColumnDescription = "双头电磁阀原点控制输出IO", IsNullable = true)]
        public int DualValveOriginOutputId { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(DualValveOriginOutputId))]
        public IOOutputInfoEntity DualValveOriginOutputInfo { get; set; }


        [SugarColumn(ColumnDescription = "双头电磁阀动点控制输出IO", IsNullable = true)]
        public int DualValveMovingOutputId { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(DualValveMovingOutputId))]
        public IOOutputInfoEntity DualValveMovingOutputInfo { get; set; }



        [SugarColumn(ColumnDescription = "气缸上的传感器数量")]
        public SensorType SensorType { get; set; }

        [SugarColumn(ColumnDescription = "延时时间ms", IsNullable = true)]
        public double DelayTime { get; set; }

        [SugarColumn(ColumnDescription = "传感器原点输入IO", IsNullable = true)]
        public int SensorOriginInputId { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(SensorOriginInputId))]
        public IOInputInfoEntity SensorOriginInputInfo { get; set; }

        [SugarColumn(ColumnDescription = "传感器动点输入IO", IsNullable = true)]
        public int SensorMovingInputId { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(SensorMovingInputId))]
        public IOInputInfoEntity SensorMovingInputInfo { get; set; }


        [SugarColumn(ColumnDescription = "屏蔽原点传感器")]
        public bool ShiedSensorOriginInput { get; set; }

        [SugarColumn(ColumnDescription = "屏蔽原点传感器延时")]
        public double ShiedSensorOriginInputDelayTime { get; set; }

        [SugarColumn(ColumnDescription = "屏蔽动点传感器")]
        public bool ShiedSensorMovingInput { get; set; }

        [SugarColumn(ColumnDescription = "屏蔽动点传感器延时")]
        public double ShiedSensorMovingInputDelayTime { get; set; }
    }
}
