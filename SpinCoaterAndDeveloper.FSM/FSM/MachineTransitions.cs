using DryIoc;
using FSM;
using LogServiceInterface;
using MotionCardServiceInterface;
using Prism.Events;
using Prism.Ioc;
using Prism.Services.Dialogs;
using PrismAutomationPlatformExt;
using SpinCoaterAndDeveloper.Actuation.Actuation;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.Event;
using SpinCoaterAndDeveloper.Shared.Extensions;
using SpinCoaterAndDeveloper.Shared.Services.MachineInterlockService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SpinCoaterAndDeveloper.FSM.FSM
{
    public class ResetTransition : FSMTransition
    {
        private readonly ILogService logService;
        private readonly IDialogService dialogService;
        private readonly IMotionCardService motionCardService;
        private readonly ActuationGlobleResetGroupManager actuationGlobleResetGroup = null;
        private readonly ActuationRunningGroupManager actuationRunningGroup = null;
        private readonly ActuationBurnInGroupManager actuationBurnInGroupManager = null;
        public ResetTransition(IContainerProvider containerProvider, FSMState currentState, FSMState nexeState) : base(FSMEventCode.Reset, currentState, nexeState)
        {
            this.logService = containerProvider.Resolve<ILogService>();
            this.dialogService = containerProvider.Resolve<IDialogService>();
            this.motionCardService = containerProvider.Resolve<IMotionCardService>();
            this.actuationGlobleResetGroup = containerProvider.Resolve<ActuationGlobleResetGroupManager>();
            this.actuationRunningGroup = containerProvider.Resolve<ActuationRunningGroupManager>();
            this.actuationBurnInGroupManager = containerProvider.Resolve<ActuationBurnInGroupManager>();
        }

        protected override bool DoExecute(FSMEvent fsmEvent)
        {
            logService.WriteLog(LogTypes.DB.ToString(), "复位按钮被按下", MessageDegree.INFO);
            switch (currentState.GetStateCode())
            {
                case FSMStateCode.PowerUpping:
                    logService.WriteLog(LogTypes.DB.ToString(), "上电复位", MessageDegree.INFO);
                    return PowerUppingToGlobleReseting();
                case FSMStateCode.Pausing:
                    logService.WriteLog(LogTypes.DB.ToString(), "暂停复位", MessageDegree.INFO);
                    return PausingToGlobleReseting();
                case FSMStateCode.EmergencyStopping:
                    logService.WriteLog(LogTypes.DB.ToString(), "急停复位", MessageDegree.INFO);
                    return EMStopToGlobleReseting();
                case FSMStateCode.Alarming:
                    logService.WriteLog(LogTypes.DB.ToString(), "报警复位", MessageDegree.INFO);
                    return AlarmingToGlobleReseting();
                case FSMStateCode.BurnInPausing:
                    logService.WriteLog(LogTypes.DB.ToString(), $"空跑暂停复位", MessageDegree.INFO);
                    return BurnInPausingToGlobleReseting();
                case FSMStateCode.BurnInAlarming:
                    logService.WriteLog(LogTypes.DB.ToString(), $"空跑报警复位", MessageDegree.INFO);
                    return BurnInAlarmingToGlobleReseting();
                default:
                    break;
            }
            return false;
        }
        private bool BurnInAlarmingToGlobleReseting()
        {
            try
            {
                //停止BurnInTesting指令集
                if (!actuationBurnInGroupManager.Stop())
                {
                    logService.WriteLog(LogTypes.DB.ToString(), "空跑报警->全局复位,BurnInTesting指令组未能如期结束,请检查程序,发送急停事件", MessageDegree.FATAL);
                    motionCardService.EmgStop();
                    GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                    return false;
                }
                actuationBurnInGroupManager.Clear();
                return actuationGlobleResetGroup.GlobleResetActuationResetAndStart(logService);
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"空跑报警->全局复位,切换发生异常:{ex.Message},请检查程序,发送急停事件", ex);
                motionCardService.EmgStop();
                GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                return false;
            }
        }
        private bool BurnInPausingToGlobleReseting()
        {
            try
            {
                //停止BurnInTesting指令集
                if (!actuationBurnInGroupManager.Stop())
                {
                    logService.WriteLog(LogTypes.DB.ToString(), "空跑暂停->全局复位,BurnInTesting指令组未能如期结束,请检查程序,发送急停事件", MessageDegree.FATAL);
                    motionCardService.EmgStop();
                    GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                    return false;
                }
                actuationBurnInGroupManager.Clear();
                return actuationGlobleResetGroup.GlobleResetActuationResetAndStart(logService);
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"空跑暂停->全局复位,切换发生异常:{ex.Message},请检查程序,发送急停事件", ex);
                motionCardService.EmgStop();
                GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                return false;
            }
        }
        private bool PowerUppingToGlobleReseting()
        {
            try
            {
                return actuationGlobleResetGroup.GlobleResetActuationResetAndStart(logService);
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"上电状态->全局复位,切换发生异常:{ex.Message},请检查程序,发送急停事件", ex);
                motionCardService.EmgStop();
                GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                return false;
            }
        }
        private bool PausingToGlobleReseting()
        {
            try
            {
                //停止Running指令集
                if (!actuationRunningGroup.Stop())
                {
                    logService.WriteLog(LogTypes.DB.ToString(), "暂停->全局复位,Running指令组未能如期结束,请检查程序,发送急停事件", MessageDegree.FATAL);
                    motionCardService.EmgStop();
                    GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                    return false;
                }
                actuationRunningGroup.Clear();
                GlobalValues.InterlockPauseWithAlarm = false;
                return actuationGlobleResetGroup.GlobleResetActuationResetAndStart(logService);
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"暂停->全局复位,切换发生异常:{ex.Message},请检查程序,发送急停事件", ex);
                motionCardService.EmgStop();
                GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                return false;
            }
        }
        private bool EMStopToGlobleReseting()
        {
            try
            {
                motionCardService.EmgStopCancel();
                return actuationGlobleResetGroup.GlobleResetActuationResetAndStart(logService);
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"急停->全局复位,切换发生异常:{ex.Message},请检查程序,发送急停事件", ex);
                motionCardService.EmgStop();
                GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                return false;
            }
        }
        private bool AlarmingToGlobleReseting()
        {
            try
            {
                //停止Running指令集
                if (!actuationRunningGroup.Stop())
                {
                    logService.WriteLog(LogTypes.DB.ToString(), "报警->全局复位,Running指令组未能如期结束,请检查程序,发送急停事件", MessageDegree.FATAL);
                    motionCardService.EmgStop();
                    GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                    return false;
                }
                actuationRunningGroup.Clear();

                return actuationGlobleResetGroup.GlobleResetActuationResetAndStart(logService);
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"报警->全局复位,切换发生异常:{ex.Message},请检查程序,发送急停事件", ex);
                motionCardService.EmgStop();
                GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                return false;
            }
        }
    }
    public class GlobleResetSuccessTransition : FSMTransition
    {
        private readonly ILogService logService;
        private readonly IMotionCardService motionCardService;
        private readonly ActuationGlobleResetGroupManager actuationGlobleResetGroup = null;
        public GlobleResetSuccessTransition(IContainerProvider containerProvider, FSMState currentState, FSMState nexeState) : base(FSMEventCode.GlobleResetSuccess, currentState, nexeState)
        {
            this.logService = containerProvider.Resolve<ILogService>();
            this.motionCardService = containerProvider.Resolve<IMotionCardService>();
            this.actuationGlobleResetGroup = containerProvider.Resolve<ActuationGlobleResetGroupManager>();
        }
        protected override bool DoExecute(FSMEvent fsmEvent)
        {
            logService.WriteLog(LogTypes.DB.ToString(), "复位完成,切换到待机", MessageDegree.INFO);
            return GlobleResettingToIdling();
        }
        private bool GlobleResettingToIdling()
        {
            try
            {
                if (!actuationGlobleResetGroup.Stop())
                {
                    logService.WriteLog(LogTypes.DB.ToString(), "全局复位->待机,复位指令组未能如期结束,请检查程序,发送急停事件", MessageDegree.FATAL);
                    motionCardService.EmgStop();
                    GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                    return false;
                }
                actuationGlobleResetGroup.Clear();
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"全局复位->待机,切换发生异常:{ex.Message},请检查程序,发送急停事件", ex);
                motionCardService.EmgStop();
                GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                return false;
            }
        }
    }
    public class GlobleResetFailTransition : FSMTransition
    {
        private readonly ILogService logService;
        private readonly IMotionCardService motionCardService;
        private readonly IDialogWithNoParentService dialogWithNoParentService;
        private readonly ActuationGlobleResetGroupManager actuationGlobleResetGroup = null;
        public GlobleResetFailTransition(IContainerProvider containerProvider, FSMState currentState, FSMState nexeState) : base(FSMEventCode.GlobleResetFail, currentState, nexeState)
        {
            this.logService = containerProvider.Resolve<ILogService>();
            this.motionCardService = containerProvider.Resolve<IMotionCardService>();
            this.dialogWithNoParentService = containerProvider.Resolve<IDialogWithNoParentService>();
            this.actuationGlobleResetGroup = containerProvider.Resolve<ActuationGlobleResetGroupManager>();
        }
        protected override bool DoExecute(FSMEvent fsmEvent)
        {
            logService.WriteLog(LogTypes.DB.ToString(), "复位失败,切换到EMStopping", MessageDegree.INFO);
            return GlobleResettingToEMStopping();
        }
        private bool GlobleResettingToEMStopping()
        {
            try
            {
                motionCardService.EmgStop();
                if (!actuationGlobleResetGroup.Stop())
                {
                    logService.WriteLog(LogTypes.DB.ToString(), "全局复位->急停(ResetFailTransition),复位指令组未能如期结束,请检查程序并重启程序", MessageDegree.FATAL);
                    dialogWithNoParentService.AppAlert("严重错误", "程序状态与设备状态不一致(全局复位->急停(ResetFailTransition)),请联系开发人员!!!", SysDialogLevel.Error, "", "确定");
                    return false;
                }
                actuationGlobleResetGroup.Clear();
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"全局复位->急停(ResetFailTransition),切换发生异常:{ex.Message},请检查程序并重启程序", ex);
                return false;
            }
        }
    }
    public class StartUpTransition : FSMTransition
    {
        private readonly ILogService logService;
        private readonly ActuationGlobleResetGroupManager actuationGlobleResetGroup = null;
        private readonly ActuationRunningGroupManager actuationRunningGroup = null;
        private readonly ActuationBurnInGroupManager actuationBurnInGroup = null;
        private readonly IMotionCardService motionCardService = null;
        private readonly IMachineInterlock machineInterlock;
        private readonly IDialogWithNoParentService dialogWithNoParentService;
        private readonly IEventAggregator eventAggregator;
        public StartUpTransition(IContainerProvider containerProvider, FSMState currentStste, FSMState nexeState) : base(FSMEventCode.StartUp, currentStste, nexeState)
        {
            this.logService = containerProvider.Resolve<ILogService>();
            this.actuationGlobleResetGroup = containerProvider.Resolve<ActuationGlobleResetGroupManager>();
            this.actuationRunningGroup = containerProvider.Resolve<ActuationRunningGroupManager>();
            this.actuationBurnInGroup = containerProvider.Resolve<ActuationBurnInGroupManager>();
            this.motionCardService = containerProvider.Resolve<IMotionCardService>();
            this.machineInterlock = containerProvider.Resolve<IMachineInterlock>();
            this.dialogWithNoParentService = containerProvider.Resolve<IDialogWithNoParentService>();
            this.eventAggregator = containerProvider.Resolve<IEventAggregator>();
        }
        protected override bool DoExecute(FSMEvent fsmEvent)
        {
            logService.WriteLog(LogTypes.DB.ToString(), "启动按钮被按下", MessageDegree.INFO);
            switch (currentState.GetStateCode())
            {
                case FSMStateCode.Idling:
                    return IdlingToRunning();
                case FSMStateCode.Pausing:
                    return PausingToRunning();
                case FSMStateCode.Alarming:
                    return AlarmingToRunning();
                case FSMStateCode.BurnInAlarming:
                    return BurnInAlarmingToBurnInTesting();
                case FSMStateCode.BurnInPausing:
                    return BurnInPausingToBurnInTesting();
                default:
                    break;
            }
            return false;
        }
        private bool BurnInPausingToBurnInTesting()
        {
            try
            {
                actuationBurnInGroup.ProcessResume();
                //切换到主页面
                eventAggregator.GetEvent<MessageEvent>().Publish(new MessageModel() { Message = "ChangeToHomeView", Filter = "ChangeToHomeView" });
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"空跑暂停->空跑运行,切换发生异常:{ex.Message},请检查程序,发送急停事件", ex);
                motionCardService.EmgStop();
                GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                return false;
            }
        }
        private bool BurnInAlarmingToBurnInTesting()
        {
            try
            {
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"空跑报警->空跑运行,切换发生异常:{ex.Message},请检查程序,发送急停事件", ex);
                motionCardService.EmgStop();
                GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                return false;
            }
        }
        private bool IdlingToRunning()
        {
            try
            {
                if (actuationRunningGroup.RunningActuationResetAndStart(logService))
                {
                    //切换到主页面
                    eventAggregator.GetEvent<MessageEvent>().Publish(new MessageModel() { Message = "ChangeToHomeView", Filter = "ChangeToHomeView" });
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"待机->运行,切换发生异常:{ex.Message},请检查程序,发送急停事件", ex);
                motionCardService.EmgStop();
                GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                return false;
            }
        }
        private bool PausingToRunning()
        {
            try
            {
                if (!machineInterlock.ForceCloseInterlockCheck)
                {
                    //检查联锁是否完成
                    switch (machineInterlock.Status)
                    {
                        case InterlockStatus.Default:
                            logService.WriteLog(LogTypes.DB.ToString(), $@"未有联锁记录,请联系开发者检查程序", MessageDegree.FATAL);
                            dialogWithNoParentService.QuestionShowWithNoParent("错误", "未有联锁记录,请联系开发者检查程序", "", "确定");
                            GlobalValues.InterlockPauseWithAlarm = true;
                            return false;
                        case InterlockStatus.Locking:
                            logService.WriteLog(LogTypes.DB.ToString(), $@"设备未完全停止,联锁记录等待中,请等待设备停止.", MessageDegree.INFO);
                            dialogWithNoParentService.QuestionShowWithNoParent("警告", "设备未完全停止,联锁记录等待中,请等待设备停止.点击确定关闭窗口.", "", "确定");
                            return false;
                        case InterlockStatus.LockFinish:
                            (bool outputCheckResult, Dictionary<string, bool> differentOutput, bool axisCheckResult, Dictionary<string, double> differentAxis, Guid guid) = machineInterlock.InterlockCheck();
                            if (outputCheckResult && axisCheckResult)
                            {
                                logService.WriteLog(LogTypes.DB.ToString(), $@"联锁检查通过", MessageDegree.INFO);
                                break;
                            }
                            else
                            {
                                logService.WriteLog(LogTypes.DB.ToString(), $@"联锁检查失败,无法启动设备", MessageDegree.ERROR);
                                DialogParameters parsLockFinish = new DialogParameters();
                                parsLockFinish.Add("Title", "警告".TryFindResourceEx());
                                parsLockFinish.Add("Content", "联锁检查失败!点击确定关闭窗口.".TryFindResourceEx());
                                parsLockFinish.Add("DifferentOutput", differentOutput);
                                parsLockFinish.Add("DifferentAxis", differentAxis);
                                parsLockFinish.Add("Guid", guid);
                                Application.Current.Dispatcher.Invoke(() => { dialogWithNoParentService.Show("InterlockDialogMessageView", parsLockFinish, r => { }); });
                                GlobalValues.InterlockPauseWithAlarm = true;
                                return false;
                            }
                        case InterlockStatus.LockTimeout:
                            logService.WriteLog(LogTypes.DB.ToString(), $@"联锁记录超时.", MessageDegree.ERROR);
                            DialogParameters parLockTimeout = new DialogParameters();
                            parLockTimeout.Add("Title", "警告".TryFindResourceEx());
                            parLockTimeout.Add("Content", "联锁记录超时,请确认设备是否停止及联锁记录时间是否合理.点击确定关闭窗口.".TryFindResourceEx());
                            parLockTimeout.Add("DifferentOutput", new Dictionary<string, bool>());
                            parLockTimeout.Add("DifferentAxis", new Dictionary<string, double>());
                            parLockTimeout.Add("Guid", machineInterlock.InterlockRecordTimeoutOrExceptionGuid());
                            Application.Current.Dispatcher.Invoke(() => { dialogWithNoParentService.Show("InterlockDialogMessageView", parLockTimeout, r => { }); });
                            GlobalValues.InterlockPauseWithAlarm = true;
                            return false;
                        case InterlockStatus.LockException:
                            logService.WriteLog(LogTypes.DB.ToString(), $@"联锁记录异常.", MessageDegree.FATAL);
                            DialogParameters parLockException = new DialogParameters();
                            parLockException.Add("Title", "警告".TryFindResourceEx());
                            parLockException.Add("Content", "联锁记录异常,请确认设备是否停止.点击确定关闭窗口.".TryFindResourceEx());
                            parLockException.Add("DifferentOutput", new Dictionary<string, bool>());
                            parLockException.Add("DifferentAxis", new Dictionary<string, double>());
                            parLockException.Add("Guid", machineInterlock.InterlockRecordTimeoutOrExceptionGuid());
                            Application.Current.Dispatcher.Invoke(() => { dialogWithNoParentService.Show("InterlockDialogMessageView", parLockException, r => { }); });
                            GlobalValues.InterlockPauseWithAlarm = true;
                            return false;
                        default:
                            break;
                    }
                }

                actuationRunningGroup.ProcessResume();
                //复位联锁中的状态
                machineInterlock.InterlockStatusReset();
                GlobalValues.InterlockPauseWithAlarm = false;
                //切换到主页面
                eventAggregator.GetEvent<MessageEvent>().Publish(new MessageModel() { Message = "ChangeToHomeView", Filter = "ChangeToHomeView" });
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"暂停->运行,切换发生异常:{ex.Message},请检查程序,发送急停事件", ex);
                motionCardService.EmgStop();
                GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                return false;
            }
        }
        private bool AlarmingToRunning()
        {
            try
            {
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"报警->运行,切换发生异常:{ex.Message},请检查程序,发送急停事件", ex);
                motionCardService.EmgStop();
                GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                return false;
            }
        }
    }
    public class EmergencyStopTransition : FSMTransition
    {
        private readonly ILogService logService;
        private readonly IMotionCardService motionCardService;
        private readonly IEventAggregator eventAggregator;
        private readonly IDialogWithNoParentService dialogWithNoParentService;
        private readonly ActuationGlobleResetGroupManager actuationGlobleResetGroup = null;
        private readonly ActuationRunningGroupManager actuationRunningGroup = null;
        private readonly ActuationBurnInGroupManager actuationBurnInGroup = null;
        public EmergencyStopTransition(IContainerProvider containerProvider, FSMState currentStste, FSMState nexeState) : base(FSMEventCode.EmergencyStop, currentStste, nexeState)
        {
            this.logService = containerProvider.Resolve<ILogService>();
            this.motionCardService = containerProvider.Resolve<IMotionCardService>();
            this.eventAggregator = containerProvider.Resolve<EventAggregator>();
            this.dialogWithNoParentService = containerProvider.Resolve<IDialogWithNoParentService>();
            this.actuationGlobleResetGroup = containerProvider.Resolve<ActuationGlobleResetGroupManager>();
            this.actuationRunningGroup = containerProvider.Resolve<ActuationRunningGroupManager>();
            this.actuationBurnInGroup = containerProvider.Resolve<ActuationBurnInGroupManager>();
        }
        protected override bool DoExecute(FSMEvent fsmEvent)
        {
            logService.WriteLog(LogTypes.DB.ToString(), "急停按钮按下", MessageDegree.INFO);
            //切换到主页面
            eventAggregator.GetEvent<MessageEvent>().Publish(new MessageModel() { Message = "ChangeToHomeView", Filter = "ChangeToHomeView" });

            switch (currentState.GetStateCode())
            {
                case FSMStateCode.GlobleResetting:
                    return GlobalResettingToEMStop();
                case FSMStateCode.Idling:
                    return IdlingToEMStop();
                case FSMStateCode.Running:
                    return RunningToEMStop();
                case FSMStateCode.Pausing:
                    return PausingToEMStop();
                case FSMStateCode.BurnInTesting:
                    return BurnInTestingToEMStop();
                case FSMStateCode.Alarming:
                    return AlarmingToEMStop();
                case FSMStateCode.PowerUpping:
                    return PowerUppingToEMStop();
                case FSMStateCode.BurnInAlarming:
                    return BurnInAlarmingToEMStop();
                case FSMStateCode.BurnInPausing:
                    return BurnInPauseingToEMStop();
                default:
                    break;
            }
            return false;
        }
        private bool BurnInPauseingToEMStop()
        {
            try
            {
                motionCardService.EmgStop();
                if (!actuationBurnInGroup.Stop())
                {
                    logService.WriteLog(LogTypes.DB.ToString(), $"空跑暂停->急停, 空跑指令组未能如期结束,请检查程序并重启程序", MessageDegree.FATAL);
                    dialogWithNoParentService.AppAlert("严重错误", "程序状态与设备状态不一致(空跑暂停->急停),请联系开发人员!!!", SysDialogLevel.Error, "", "确定");
                    return false;
                }
                actuationBurnInGroup.Clear();
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"空跑暂停=>急停,切换发生异常:{ex.Message},请检查程序并重启程序", ex);
                return false;
            }
        }
        private bool BurnInAlarmingToEMStop()
        {
            try
            {
                motionCardService.EmgStop();
                if (!actuationBurnInGroup.Stop())
                {
                    logService.WriteLog(LogTypes.DB.ToString(), $"空跑报警->急停, 空跑指令组未能如期结束,请检查程序并重启程序.", MessageDegree.FATAL);
                    dialogWithNoParentService.AppAlert("严重错误", "程序状态与设备状态不一致(空跑报警->急停),请联系开发人员!!!", SysDialogLevel.Error, "", "确定");
                    return false;
                }
                actuationBurnInGroup.Clear();
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"空跑报警->急停,切换发生异常{ex.Message},请检查程序并重启程序", ex);
                return false;
            }
        }
        private bool PowerUppingToEMStop()
        {
            return true;
        }
        private bool GlobalResettingToEMStop()
        {
            try
            {
                motionCardService.EmgStop();
                if (!actuationGlobleResetGroup.Stop())
                {
                    logService.WriteLog(LogTypes.DB.ToString(), "全局复位->急停,复位指令组未能如期结束,请检查程序并重启程序", MessageDegree.FATAL);
                    dialogWithNoParentService.AppAlert("严重错误", "程序状态与设备状态不一致(全局复位->急停),请联系开发人员!!!", SysDialogLevel.Error, "", "确定");
                    return false;
                }
                actuationGlobleResetGroup.Clear();
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"全局复位->急停,切换发生异常{ex.Message},请检查程序并重启程序", ex);
                return false;
            }
        }
        private bool IdlingToEMStop()
        {
            try
            {
                motionCardService.EmgStop();
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"待机->急停,切换发生异常:{ex.Message},请检查程序并重启程序", ex);
                return false;
            }
        }
        private bool RunningToEMStop()
        {
            try
            {
                motionCardService.EmgStop();
                if (!actuationRunningGroup.Stop())
                {
                    logService.WriteLog(LogTypes.DB.ToString(), "运行->急停,Running指令组未能如期结束,请检查程序并重启程序", MessageDegree.FATAL);
                    dialogWithNoParentService.AppAlert("严重错误", "程序状态与设备状态不一致(运行->急停),请联系开发人员!!!", SysDialogLevel.Error, "", "确定");
                    return false;
                }
                actuationRunningGroup.Clear();
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"运行->急停,切换发生异常:{ex.Message},请检查程序并重启程序", ex);
                return false;
            }
        }
        private bool PausingToEMStop()
        {
            try
            {
                motionCardService.EmgStop();
                if (!actuationRunningGroup.Stop())
                {
                    logService.WriteLog(LogTypes.DB.ToString(), "暂停->急停,Running指令组未能如期结束,请检查程序并重启程序", MessageDegree.FATAL);
                    dialogWithNoParentService.AppAlert("严重错误", "程序状态与设备状态不一致(暂停->急停),请联系开发人员!!!", SysDialogLevel.Error, "", "确定");
                    return false;
                }
                actuationRunningGroup.ProcessResume();

                actuationRunningGroup.Clear();
                GlobalValues.InterlockPauseWithAlarm = false;
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"暂停->急停,切换发生异常:{ex.Message},请检查程序并重启程序", ex);
                return false;
            }
        }
        private bool BurnInTestingToEMStop()
        {
            try
            {
                motionCardService.EmgStop();
                if (!actuationBurnInGroup.Stop())
                {
                    logService.WriteLog(LogTypes.DB.ToString(), "空跑运行->急停,BurnIn指令组未能如期结束,请检查程序并重启程序", MessageDegree.FATAL);
                    dialogWithNoParentService.AppAlert("严重错误", "程序状态与设备状态不一致(空跑运行->急停),请联系开发人员!!!", SysDialogLevel.Error, "", "确定");
                    return false;
                }
                actuationBurnInGroup.Clear();
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"空跑运行->急停,切换发生异常:{ex.Message},请检查程序并重启程序", ex);
                return false;
            }
        }
        private bool AlarmingToEMStop()
        {
            try
            {
                motionCardService.EmgStop();
                if (!actuationRunningGroup.Stop())
                {
                    logService.WriteLog(LogTypes.DB.ToString(), "报警->急停, Running指令组未能如期结束,请检查程序并重启程序", MessageDegree.FATAL);
                    dialogWithNoParentService.AppAlert("严重错误", "程序状态与设备状态不一致(报警->急停),请联系开发人员!!!", SysDialogLevel.Error, "", "确定");
                    return false;
                }
                actuationRunningGroup.Clear();
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"报警->急停,切换发生异常:{ex.Message},请检查程序并重启程序", ex);
                return false;
            }
        }
    }
    public class StopTransition : FSMTransition
    {
        private readonly ILogService logService;
        private readonly IDialogWithNoParentService dialogWithNoParentService;
        private readonly ActuationRunningGroupManager actuationRunningGroup = null;
        private readonly ActuationGlobleResetGroupManager actuationGlobleResetGroup = null;
        private readonly ActuationBurnInGroupManager actuationBurnInGroup = null;
        private readonly IMotionCardService motionCardService;
        private readonly IMachineInterlock machineInterlock;
        public StopTransition(IContainerProvider containerProvider, FSMState currentState, FSMState nexeState) : base(FSMEventCode.Stop, currentState, nexeState)
        {
            this.logService = containerProvider.Resolve<ILogService>();
            this.dialogWithNoParentService = containerProvider.Resolve<IDialogWithNoParentService>();
            this.actuationRunningGroup = containerProvider.Resolve<ActuationRunningGroupManager>();
            this.actuationGlobleResetGroup = containerProvider.Resolve<ActuationGlobleResetGroupManager>();
            this.actuationBurnInGroup = containerProvider.Resolve<ActuationBurnInGroupManager>();
            this.motionCardService = containerProvider.Resolve<IMotionCardService>();
            this.machineInterlock = containerProvider.Resolve<IMachineInterlock>();
        }
        protected override bool DoExecute(FSMEvent fsmEvent)
        {

            logService.WriteLog(LogTypes.DB.ToString(), "停止按钮被按下", MessageDegree.INFO);
            switch (currentState.GetStateCode())
            {
                case FSMStateCode.GlobleResetting:
                    return GlobleResetingToEMStop();
                case FSMStateCode.Running:
                    return RunningToPausing();
                case FSMStateCode.Alarming:
                    return AlarmingToPausing();
                case FSMStateCode.EmergencyStopping:
                    return EmergencyStopToPowerUpping();
                case FSMStateCode.BurnInAlarming:
                    return BurnInAlarmingToBurnInPausing();
                case FSMStateCode.BurnInTesting:
                    return BurnInTestingToBurnInPausing();
                default:
                    break;
            }
            return false;
        }
        private bool BurnInTestingToBurnInPausing()
        {
            try
            {
                actuationBurnInGroup.ProcessPause();
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"空跑运行->空跑暂停,切换发生异常:{ex.Message},请检查程序,发送急停事件", ex);
                motionCardService.EmgStop();
                GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                return false;
            }
        }
        private bool BurnInAlarmingToBurnInPausing()
        {
            try
            {
                actuationBurnInGroup.ProcessPause();
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"空跑报警->空跑暂停,切换发生异常:{ex.Message},请检查程序,发送急停事件", ex);
                motionCardService.EmgStop();
                GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                return false;
            }
        }
        private bool EmergencyStopToPowerUpping()
        {
            if (!motionCardService.EmgStopCancel())
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"取消急停失败", MessageDegree.ERROR);
                return false;
            }
            return true;
        }
        private bool RunningToPausing()
        {
            try
            {
                actuationRunningGroup.ProcessPause();
                //开始记录联锁状态
                machineInterlock.InterlockRecord();
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"运行->暂停,切换发生异常:{ex.Message},请检查程序,发送急停事件", ex);
                motionCardService.EmgStop();
                GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                return false;
            }
        }
        private bool GlobleResetingToEMStop()
        {
            try
            {
                motionCardService.EmgStop();
                if (!actuationGlobleResetGroup.Stop())
                {
                    logService.WriteLog(LogTypes.DB.ToString(), "全局复位->急停(StopTransition),复位指令组未能如期结束,请检查程序并重启程序", MessageDegree.FATAL);
                    dialogWithNoParentService.AppAlert("严重错误", "程序状态与设备状态不一致(全局复位->急停(StopTransition)),请联系开发人员!!!", SysDialogLevel.Error, "", "确定");
                    return false;
                }
                actuationGlobleResetGroup.Clear();
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"全局复位->急停(StopTransition),切换发生异常:{ex.Message},请检查程序并重启程序", ex);
                return false;
            }
        }
        private bool AlarmingToPausing()
        {
            try
            {
                actuationRunningGroup.ProcessPause();
                //开始记录联锁状态
                machineInterlock.InterlockRecord();
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"报警->暂停,切换发生异常:{ex.Message},请检查程序,发送急停事件", ex);
                motionCardService.EmgStop();
                GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                return false;
            }
        }
    }

    public class LeaveBurnInTransition : FSMTransition
    {
        private readonly ILogService logService;
        private readonly IMotionCardService motionCardService;
        private readonly IDialogWithNoParentService dialogWithNoParentService;
        private readonly ActuationBurnInGroupManager actuationBurnInGroup = null;
        public LeaveBurnInTransition(IContainerProvider containerProvider, FSMState currentState, FSMState nexeState) : base(FSMEventCode.LeaveBurnIn, currentState, nexeState)
        {
            this.logService = containerProvider.Resolve<ILogService>();
            this.motionCardService = containerProvider.Resolve<IMotionCardService>();
            this.dialogWithNoParentService = containerProvider.Resolve<IDialogWithNoParentService>();
            this.actuationBurnInGroup = containerProvider.Resolve<ActuationBurnInGroupManager>();
        }

        protected override bool DoExecute(FSMEvent fsmEvent)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $"离开空跑", MessageDegree.INFO);
            return BurnInTestingToPowerUpping();
        }
        private bool BurnInTestingToPowerUpping()
        {
            try
            {
                if (!actuationBurnInGroup.Stop())
                {
                    logService.WriteLog(LogTypes.DB.ToString(), "空跑运行->上电, 空跑指令组未能如期结束,请检查程序并重启程序", MessageDegree.FATAL);
                    dialogWithNoParentService.AppAlert("严重错误", "程序状态与设备状态不一致(空跑运行->上电),请联系开发人员!!!", SysDialogLevel.Error, "", "确定");
                    return false;
                }
                //清空指令集合虚拟IO集合
                actuationBurnInGroup.Clear();
                return true;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"空跑运行->上电,切换发生异常:{ex.Message},请检查程序,发送急停事件", ex);
                motionCardService.EmgStop();
                GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                return false;
            }
        }
    }
    public class EnterBurnInTransition : FSMTransition
    {
        private readonly ILogService logService;
        private readonly IMotionCardService motionCardService;
        private readonly ActuationBurnInGroupManager actuationBurnInGroup = null;
        public EnterBurnInTransition(IContainerProvider containerProvider, FSMState currentState, FSMState nexeState) : base(FSMEventCode.EnterBurnIn, currentState, nexeState)
        {
            this.logService = containerProvider.Resolve<ILogService>();
            this.motionCardService = containerProvider.Resolve<IMotionCardService>();
            this.actuationBurnInGroup = containerProvider.Resolve<ActuationBurnInGroupManager>();
        }

        protected override bool DoExecute(FSMEvent fsmEvent)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $"切换到空跑运行", MessageDegree.INFO);
            return IdlingToBrunInTesting();
        }
        private bool IdlingToBrunInTesting()
        {
            try
            {
                return actuationBurnInGroup.BurnInActuationResetAndStart(logService);
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"待机->空跑运行,切换发生异常:{ex.Message},请检查程序,发送急停事件", ex);
                motionCardService.EmgStop();
                GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                return false;
            }
        }
    }
    public class AlarmTransition : FSMTransition
    {
        private readonly ILogService logService;
        public AlarmTransition(IContainerProvider containerProvider, FSMState currentState, FSMState nexeState) : base(FSMEventCode.Alarm, currentState, nexeState)
        {
            this.logService = containerProvider.Resolve<ILogService>();
        }

        protected override bool DoExecute(FSMEvent fsmEvent)
        {
            switch (currentState.GetStateCode())
            {
                case FSMStateCode.Running:
                    logService.WriteLog(LogTypes.DB.ToString(), $"运行切换到报警", MessageDegree.INFO);
                    return RunningToAlarming();
                case FSMStateCode.BurnInTesting:
                    logService.WriteLog(LogTypes.DB.ToString(), $"空跑运行切换到空跑报警", MessageDegree.INFO);
                    return BurnInTestingToBurnInAlarming();
                default:
                    break;
            }
            return false;
        }
        private bool BurnInTestingToBurnInAlarming()
        {
            return true;
        }
        private bool RunningToAlarming()
        {
            return true;
        }
    }
}