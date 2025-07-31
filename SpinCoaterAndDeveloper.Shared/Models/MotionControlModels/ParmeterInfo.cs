using SpinCoaterAndDeveloper.Shared.DataEntity;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.Models.MotionControlModels
{
    public class ParmeterInfo
    {
        /// <summary>
        /// 数据库映射ID,主键
        /// </summary>
        public int Id { private get; set; }
        /// <summary>
        /// 编号
        /// </summary>
        public string Number { get; set; }
        /// <summary>
        /// 参数名称,程序用
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 中文名称,显示用
        /// </summary>
        public string CNName { private get; set; }
        /// <summary>
        /// 英文名称,显示用
        /// </summary>
        public string ENName { private get; set; }
        /// <summary>
        /// 越语名称,先使用
        /// </summary>
        public string VNName { private get; set; }
        /// <summary>
        /// 预留名称
        /// </summary>
        public string XXName { private get; set; }
        /// <summary>
        /// 数值
        /// </summary>
        public string Data { get; set; }
        /// <summary>
        /// 数据类型,用于数据校验
        /// </summary>
        public ParmeterType DataType { get; set; }
        /// <summary>
        /// 数据单位
        /// </summary>
        public string Unit { get; set; }
        /// <summary>
        /// 分组
        /// </summary>
        public string Group { private get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Backup { private get; set; }
        /// <summary>
        /// Tag
        /// </summary>
        public string Tag { get; set; }
        /// <summary>
        /// 关联的产品ID
        /// </summary>
        public int ProductId { private get; set; }

        public int GetId() => Id;
        public string GetCNName() => CNName;
        public string GetENName() => ENName;
        public string GetVNName() => VNName;
        public string GetXXName() => XXName;
        public string GetGroup() => Group;
        public string GetBackup() => Backup;
        public int GetProductId() => ProductId;
    }
}
