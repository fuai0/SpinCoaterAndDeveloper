using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.DataEntity
{
    [SugarTable("function_shield_info")]
    public class FunctionShieldEntity
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "主键")]
        public int Id { get; set; }

        [SugarColumn(ColumnDescription = "屏蔽功能名称,用于程序调用")]
        public string Name { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "中文名称,UI上显示")]
        public string CNName { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "英文名称,UI上显示")]
        public string ENName { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "越南名称,UI上显示")]
        public string VNName { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "预留语言名称,UI上显示")]
        public string XXName { get; set; }

        [SugarColumn(ColumnDescription = "是否启用此功能")]
        public bool IsActive { get; set; }

        [SugarColumn(ColumnDescription = "是否显示在主界面UI上")]
        public bool EnableOnUI { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "自定义分类")]
        public string Group { get; set; }

        [SugarColumn(ColumnDescription = "备注", IsNullable = true)]
        public string Backup { get; set; }

        [SugarColumn(ColumnDescription = "product外键")]
        public int ProductId { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(ProductId))]
        public ProductInfoEntity ProductInfo { get; set; }
    }
}
