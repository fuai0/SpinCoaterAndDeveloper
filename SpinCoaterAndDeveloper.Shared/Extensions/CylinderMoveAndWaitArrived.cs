using LogServiceInterface;
using MotionCardServiceInterface;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.Extensions
{
    public class CylinderMoveAndWaitArrived
    {
        private readonly IMotionCardService motionCardService;
        private readonly ILogService logService;
        private CancellationTokenSource cancellationTokenSource;

        public CylinderMoveAndWaitArrived(IContainerProvider containerProvider)
        {
            this.motionCardService = containerProvider.Resolve<IMotionCardService>();
            this.logService = containerProvider.Resolve<ILogService>();
        }
        /// <summary>
        /// 设定气缸动作
        /// </summary>
        /// <param name="cylinderName"></param>
        /// <param name="value">True:气缸伸出,到达动点. False:气缸缩回,到达原点</param>
        /// <param name="cancellationTokenSource"></param>
        /// <returns></returns>
        public async Task<bool> StartAsync(string cylinderName, bool value, CancellationTokenSource cancellationTokenSource)
        {
            this.cancellationTokenSource = cancellationTokenSource;
            var result = await Task.Run(() =>
            {
                switch (GlobalValues.CylinderDicCollection[cylinderName].ValveType)
                {
                    case Models.CylinderModels.ValveType.SingleHeader:
                        motionCardService.SetOuputStsEx(GlobalValues.CylinderDicCollection[cylinderName].SingleValveOutputInfo.Name, value);
                        break;
                    case Models.CylinderModels.ValveType.DualHeader:
                        if (value)
                        {
                            //气缸伸出
                            motionCardService.SetOuputStsEx(GlobalValues.CylinderDicCollection[cylinderName].DualValveOriginOutputInfo.Name, true);
                            motionCardService.SetOuputStsEx(GlobalValues.CylinderDicCollection[cylinderName].DualValveMovingOutputInfo.Name, false);
                        }
                        else
                        {
                            //气缸缩回
                            motionCardService.SetOuputStsEx(GlobalValues.CylinderDicCollection[cylinderName].DualValveOriginOutputInfo.Name, false);
                            motionCardService.SetOuputStsEx(GlobalValues.CylinderDicCollection[cylinderName].DualValveMovingOutputInfo.Name, true);
                        }
                        break;
                    default:
                        break;
                }
                DateTime startTime = DateTime.Now;
                while (true)
                {
                    Thread.Sleep(100);
                    //气缸伸出超时,动点超时
                    if (value && (DateTime.Now - startTime).TotalMilliseconds > GlobalValues.CylinderDicCollection[cylinderName].MovingPointTimeout) return false;
                    //气缸缩回超时,原点超时
                    if (!value && (DateTime.Now - startTime).TotalMilliseconds > GlobalValues.CylinderDicCollection[cylinderName].OriginPointTimeout) return false;

                    //取消
                    if (cancellationTokenSource.IsCancellationRequested) return false;
                    //到位
                    switch (GlobalValues.CylinderDicCollection[cylinderName].SensorType)
                    {
                        case Models.CylinderModels.SensorType.None:
                            //气缸无传感器,使用DelayTime.DelayTime需要小于超时时间
                            if ((DateTime.Now - startTime).TotalMilliseconds > GlobalValues.CylinderDicCollection[cylinderName].DelayTime) return true;
                            break;
                        case Models.CylinderModels.SensorType.SingleOrigin:
                            //气缸伸出,只有原点传感器则使用延时时间
                            if (value) if ((DateTime.Now - startTime).TotalMilliseconds > GlobalValues.CylinderDicCollection[cylinderName].DelayTime) return true;
                            //气缸缩回,未屏蔽原点传感器时,判断原点传感器是否亮
                            if (!value && !GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorOriginInput) if (motionCardService.GetInputStsEx(GlobalValues.CylinderDicCollection[cylinderName].SensorOriginInputInfo.Name) == true) return true;
                            //气缸缩回,屏蔽原点传感器时,使用屏蔽原点传感器延时
                            if (!value && GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorOriginInput) if ((DateTime.Now - startTime).TotalMilliseconds > GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorOriginInputDelayTime) return true;
                            break;
                        case Models.CylinderModels.SensorType.SingleMoving:
                            //气缸伸出,未屏蔽动点传感器时,判断动点传感器是否亮
                            if (value && !GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorMovingInput) if (motionCardService.GetInputStsEx(GlobalValues.CylinderDicCollection[cylinderName].SensorMovingInputInfo.Name) == true) return true;
                            //气缸伸出,屏蔽动点传感器时,使用屏蔽动点传感器延时时间
                            if (value && GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorMovingInput) if ((DateTime.Now - startTime).TotalMilliseconds > GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorMovingInputDelayTime) return true;
                            //气缸缩回,只有动点传感器则使用延时时间
                            if (!value) if ((DateTime.Now - startTime).TotalMilliseconds > GlobalValues.CylinderDicCollection[cylinderName].DelayTime) return true;
                            break;
                        case Models.CylinderModels.SensorType.Dual:
                            //气缸伸出,未屏蔽动点传感器时,判断动点传感器是否亮
                            if (value && !GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorMovingInput) if (motionCardService.GetInputStsEx(GlobalValues.CylinderDicCollection[cylinderName].SensorMovingInputInfo.Name) == true) return true;
                            //气缸伸出,屏蔽动点传感器时,使用屏蔽动点传感器延时时间
                            if (value && GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorMovingInput) if ((DateTime.Now - startTime).TotalMilliseconds > GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorMovingInputDelayTime) return true;
                            //气缸缩回,未屏蔽原点传感器时,判断原点传感器是否亮
                            if (!value && !GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorOriginInput) if (motionCardService.GetInputStsEx(GlobalValues.CylinderDicCollection[cylinderName].SensorOriginInputInfo.Name) == true) return true;
                            //气缸缩回,屏蔽原点传感器时,使用屏蔽原点传感器延时时间
                            if (!value && GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorOriginInput) if ((DateTime.Now - startTime).TotalMilliseconds > GlobalValues.CylinderDicCollection[cylinderName].ShiedSensorOriginInputDelayTime) return true;
                            break;
                        default:
                            break;
                    }
                }
            }, cancellationTokenSource.Token);
            return result;
        }

        public void Cancel()
        {
            cancellationTokenSource?.Cancel();
        }
    }
}
