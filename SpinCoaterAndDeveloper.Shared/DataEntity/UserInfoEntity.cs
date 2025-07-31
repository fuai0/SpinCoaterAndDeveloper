using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.DataEntity
{
    [SugarTable("user_info")]
    public class UserInfoEntity
    {
        [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
        public int Id { get; set; }

        [SugarColumn(ColumnDescription = "用户名")]
        public string UserName { get; set; }

        [SugarColumn(ColumnDescription = "用户密码")]
        public string Password { get; set; }

        [SugarColumn(ColumnDescription = "权限:操作员/管理员/开发人员")]
        public string Authority { get; set; }

    }
}
