using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.Models.MotionControlModels
{
    public class MovementPointInfo
    {
        /// <summary>
        /// 数据库映射Id
        /// </summary>
        public int Id { private get; set; }
        /// <summary>
        /// 运动点位名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 中文名称
        /// </summary>
        public string CNName { private get; set; }
        /// <summary>
        /// 英文名称
        /// </summary>
        public string ENName { private get; set; }
        /// <summary>
        /// 越语名称
        /// </summary>
        public string VNName { private get; set; }
        /// <summary>
        /// 预留名称
        /// </summary>
        public string XXName { private get; set; }
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
        public string Tag { private get; set; }
        /// <summary>
        /// 产品Id
        /// </summary>
        public int ProductId { private get; set; }
        /// <summary>
        /// 点位手动运动时安全配置
        /// </summary>
        public bool ManualMoveSecurityEnable { get; set; }
        /// <summary>
        /// 手动运动到点位时超时时间
        /// </summary>
        public double ManualMoveSecurityTimeOut { private get; set; }
        /// <summary>
        /// 运动点位参与运动的轴集合
        /// </summary>
        public List<MovementPointPosition> MovementPointPositions { get; set; }
        /// <summary>
        /// 点位运动时安全轴(需要带这些轴到位后才能运行)
        /// </summary>
        public List<MovementPointSercurity> MovementPointSecurities { get; set; }

        public int GetId() => Id;
        public string GetCNName() => CNName;
        public string GetENName() => ENName;
        public string GetVNName() => VNName;
        public string GetXXName() => XXName;
        public string GetGroup() => Group;
        public string GetBackup() => Backup;
        public string GetTag() => Tag;
        public int GetProductId() => ProductId;
        public double GetManualMoveSecurityTimeOut() => ManualMoveSecurityTimeOut;
    }
}
