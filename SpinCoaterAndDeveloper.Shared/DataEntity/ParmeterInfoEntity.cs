using SpinCoaterAndDeveloper.Shared.Models.MotionControlModels;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.DataEntity
{
    [SugarTable("parmeter_info")]
    public class ParmeterInfoEntity
    {
        [SugarColumn(IsIdentity = true, IsPrimaryKey = true, ColumnDescription = "主键")]
        public int Id { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "编号,用于排序")]
        public string Number { get; set; }

        [SugarColumn(ColumnDescription = "名称,不可重复,用于程序调用时的名字,一般与CNName相同")]
        public string Name { get; set; }

        [SugarColumn(ColumnDescription = "中文名称", IsNullable = true)]
        public string CNName { get; set; }

        [SugarColumn(ColumnDescription = "英文名称", IsNullable = true)]
        public string ENName { get; set; }

        [SugarColumn(ColumnDescription = "越南名称,UI上显示名字", IsNullable = true)]
        public string VNName { get; set; }

        [SugarColumn(ColumnDescription = "预留语言名称,UI上显示名字", IsNullable = true)]
        public string XXName { get; set; }

        [SugarColumn(ColumnDescription = "内容", IsNullable = true)]
        public string Data { get; set; }

        [SugarColumn(ColumnDescription = "数据类型,用于数据校验", IsNullable = true)]
        public ParmeterType DataType { get; set; }

        [SugarColumn(ColumnDescription = "数据单位", IsNullable = true)]
        public string Unit { get; set; }

        [SugarColumn(ColumnDescription = "分组", IsNullable = true)]
        public string Group { get; set; }

        [SugarColumn(IsNullable = true)]
        public string Backup { get; set; }

        [SugarColumn(ColumnDescription = "Tag", IsNullable = true)]
        public string Tag { get; set; }

        [SugarColumn(ColumnDescription = "product外键")]
        public int ProductId { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(ProductId))]
        public ProductInfoEntity ProductInfo { get; set; }

    }
}
