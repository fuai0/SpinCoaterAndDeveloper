using DataBaseServiceInterface;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.Extensions
{
    public static class DataBaseExtensions
    {
        /// <summary>
        /// 添加新产品记录到数据库.
        /// 产品记录表使用自动分表,且主键使用雪花ID.
        /// </summary>
        /// <param name="dataBaseService"></param>
        /// <param name="produceInfoEntity"></param>
        public static void InsertProductInfo(this IDataBaseService dataBaseService, ProduceInfoEntity produceInfoEntity)
        {
            dataBaseService.Db.Insertable(produceInfoEntity).SplitTable().ExecuteReturnSnowflakeIdList();
        }
    }
}
