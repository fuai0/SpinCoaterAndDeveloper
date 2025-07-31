using SpinCoaterAndDeveloper.Shared.Models.MovementPointSecurityGraphModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.Models.MotionControlModels
{
    public class MovementPointSercurity
    {
        /// <summary>
        /// 数据库映射Id
        /// </summary>
        public int Id { private get; set; }
        /// <summary>
        /// 数据库映射运动点位Id,关联点位名称
        /// </summary>
        public int MovementPointNameId { private get; set; }
        /// <summary>
        /// 执行顺序
        /// </summary>
        public int Sequence { private get; set; }
        /// <summary>
        /// 执行动作的名字
        /// </summary>
        public string Name { private get; set; }
        /// <summary>
        /// 动作类型
        /// </summary>
        public MovementPointSecurityTypes SecurityTypes { private get; set; }
        /// <summary>
        /// 动作为输出点位时,输出的值
        /// </summary>
        public bool BoolSecurityTypeValue { private get; set; }
        /// <summary>
        /// 动作位IO输出点位时,执行延迟时间
        /// </summary>
        public int IOOutputSecurityTypeDelayValue { private get; set; }

        public int GetId() => Id;
        public int GetMovementPointNameId() => MovementPointNameId;
        public int GetSequence() => Sequence;
        public string GetName() => Name;
        public MovementPointSecurityTypes GetSecurityTypes() => SecurityTypes;
        public bool GetBoolSecurityTypeValue() => BoolSecurityTypeValue;
        public int GetIOOutputSecurityTypeDelayValue() => IOOutputSecurityTypeDelayValue;
    }
}
