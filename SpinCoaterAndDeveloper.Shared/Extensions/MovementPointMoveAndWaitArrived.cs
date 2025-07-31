using LogServiceInterface;
using MotionCardServiceInterface;
using MotionControlActuation;
using Prism.Ioc;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.Extensions
{
    public class MovementPointMoveAndWaitArrived
    {
        private readonly IMotionCardService motionCardService;
        private readonly ILogService logService;
        private readonly IContainerProvider containerProvider;
        private CancellationTokenSource cancellationTokenSource;
        public MovementPointMoveAndWaitArrived(IContainerProvider containerProvider)
        {
            this.motionCardService = containerProvider.Resolve<IMotionCardService>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.containerProvider = containerProvider;
        }
        /// <summary>
        /// 异步启动运动,点位安全步骤不递归,只执行一层,点位移动速度取运动速度的5%
        /// </summary>
        /// <param name="pointName"></param>
        /// <param name="cancellationTokenSource"></param>
        /// <param name="executeSecuritySteps">是否执行点位中配置的安全动作步骤</param>
        /// <param name="manualSetTimeout">如果不为NaN,则使用手动指定的超时时间,不在使用点位中设置的手动超时时间</param>
        /// <param name="velRatio">启动运动,所有点位速度使用运行速度的5%</param>
        /// <returns></returns>
        public async Task<bool> StartAsync(string pointName, CancellationTokenSource cancellationTokenSource, bool executeSecuritySteps = true, double manualSetTimeout = double.NaN, double velRatio = 0.05)
        {
            this.cancellationTokenSource = cancellationTokenSource;
            var result = await Task.Run(async () =>
            {
                //安全动作执行
                if (executeSecuritySteps)
                {
                    foreach (var item in GlobalValues.MCPointDicCollection[pointName].MovementPointSecurities)
                    {
                        switch (item.GetSecurityTypes())
                        {
                            case Models.MovementPointSecurityGraphModels.MovementPointSecurityTypes.MovementPoint:
                                //点位超时时间使用点位设置中的手动移动超时时间
                                if (MCPointMoveAndWaitArrived(item.GetName(), GlobalValues.MCPointDicCollection[item.GetName()].GetManualMoveSecurityTimeOut(), velRatio) != true) return false;
                                break;
                            case Models.MovementPointSecurityGraphModels.MovementPointSecurityTypes.Cylinder:
                                //气缸
                                CylinderMoveAndWaitArrived cyliderMoveAndWaitArrived = new CylinderMoveAndWaitArrived(containerProvider);
                                var cylinderResult = await cyliderMoveAndWaitArrived.StartAsync(item.GetName(), item.GetBoolSecurityTypeValue(), cancellationTokenSource);
                                if (cylinderResult == false) return false;
                                break;
                            case Models.MovementPointSecurityGraphModels.MovementPointSecurityTypes.IOOutput:
                                //IO输出发送命令后等待设定的延时时间到达则认为动作完成
                                motionCardService.SetOuputStsEx(item.GetName(), item.GetBoolSecurityTypeValue());
                                await Task.Delay(item.GetIOOutputSecurityTypeDelayValue(), cancellationTokenSource.Token);
                                break;
                            case Models.MovementPointSecurityGraphModels.MovementPointSecurityTypes.AxisSafePoint:
                                //轴移动到安全位,安全位设定在轴表中.速度等使用回原相关速度
                                SingleAxisAbsMoveAndWaitArrived safeAxis = new SingleAxisAbsMoveAndWaitArrived(containerProvider);
                                if (MotionControlResource.AxisResource[item.GetName()].GetSafeAxisEnable() != true)
                                {
                                    logService.WriteLog(LogTypes.DB.ToString(), $@"轴{item.GetName()}不是安全轴!", MessageDegree.ERROR);
                                    return false;
                                }
                                var axisResult = await safeAxis.StartAbsAsync(item.GetName(),
                                                                              MotionControlResource.AxisResource[item.GetName()].GetHomeHighVel(),
                                                                              MotionControlResource.AxisResource[item.GetName()].GetHomeAcc(),
                                                                              MotionControlResource.AxisResource[item.GetName()].GetHomeAcc(),
                                                                              MotionControlResource.AxisResource[item.GetName()].GetSafeAxisPosition(),
                                                                              MotionControlResource.AxisResource[item.GetName()].GetHomeTimeout(),
                                                                              cancellationTokenSource);
                                if (axisResult == false) return false;
                                break;
                            case Models.MovementPointSecurityGraphModels.MovementPointSecurityTypes.AxisOriginal:
                                //轴移动到原点,速度等使用回原相关速度
                                SingleAxisAbsMoveAndWaitArrived orignalAxis = new SingleAxisAbsMoveAndWaitArrived(containerProvider);
                                var axisOrgResult = await orignalAxis.StartAbsAsync(item.GetName(),
                                                                                    MotionControlResource.AxisResource[item.GetName()].GetHomeHighVel(),
                                                                                    MotionControlResource.AxisResource[item.GetName()].GetHomeAcc(),
                                                                                    MotionControlResource.AxisResource[item.GetName()].GetHomeAcc(),
                                                                                    0,
                                                                                    MotionControlResource.AxisResource[item.GetName()].GetHomeTimeout(),
                                                                                    cancellationTokenSource);
                                if (axisOrgResult == false) return false;
                                break;
                            default:
                                break;
                        }
                    }
                }
                return MCPointMoveAndWaitArrived(pointName, double.IsNaN(manualSetTimeout) ? GlobalValues.MCPointDicCollection[pointName].GetManualMoveSecurityTimeOut() : manualSetTimeout, velRatio);
            }, cancellationTokenSource.Token);
            return result;
        }
        public void Cancel()
        {
            cancellationTokenSource?.Cancel();
        }

        private bool MCPointMoveAndWaitArrived(string pointName, double timeout, double velRatio)
        {
            foreach (var item in GlobalValues.MCPointDicCollection[pointName].MovementPointPositions)
            {
                //检查Jog运动是否设定到位IO
                if (item.GetMovementPointType() == Models.MotionControlModels.MovementType.Jog && item.GetJogIOInputId() == 0)
                {
                    GlobalValues.MCPointDicCollection[pointName].MovementPointPositions.ForEach(x => motionCardService.StopMove((short)x.AxisInfo.AxisIdOnCard, 1));
                    logService.WriteLog(LogTypes.DB.ToString(), $@"未设定Jog停止点位,移动到点位{pointName}失败", MessageDegree.ERROR);
                    return false;
                }
                //开环步进不能下使能,下使能之后轴发生位移位置会丢失.开环步进下使能之后必须回原才能手动运动.若要下使能清除错误,需手动在外部控制
                //motionCardService.AxisServo((short)item.AxisInfo.AxisIdOnCard, false);
                //motionCardService.ClearAxSts((short)item.AxisInfo.AxisIdOnCard);
                //motionCardService.AxisServo((short)item.AxisInfo.AxisIdOnCard, true);

                switch (item.GetMovementPointType())
                {
                    case Models.MotionControlModels.MovementType.Abs:
                        logService.WriteLog(LogTypes.DB.ToString(), $@"点位{pointName}参与轴{item.AxisInfo.Name}启动运行,运动类型为{item.GetMovementPointType()},目标位置为{item.AbsValue}.", MessageDegree.INFO);
                        motionCardService.StartMoveAbs((short)item.AxisInfo.AxisIdOnCard,
                                                       0,
                                                       0,
                                                       item.Vel * item.AxisInfo.Proportion * velRatio,
                                                       item.Acc * item.AxisInfo.Proportion,
                                                       item.Dec * item.AxisInfo.Proportion,
                                                       (item.AbsValue + item.Offset) * item.AxisInfo.Proportion);
                        break;
                    case Models.MotionControlModels.MovementType.Rel:
                        logService.WriteLog(LogTypes.DB.ToString(), $@"点位{pointName}参与轴{item.AxisInfo.Name}启动运行,运动类型为{item.GetMovementPointType()},目标位置为{item.RelValue}.", MessageDegree.INFO);
                        motionCardService.StartMoveRel((short)item.AxisInfo.AxisIdOnCard,
                                                       0,
                                                       0,
                                                       item.Vel * item.AxisInfo.Proportion * velRatio,
                                                       item.Acc * item.AxisInfo.Proportion,
                                                       item.Dec * item.AxisInfo.Proportion,
                                                       (item.RelValue + item.Offset) * item.AxisInfo.Proportion);
                        break;
                    case Models.MotionControlModels.MovementType.Jog:
                        logService.WriteLog(LogTypes.DB.ToString(), $@"点位{pointName}参与轴{item.AxisInfo.Name}启动运行,运动类型为{item.GetMovementPointType()},停止条件为{item.JogIOInputInfo.Name},{item.JogArrivedCondition}.", MessageDegree.INFO);
                        motionCardService.JogMoveStart((short)item.AxisInfo.AxisIdOnCard,
                                                       item.Vel * item.AxisInfo.Proportion * velRatio,
                                                       item.JogDirection,
                                                       item.Acc * item.AxisInfo.Proportion,
                                                       item.Dec * item.AxisInfo.Proportion);
                        break;
                    default:
                        break;
                }
            }
            //等待到位
            DateTime startTime = DateTime.Now;
            while (true)
            {
                //超时判断
                if ((DateTime.Now - startTime).TotalMilliseconds > timeout)
                {
                    //超时,停止所有轴运动
                    foreach (var item in GlobalValues.MCPointDicCollection[pointName].MovementPointPositions)
                    {
                        motionCardService.StopMove((short)item.AxisInfo.AxisIdOnCard, 1);
                    }
                    logService.WriteLog(LogTypes.DB.ToString(), $@"移动到点位{pointName}超时", MessageDegree.ERROR);
                    return false;
                }
#if DEBUG
                if (GlobalValues.MCPointDicCollection[pointName].MovementPointPositions.Count == 0)
                    throw new Exception($"点位{pointName}中未设定轴");
#endif
                //到位判断
                bool temp = true;
                foreach (var item in GlobalValues.MCPointDicCollection[pointName].MovementPointPositions)
                {
                    switch (item.GetMovementPointType())
                    {
                        case Models.MotionControlModels.MovementType.Abs:
                        case Models.MotionControlModels.MovementType.Rel:
                            temp &= MotionControlResourceExtensions.RealTimeCheckAxisArrivedEx(motionCardService, item.AxisInfo.Name);
                            break;
                        case Models.MotionControlModels.MovementType.Jog:
                            if (motionCardService.GetInputStsEx(item.JogIOInputInfo.Name) == (item.JogArrivedCondition == Models.MotionControlModels.JogArrivedType.Input ? true : false))
                            {
                                motionCardService.JogMoveStop((short)item.AxisInfo.AxisIdOnCard);
                                temp &= true;
                            }
                            else
                                temp &= false;
                            break;
                        default:
                            break;
                    }
                }
                if (temp) break;
                //取消运动判断
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    //取消,停止所有轴运动
                    foreach (var item in GlobalValues.MCPointDicCollection[pointName].MovementPointPositions)
                    {
                        motionCardService.StopMove((short)item.AxisInfo.AxisIdOnCard, 1);
                    }
                    return false;
                }
                Thread.Sleep(100);
            }
            return true;
        }
    }
}
