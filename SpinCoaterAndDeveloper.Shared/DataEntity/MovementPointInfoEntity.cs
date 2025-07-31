using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.DataEntity
{
    [SugarTable("movement_point_info")]
    public class MovementPointInfoEntity
    {
        [SugarColumn(IsIdentity = true, IsPrimaryKey = true, ColumnDescription = "主键")]
        public int Id { get; set; }

        [SugarColumn(ColumnDescription = "名称,不可重复,用于程序调用时的名字,一般与CNName相同")]
        public string Name { get; set; }

        [SugarColumn(ColumnDescription = "中文名称,UI上显示", IsNullable = true)]
        public string CNName { get; set; }

        [SugarColumn(ColumnDescription = "英文名称,UI上显示", IsNullable = true)]
        public string ENName { get; set; }

        [SugarColumn(ColumnDescription = "越语名称,UI上显示", IsNullable = true)]
        public string VNName { get; set; }

        [SugarColumn(ColumnDescription = "预留名称,UI上显示", IsNullable = true)]
        public string XXName { get; set; }

        [SugarColumn(ColumnDescription = "轴分组", IsNullable = true)]
        public string Group { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "备注")]
        public string Backup { get; set; }

        [SugarColumn(ColumnDescription = "Tag", IsNullable = true)]
        public string Tag { get; set; }


        [SugarColumn(ColumnDescription = "product外键")]
        public int ProductId { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(ProductId))]
        public ProductInfoEntity ProductInfo { get; set; }


        [Navigate(NavigateType.OneToMany, nameof(MovementPointPositionEntity.MovementPointNameId))]
        public List<MovementPointPositionEntity> MovementPointPositions { get; set; }

        [Navigate(NavigateType.OneToMany, nameof(MovementPointSecurityEntity.MovementPointNameId))]
        public List<MovementPointSecurityEntity> MovementPointSecurities { get; set; }


        [SugarColumn(ColumnDescription = "手动点位运动安全配置是否启用")]
        public bool ManualMoveSecurityEnable { get; set; }

        [SugarColumn(ColumnDescription = "手动运动点位超时时间ms")]
        public double ManualMoveSecurityTimeOut { get; set; }
    }
}
