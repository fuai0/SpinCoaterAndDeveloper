using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.DataEntity
{
    [SugarTable("io_output_info")]
    public class IOOutputInfoEntity
    {
        [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
        public int Id { get; set; }

        [SugarColumn(ColumnDescription = "编号")]
        public string Number { get; set; }

        [SugarColumn(ColumnDescription = "输出名称,用于程序调用时名称")]
        public string Name { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "中文名称,UI上显示")]
        public string CNName { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "英文名称,UI上显示")]
        public string ENName { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "越南名称,UI上显示")]
        public string VNName { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "预留语言名称,UI上显示")]
        public string XXName { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "是否反转")]
        public bool ReverseEnable { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "是否有效,屏蔽")]
        public bool ShieldEnable { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "屏蔽时返回值")]
        public bool ShiedlEnableDefaultValue { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "备注")]
        public string Backup { get; set; }

        [SugarColumn(ColumnDescription = "IO硬件位置", IsNullable = true)]
        public string PhysicalLocation { get; set; }

        [SugarColumn(ColumnDescription = "分组", IsNullable = true)]
        public string Group { get; set; }

        [SugarColumn(ColumnDescription = "Tag", IsNullable = true)]
        public string Tag { get; set; }

        [SugarColumn(ColumnDescription = "软件读取写入用组地址,按8bit计算及从板卡取值")]
        public int ProgramAddressGroup { get; set; }

        [SugarColumn(ColumnDescription = "软件读取写入用地址,按8bit计算及从板卡取值")]
        public int ProgramAddressPosition { get; set; }
    }
}
