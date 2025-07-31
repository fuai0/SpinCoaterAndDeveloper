using MotionCardServiceInterface;
using SpinCoaterAndDeveloper.Shared.Models.MotionControlModels;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.DataEntity
{
    [SugarTable("movement_point_position")]
    public class MovementPointPositionEntity
    {
        [SugarColumn(IsIdentity = true, IsPrimaryKey = true, ColumnDescription = "主键")]
        public int Id { get; set; }

        [SugarColumn(ColumnDescription = "movement_point_name外键")]
        public int MovementPointNameId { get; set; }

        [SugarColumn(ColumnDescription = "轴移动类型")]
        public MovementType MovementPointType { get; set; }

        [SugarColumn(DefaultValue = "0.00000", Length = 18, DecimalDigits = 5, ColumnDescription = "绝对位移")]
        public double AbsValue { get; set; }

        [SugarColumn(DefaultValue = "0.00000", Length = 18, DecimalDigits = 5, ColumnDescription = "相对位移")]
        public double RelValue { get; set; }


        [SugarColumn(ColumnDescription = "Jog方向")]
        public Direction JogDirection { get; set; }

        [SugarColumn(ColumnDescription = "Jog运动停止运动IO名称", IsNullable = true)]
        public int JogIOInputId { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(JogIOInputId))]
        public IOInputInfoEntity JogIOInputInfo { get; set; }

        [SugarColumn(ColumnDescription = "Jog停止运动条件")]
        public JogArrivedType JogArrivedCondition { get; set; }


        [SugarColumn(DefaultValue = "100.00000", Length = 18, DecimalDigits = 5)]
        public double Vel { get; set; }

        [SugarColumn(DefaultValue = "100.00000", Length = 18, DecimalDigits = 5)]
        public double Acc { get; set; }

        [SugarColumn(DefaultValue = "100.00000", Length = 18, DecimalDigits = 5)]
        public double Dec { get; set; }

        [SugarColumn(DefaultValue = "0.00000", Length = 18, DecimalDigits = 5)]
        public double Offset { get; set; }


        [SugarColumn(ColumnDescription = "axis_info外键")]
        public int AxisInfoId { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(AxisInfoId))]
        public AxisInfoEntity AxisInfo { get; set; }

        [SugarColumn(ColumnDescription = "此轴是否在点位中被使用")]
        public bool InvolveAxis { get; set; }
    }
}
