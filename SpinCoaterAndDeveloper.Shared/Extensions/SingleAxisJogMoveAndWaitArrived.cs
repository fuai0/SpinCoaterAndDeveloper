using LogServiceInterface;
using MotionCardServiceInterface;
using MotionControlActuation;
using Prism.Ioc;
using SpinCoaterAndDeveloper.Shared.Models.MotionControlModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SpinCoaterAndDeveloper.Shared.Extensions
{
    public class SingleAxisJogMoveAndWaitArrived
    {
        private readonly IMotionCardService motionCardService;
        private readonly ILogService logService;
        private CancellationTokenSource cancellationTokenSource;

        public SingleAxisJogMoveAndWaitArrived(IContainerProvider containerProvider)
        {
            this.motionCardService = containerProvider.Resolve<IMotionCardService>();
            this.logService = containerProvider.Resolve<ILogService>();
        }
        /// <summary>
        /// 异步启动单轴Jog运动
        /// </summary>
        /// <param name="axisName"></param>
        /// <param name="vel"></param>
        /// <param name="acc"></param>
        /// <param name="dec"></param>
        /// <param name="ioInputName">停止运动输入IO的名字</param>
        /// <param name="dir">方向</param>
        /// <param name="jogArrivedType">停止运动的类型</param>
        /// <param name="timeout">ms</param>
        /// <param name="cancellationTokenSource"></param>
        /// <returns></returns>
        public async Task<bool> StartJogAsync(string axisName, double vel, double acc, double dec, string ioInputName, Direction dir, JogArrivedType jogArrivedType, double timeout, CancellationTokenSource cancellationTokenSource)
        {
            this.cancellationTokenSource = cancellationTokenSource;
            var result = await Task.Run(() =>
            {
                motionCardService.JogMoveStart((short)MotionControlResource.AxisResource[axisName].AxisIdOnCard,
                                               vel * MotionControlResource.AxisResource[axisName].Proportion,
                                               dir,
                                               acc * MotionControlResource.AxisResource[axisName].Proportion,
                                               dec * MotionControlResource.AxisResource[axisName].Proportion);
                DateTime startTime = DateTime.Now;
                while (true)
                {
                    if ((DateTime.Now - startTime).TotalMilliseconds > timeout)
                    {
                        motionCardService.StopMove((short)MotionControlResource.AxisResource[axisName].AxisIdOnCard, 1);
                        logService.WriteLog(LogTypes.DB.ToString(), $@"移动到目标位置超时", MessageDegree.ERROR);
                        return false;
                    }
                    if (MotionControlResource.IOInputResource[ioInputName].Status == (jogArrivedType == JogArrivedType.Input ? true : false))
                    {
                        motionCardService.JogMoveStop((short)MotionControlResource.AxisResource[axisName].AxisIdOnCard);
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
