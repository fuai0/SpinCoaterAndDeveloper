using LogServiceInterface;
using MotionCardServiceInterface;
using MotionControlActuation;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.Extensions
{
    public class SingleAxisAbsMoveAndWaitArrived
    {
        private readonly IMotionCardService motionCardService;
        private readonly ILogService logService;
        private CancellationTokenSource cancellationTokenSource;

        public SingleAxisAbsMoveAndWaitArrived(IContainerProvider containerProvider)
        {
            this.motionCardService = containerProvider.Resolve<IMotionCardService>();
            this.logService = containerProvider.Resolve<ILogService>();
        }
        /// <summary>
        /// 异步启动单轴绝对运动
        /// </summary>
        /// <param name="axisName"></param>
        /// <param name="vel"></param>
        /// <param name="acc"></param>
        /// <param name="dec"></param>
        /// <param name="absValue">mm</param>
        /// <param name="timeout">ms</param>
        /// <param name="cancellationTokenSource"></param>
        /// <returns></returns>
        public async Task<bool> StartAbsAsync(string axisName, double vel, double acc, double dec, double absValue, double timeout, CancellationTokenSource cancellationTokenSource)
        {
            this.cancellationTokenSource = cancellationTokenSource;
            var result = await Task.Run(() =>
            {
                motionCardService.StartMoveAbs((short)MotionControlResource.AxisResource[axisName].AxisIdOnCard,
                                               0,
                                               0,
                                               vel * MotionControlResource.AxisResource[axisName].Proportion,
                                               acc * MotionControlResource.AxisResource[axisName].Proportion,
                                               dec * MotionControlResource.AxisResource[axisName].Proportion,
                                               absValue * MotionControlResource.AxisResource[axisName].Proportion);
                DateTime startTime = DateTime.Now;
                while (true)
                {
                    if ((DateTime.Now - startTime).TotalMilliseconds > timeout)
                    {
                        motionCardService.StopMove((short)MotionControlResource.AxisResource[axisName].AxisIdOnCard, 1);
                        logService.WriteLog(LogTypes.DB.ToString(), $@"移动到目标位置超时", MessageDegree.ERROR);
                        return false;
                    }
                    if (motionCardService.RealTimeCheckAxisArrivedEx(axisName))
                    {
                        break;
                    }
                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        motionCardService.StopMove((short)MotionControlResource.AxisResource[axisName].AxisIdOnCard, 1);
                        return false;
                    }
                    Thread.Sleep(100);
                }
                return true;
            }, cancellationTokenSource.Token);
            return result;
        }

        public void Cancel()
        {
            cancellationTokenSource?.Cancel();
        }
    }
}
