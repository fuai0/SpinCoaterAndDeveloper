using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.DataEntity
{
    [SplitTable(SplitType.Month)]
    [SugarTable("produce_info_{year}{month}{day}")]
    [SugarIndex("index_produce_info", nameof(ProduceInfoEntity.Id), OrderByType.Asc)]
    public class ProduceInfoEntity
    {
        //自动分表Id不可为自增,使用雪花ID
        //db.Insertable(data).SplitTable().ExecuteReturnSnowflakeIdList(); 插入并返回雪花ID并且自动赋值ID
        //SnowFlakeSingle.WorkId= 唯一数字; 多服务器一定要设置WorkId确保雪花ID不同.
        [SugarColumn(IsPrimaryKey = true)]
        public long Id { get; set; }

        //产品信息码如二维码/SN等,可为空
        [SugarColumn(IsNullable = true)]
        public string ProductCode { get; set; }

        [SugarColumn(ColumnDataType = "DATETIME(3) ", ColumnDescription = "生产开始时间", IsNullable = true)]
        public DateTime ProductStartTime { get; set; }

        [SugarColumn(ColumnDataType = "DATETIME(3) ", ColumnDescription = "生产结束时间", IsNullable = true)]
        public DateTime ProductEndTime { get; set; }

        //产品结果
        [SugarColumn(IsNullable = true)]
        public bool ProductResult { get; set; }

        //产品CT
        [SugarColumn(IsNullable = true, Length = 18, DecimalDigits = 5)]
        public double ProductCT { get; set; }

        //产品关联ID,可为空
        [SugarColumn(IsNullable = true)]
        public int ProductId { get; set; }

        //更新时间,暂时不使用
        [SugarColumn(IsNullable = true)]
        public DateTime UpdateTime { get; set; }

        //分表字段,根据时间分表
        [SugarColumn(ColumnDataType = "DATETIME(3) ")]
        [SplitField]
        public DateTime CreateTime { get; set; }
    }
}
