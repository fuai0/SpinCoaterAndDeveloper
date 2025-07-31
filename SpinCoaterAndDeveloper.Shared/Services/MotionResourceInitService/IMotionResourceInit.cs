using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.Services.MotionResourceInitService
{
    public interface IMotionResourceInit
    {
        void InitAxisResourceDicCollection();
        void InitIOInputResourceDicCollection();
        void InitIOOutputResourceDicCollection();
        void InitMCParmeterDicCollection();
        void InitMCPointDicCollection();
        void InitInterpolationPaths();
        void InitCylinderDicCollection();
        void InitFunctionShieldDicCollection();
    }
}
