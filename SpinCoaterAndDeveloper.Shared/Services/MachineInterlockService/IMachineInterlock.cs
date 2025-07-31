using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.Services.MachineInterlockService
{
    public enum InterlockStatus
    {
        Default,
        //连锁锁定中
        Locking,
        //连锁锁定完成
        LockFinish,
        //连锁锁定超时
        LockTimeout,
        //连锁锁定异常
        LockException,
    }
    public interface IMachineInterlock
    {
        InterlockStatus Status { get; }
        bool ForceCloseInterlockCheck { get; }
        void InterlockRecord(double maxWaitTime = 5000);
        (bool outputCheckResult, Dictionary<string, bool> differentOutput, bool axisCheckResult, Dictionary<string, double> differentAxis, Guid guid) InterlockCheck();
        void InterlockStatusReset();
        bool InterlockOneCycleForceColse(Guid guid);
        Guid InterlockRecordTimeoutOrExceptionGuid();
    }
}
