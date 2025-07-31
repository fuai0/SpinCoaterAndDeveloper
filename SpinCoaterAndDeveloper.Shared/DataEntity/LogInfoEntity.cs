using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.DataEntity
{
    [SugarTable("log_info")]
    [SugarIndex("index_log_name", nameof(LogInfoEntity.Id), OrderByType.Asc)]
    public class LogInfoEntity
    {
        [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
        public long Id { get; set; }

        public string Level { get; set; }

        [SugarColumn(IsNullable = true)]
        public string Keyword { get; set; }

        public string Message { get; set; }

        [SugarColumn(ColumnDataType = "DATETIME(3) ")]  //插入时间带毫秒
        public DateTime Time { get; set; }
    }
}
