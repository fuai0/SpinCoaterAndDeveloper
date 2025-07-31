using SpinCoaterAndDeveloper.Shared.Models.MovementPointSecurityGraphModels;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.DataEntity
{
    [SugarTable("movement_point_security")]
    public class MovementPointSecurityEntity
    {
        [SugarColumn(IsIdentity = true, IsPrimaryKey = true, ColumnDescription = "主键")]
        public int Id { get; set; }

        [SugarColumn(ColumnDescription = "movement_point_security外键")]
        public int MovementPointNameId { get; set; }

        [SugarColumn(ColumnDescription = "顺序")]
        public int Sequence { get; set; }

        [SugarColumn(ColumnDescription = "安全类型")]
        public MovementPointSecurityTypes SecurityTypes { get; set; }

        [SugarColumn(ColumnDescription = "安全动作名称")]
        public string Name { get; set; }

        [SugarColumn(ColumnDescription = "动作类型为输出点位/气缸时,输出值")]
        public bool BoolSecurityTypeValue { get; set; }

        [SugarColumn(ColumnDescription = "动作类型为输出点位/气缸时,执行延迟时间ms")]
        public int IOOutputSecurityTypeDelayValue { get; set; }
    }
}
