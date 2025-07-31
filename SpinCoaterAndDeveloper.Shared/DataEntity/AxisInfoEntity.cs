using MotionControlActuation;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.DataEntity
{
    [SugarTable("axis_info")]
    public class AxisInfoEntity
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "主键")]
        public int Id { get; set; }

        [SugarColumn(ColumnDescription = "轴ID,用于操作板卡轴ID")]
        public int AxisIdOnCard { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "编号,用于轴排序,用于查询运动控制点时控制顺序")]
        public string Number { get; set; }

        [SugarColumn(ColumnDescription = "轴名称,不可重复,用于程序调用轴时的名字,一般与CNName相同")]
        public string Name { get; set; }

        [SugarColumn(ColumnDescription = "中文名称,UI上显示名字", IsNullable = true)]
        public string CNName { get; set; }

        [SugarColumn(ColumnDescription = "英文名称,UI上显示名字", IsNullable = true)]
        public string ENName { get; set; }

        [SugarColumn(ColumnDescription = "越南名称,UI上显示名字", IsNullable = true)]
        public string VNName { get; set; }

        [SugarColumn(ColumnDescription = "预留语言名称,UI上显示名字", IsNullable = true)]
        public string XXName { get; set; }

        [SugarColumn(ColumnDescription = "回原方式")]
        public int HomeMethod { get; set; }

        [SugarColumn(ColumnDescription = "回原High速度", Length = 18, DecimalDigits = 3)]
        public double HomeHighVel { get; set; }

        [SugarColumn(ColumnDescription = "回原Low速度", Length = 18, DecimalDigits = 3)]
        public double HomeLowVel { get; set; }

        [SugarColumn(ColumnDescription = "回原加速度", Length = 18, DecimalDigits = 3)]
        public double HomeAcc { get; set; }

        [SugarColumn(ColumnDescription = "回原超时时间ms")]
        public int HomeTimeout { get; set; }

        [SugarColumn(ColumnDescription = "轴当量")]
        public int Proportion { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "HomeOffset原点偏移", DefaultValue = "0")]
        public double HomeOffset { get; set; }

        [SugarColumn(ColumnDescription = "轴类型:旋转轴,直线轴")]
        public AxisType Type { get; set; }

        [SugarColumn(ColumnDescription = "轴软限位是否启用")]
        public bool SoftLimitEnable { get; set; }

        [SugarColumn(ColumnDescription = "轴软正限位")]
        public double SoftPositiveLimitPos { get; set; }

        [SugarColumn(ColumnDescription = "轴软负限位")]
        public double SoftNegativeLimitPos { get; set; }

        [SugarColumn(ColumnDescription = "轴分组", IsNullable = true)]
        public string Group { get; set; }

        [SugarColumn(ColumnDescription = "Tag", IsNullable = true)]
        public string Tag { get; set; }

        [SugarColumn(ColumnDescription = "Jog速度", Length = 18, DecimalDigits = 3)]
        public double JogVel { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "是否为安全轴")]
        public bool SafeAxisEnable { get; set; }

        [SugarColumn(DefaultValue = "0.000", ColumnDescription = "安全轴的安全位", Length = 18, DecimalDigits = 3)]
        public double SafeAxisPosition { get; set; }

        [SugarColumn(DefaultValue = "0.5", ColumnDescription = "到达目标位置间隙", Length = 18, DecimalDigits = 3)]
        public double TargetLocationGap { get; set; }
    }
}
