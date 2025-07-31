using FSM;
using LogServiceInterface;
using MotionControlActuation;
using PrismAutomationPlatformExt;
using SpinCoaterAndDeveloper.Actuation.Actuation;
using SpinCoaterAndDeveloper.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SpinCoaterAndDeveloper.FSM
{
    public static class ActuationExtensions
    {
        /// <summary>
        /// 全局复位指令集清空,并重新初始化与启动
        /// </summary>
        /// <param name="actuationGlobleResetGroup"></param>
        /// <param name="logService"></param>
        /// <returns></returns>
        public static bool GlobleResetActuationResetAndStart(this ActuationGlobleResetGroupManager actuationGlobleResetGroup, ILogService logService)
        {
            //初始化复位指令集
            actuationGlobleResetGroup.Clear();
            actuationGlobleResetGroup.InitActuationCollections();

            actuationGlobleResetGroup.WorkerFinishEvent += ((sts) =>
            {
                switch (sts)
                {
                    case BackgroundWorkerStatus.FinishWithSuccess:
                        logService.WriteLog(LogTypes.DB.ToString(), "复位完成", MessageDegree.INFO);
                        GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.GlobleResetSuccess));
                        break;
                    case BackgroundWorkerStatus.FinishWithFail:
                        logService.WriteLog(LogTypes.DB.ToString(), "复位失败", MessageDegree.INFO);
                        GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.GlobleResetFail));
                        break;
                    case BackgroundWorkerStatus.FinishWithException:
                        logService.WriteLog(LogTypes.DB.ToString(), "复位线程异常结束!", MessageDegree.INFO);
                        GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                        break;
                    case BackgroundWorkerStatus.FinishWithCancel:
                        logService.WriteLog(LogTypes.DB.ToString(), "复位线程被取消!", MessageDegree.INFO);
                        break;
                    default:
                        logService.WriteLog(LogTypes.DB.ToString(), "复位执行出现预期之外状态!", MessageDegree.INFO);
                        break;
                }
            });
            //触发指令执行,默认变量
            actuationGlobleResetGroup.SetVIOSts("MachineResetEnable", true);
            //启动异步线程
            actuationGlobleResetGroup.Start();
            return true;
        }
        /// <summary>
        /// Running指令集清空,并重新初始化与启动
        /// </summary>
        /// <param name="actuationRunningGroup"></param>
        /// <param name="logService"></param>
        /// <returns></returns>
        public static bool RunningActuationResetAndStart(this ActuationRunningGroupManager actuationRunningGroup, ILogService logService)
        {
            //初始化运行指令集合
            actuationRunningGroup.Clear();
            actuationRunningGroup.InitActuationCollections();
            actuationRunningGroup.WorkerFinishEvent += ((sts) =>
            {
                switch (sts)
                {
                    case BackgroundWorkerStatus.None:
                        logService.WriteLog(LogTypes.DB.ToString(), "actionRunningGroup线程以None状态退出", MessageDegree.INFO);
                        break;
                    case BackgroundWorkerStatus.FinishWithException:
                        logService.WriteLog(LogTypes.DB.ToString(), "actionRunningGroup线程异常退出", MessageDegree.FATAL);
                        GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                        break;
                    case BackgroundWorkerStatus.FinishWithSuccess:
                        logService.WriteLog(LogTypes.DB.ToString(), "调试用特殊中断,请急停后复位在使用!", MessageDegree.INFO);
                        break;
                    case BackgroundWorkerStatus.FinishWithCancel:
                        logService.WriteLog(LogTypes.DB.ToString(), "Running线程被取消!", MessageDegree.INFO);
                        break;
                    default:
                        logService.WriteLog(LogTypes.DB.ToString(), "actionRunningGroup预期之外状态出现!", MessageDegree.FATAL);
                        break;
                }
            });

            //触发指令执行,默认变量
            actuationRunningGroup.SetVIOSts("MachineRunningEnable", true);
            //触发指令集执行
            actuationRunningGroup.Start();
            return true;
        }
        /// <summary>
        /// BurnIn指令集清空,并重新初始化与启动
        /// </summary>
        /// <param name="actuationBurnInGroup"></param>
        /// <param name="logService"></param>
        /// <returns></returns>
        public static bool BurnInActuationResetAndStart(this ActuationBurnInGroupManager actuationBurnInGroup, ILogService logService)
        {
            //清空指令集合,虚拟IO集合
            actuationBurnInGroup.Clear();
            actuationBurnInGroup.InitActuationCollections();
            actuationBurnInGroup.WorkerFinishEvent += ((sts) =>
            {
                switch (sts)
                {
                    case BackgroundWorkerStatus.None:
                        logService.WriteLog(LogTypes.DB.ToString(), "actuationBurnInGroup线程以None状态退出", MessageDegree.INFO);
                        break;
                    case BackgroundWorkerStatus.FinishWithException:
                        logService.WriteLog(LogTypes.DB.ToString(), "actuationBurnInGroup线程异常退出", MessageDegree.FATAL);
                        GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                        break;
                    case BackgroundWorkerStatus.FinishWithCancel:
                        logService.WriteLog(LogTypes.DB.ToString(), "空跑线程被取消!", MessageDegree.INFO);
                        break;
                    default:
                        logService.WriteLog(LogTypes.DB.ToString(), $"actuationBurnInGroup预期之外状态出现{sts}!", MessageDegree.FATAL);
                        break;
                }
            });
            actuationBurnInGroup.SetVIOSts("MachineBurnInEnable", true);
            actuationBurnInGroup.Start();
            return true;
        }
    }
}
