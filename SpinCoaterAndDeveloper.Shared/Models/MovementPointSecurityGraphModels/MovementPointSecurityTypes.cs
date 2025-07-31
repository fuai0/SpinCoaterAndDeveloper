using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.Models.MovementPointSecurityGraphModels
{
    public enum MovementPointSecurityTypes
    {
        [Description("运动点位")]
        MovementPoint,
        [Description("气缸")]
        Cylinder,
        [Description("IO输出点位")]
        IOOutput,
        [Description("轴安全位")]
        AxisSafePoint,
        [Description("轴原点")]
        AxisOriginal,
    }
}
