using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.DataEntity
{
    [SugarTable("product_info")]
    public class ProductInfoEntity
    {
        [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
        public int Id { get; set; }

        public string Name { get; set; }

        public bool Select { get; set; }
    }
}
