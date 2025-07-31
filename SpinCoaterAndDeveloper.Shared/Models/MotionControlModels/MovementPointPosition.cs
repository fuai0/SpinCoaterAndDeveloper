using MotionCardServiceInterface;
using MotionControlActuation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.Models.MotionControlModels
{
    public class MovementPointPosition
    {
        /// <summary>
        /// 数据库映射Id,主键
        /// </summary>
        public int Id { private get; set; }
        /// <summary>
        /// 数据库映射Id,点位名称Id
        /// </summary>
        public int MovementPointNameId { private get; set; }
        /// <summary>
        /// 轴移动类型
        /// </summary>
        public MovementType MovementPointType { private get; set; }
        /// <summary>
        /// 轴运动位置
        /// </summary>
        public double AbsValue { get; set; }
        /// <summary>
        /// 轴运动相对位置
        /// </summary>
        public double RelValue { get; set; }
        /// <summary>
        /// Jog运动方向
        /// </summary>
        public Direction JogDirection { get; set; }
        /// <summary>
        /// Jog运动停止条件IO
        /// </summary>
        public int JogIOInputId { private get; set; }
        /// <summary>
        /// Jog运动关联IO Info
        /// </summary>
        public IOInputInfo JogIOInputInfo { get; set; }
        /// <summary>
        /// Jog运动到位IO停止时条件
        /// </summary>
        public JogArrivedType JogArrivedCondition { get; set; }
        /// <summary>
        /// 轴运动速度
        /// </summary>
        public double Vel { get; set; }
        /// <summary>
        /// 轴运动加速度
        /// </summary>
        public double Acc { get; set; }
        /// <summary>
        /// 轴运动减速度
        /// </summary>
        public double Dec { get; set; }
        /// <summary>
        /// 轴运动偏移量(相对运动,绝对运动的补偿值)
        /// </summary>
        public double Offset { get; set; }
        /// <summary>
        /// 数据库映射Id,关联轴的数据库存储ID.非轴控制ID
        /// </summary>
        public int AxisInfoId { private get; set; }
        /// <summary>
        /// 关联轴Info
        /// </summary>
        public AxisInfo AxisInfo { get; set; }
        /// <summary>
        /// 是否是参与轴,在全局点位信息字典中,只保留参与轴
        /// </summary>
        public bool InvolveAxis { private get; set; }

        public int GetId() => Id;
        public int GetMovementPointId() => MovementPointNameId;
        public MovementType GetMovementPointType() => MovementPointType;
        public int GetJogIOInputId() => JogIOInputId;
        public int GetAxisInfoId() => AxisInfoId;
        public bool GetInvolveAxis() => InvolveAxis;
    }
}
