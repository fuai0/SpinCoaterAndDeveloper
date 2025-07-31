using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.Services.MouseIdelTimeService
{
    public interface IMouseIdleTime
    {
        double GetMouseIdleTimeSecondes();
        void ResetIdleTime();
    }
}
