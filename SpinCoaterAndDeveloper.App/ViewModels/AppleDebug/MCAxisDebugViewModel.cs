using AutoMapper;
using DataBaseServiceInterface;
using LogServiceInterface;
using MaterialDesignThemes.Wpf;
using MotionCardServiceInterface;
using MotionControlActuation;
using PermissionServiceInterface;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using SpinCoaterAndDeveloper.App.Common.Models;
using SpinCoaterAndDeveloper.App.Extensions;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using SpinCoaterAndDeveloper.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class MCAxisDebugViewModel : BindableBase, INavigationAware
    {
        private static readonly object axisChangeLock = new object();

        private readonly IMapper mapper;
        private readonly ILogService logService;
        private readonly IPermissionService permissionService;
        private readonly IDataBaseService dataBaseService;
        private readonly IMotionCardService motionCardService;
        private readonly ISnackbarMessageQueue snackbarMessageQueue;

        private CancellationTokenSource cancellationTokenSource = null;
        private CancellationTokenSource relMoveArrivedCancellationToken = null;
        private CancellationTokenSource reciprocateCancellationToken = null;
        private CancellationTokenSource homingCancellationToken = null;

        #region Binding
        private bool buttonEnable = true;
        public bool ButtonEnable
        {
            get { return buttonEnable; }
            set { SetProperty(ref buttonEnable, value); }
        }
        private uint _EcatErrorCode;
        public uint EcatErrorCode
        {
            get { return _EcatErrorCode; }
            set { SetProperty(ref _EcatErrorCode, value); }
        }
        private bool _ReciprocateEnable = true;
        public bool ReciprocateEnable
        {
            get { return _ReciprocateEnable; }
            set { SetProperty(ref _ReciprocateEnable, value); }
        }
        private bool _HomingButtonEnable = true;
        public bool HomingButtonEnable
        {
            get { return _HomingButtonEnable; }
            set { SetProperty(ref _HomingButtonEnable, value); }
        }
        #endregion
        public ObservableCollection<AxisMonitorModel> AxisMonitorCollection { get; set; } = new ObservableCollection<AxisMonitorModel>();
        public ObservableCollection<string> AxisGroupCollection { get; set; } = new ObservableCollection<string>();
        public MCAxisDebugViewModel(IContainerProvider containerProvider)
        {
            this.mapper = containerProvider.Resolve<IMapper>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.permissionService = containerProvider.Resolve<IPermissionService>();
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
            this.motionCardService = containerProvider.Resolve<IMotionCardService>();
            this.snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();
        }

        private DelegateCommand<string> _AxesGroupChangedCommand;
        public DelegateCommand<string> AxesGroupChangedCommand =>
            _AxesGroupChangedCommand ?? (_AxesGroupChangedCommand = new DelegateCommand<string>(ExecuteAxesGroupChangedCommand));

        void ExecuteAxesGroupChangedCommand(string parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter))
            {
                return;
            }
            var axes = dataBaseService.Db.Queryable<AxisInfoEntity>().Where(x => x.Group == parameter).OrderBy(x => x.Number).OrderBy(x => x.Id).ToList();
            lock (axisChangeLock)
            {
                AxisMonitorCollection.Clear();
                mapper.Map(axes, AxisMonitorCollection);
            }
            snackbarMessageQueue.EnqueueEx("筛选成功");
        }

        private DelegateCommand _ShowAllAxesCommand;
        public DelegateCommand ShowAllAxesCommand =>
            _ShowAllAxesCommand ?? (_ShowAllAxesCommand = new DelegateCommand(ExecuteShowAllAxesCommand));

        void ExecuteShowAllAxesCommand()
        {
            lock (axisChangeLock)
            {
                GetAxesInfo();
            }
            AxisGroupCollection.Clear();
            GetAxesGroup();
            snackbarMessageQueue.EnqueueEx("显示所有成功");
        }

        private DelegateCommand<AxisMonitorModel> _UpdateJogVelCommand;
        public DelegateCommand<AxisMonitorModel> UpdateJogVelCommand =>
            _UpdateJogVelCommand ?? (_UpdateJogVelCommand = new DelegateCommand<AxisMonitorModel>(ExecuteUpdateJogVelCommand));
        //更新记录Jog速度
        void ExecuteUpdateJogVelCommand(AxisMonitorModel parameter)
        {
            var result = dataBaseService.Db.Updateable(mapper.Map<AxisInfoEntity>(parameter)).ExecuteCommand();
            if (result != 0)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"Jog速度已保存", MessageDegree.INFO);
            }
            else
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"Jog速度保存失败", MessageDegree.ERROR);
            }
        }

        private DelegateCommand<AxisMonitorModel> _JogNegativeCommand;
        public DelegateCommand<AxisMonitorModel> JogNegativeCommand =>
            _JogNegativeCommand ?? (_JogNegativeCommand = new DelegateCommand<AxisMonitorModel>(ExecuteJogNegativeCommand));

        void ExecuteJogNegativeCommand(AxisMonitorModel parameter)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}对轴{parameter.Name}负向点动", MessageDegree.INFO);
            //Jog加减速使用回原加速度
            motionCardService.JogMoveStart((short)parameter.AxisIdOnCard, parameter.JogVel * parameter.Proportion, Direction.Negative, parameter.HomeAcc * parameter.Proportion, parameter.HomeAcc * parameter.Proportion);
        }

        private DelegateCommand<AxisMonitorModel> _JogPositiveCommand;
        public DelegateCommand<AxisMonitorModel> JogPositiveCommand =>
            _JogPositiveCommand ?? (_JogPositiveCommand = new DelegateCommand<AxisMonitorModel>(ExecuteJogPositiveCommand));

        void ExecuteJogPositiveCommand(AxisMonitorModel parameter)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}对轴{parameter.Name}正向点动", MessageDegree.INFO);
            //Jog加减速使用回原加速度
            motionCardService.JogMoveStart((short)parameter.AxisIdOnCard, parameter.JogVel * parameter.Proportion, Direction.Positive, parameter.HomeAcc * parameter.Proportion, parameter.HomeAcc * parameter.Proportion);
        }

        private DelegateCommand<AxisMonitorModel> _JogMoveStopCommand;
        public DelegateCommand<AxisMonitorModel> JogMoveStopCommand =>
            _JogMoveStopCommand ?? (_JogMoveStopCommand = new DelegateCommand<AxisMonitorModel>(ExecuteJogMoveStopCommand));
        //Jog停止
        void ExecuteJogMoveStopCommand(AxisMonitorModel parameter)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}停止点动", MessageDegree.INFO);
            motionCardService.JogMoveStop((short)parameter.AxisIdOnCard);
        }

        private DelegateCommand<AxisMonitorModel> _AxisServoOnCommand;
        public DelegateCommand<AxisMonitorModel> AxisServoOnCommand =>
            _AxisServoOnCommand ?? (_AxisServoOnCommand = new DelegateCommand<AxisMonitorModel>(ExecuteAxisServoOnCommand).ObservesCanExecute(() => ButtonEnable));
        //上使能
        void ExecuteAxisServoOnCommand(AxisMonitorModel parameter)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}对轴{parameter.Name}上使能", MessageDegree.INFO);
            motionCardService.AxisServo((short)parameter.AxisIdOnCard, true);
        }

        private DelegateCommand<AxisMonitorModel> _AxisClearErrorCommand;
        public DelegateCommand<AxisMonitorModel> AxisClearErrorCommand =>
            _AxisClearErrorCommand ?? (_AxisClearErrorCommand = new DelegateCommand<AxisMonitorModel>(ExecuteAxisClearErrorCommand).ObservesCanExecute(() => ButtonEnable));
        //清除轴错误
        void ExecuteAxisClearErrorCommand(AxisMonitorModel parameter)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}对轴{parameter.Name}清除错误", MessageDegree.INFO);
            motionCardService.ClearAxSts((short)parameter.AxisIdOnCard);
        }

        private DelegateCommand<AxisMonitorModel> _AxisServoOffCommand;
        public DelegateCommand<AxisMonitorModel> AxisServoOffCommand =>
            _AxisServoOffCommand ?? (_AxisServoOffCommand = new DelegateCommand<AxisMonitorModel>(ExecuteAxisServoOffCommand).ObservesCanExecute(() => ButtonEnable));
        //下使能
        void ExecuteAxisServoOffCommand(AxisMonitorModel parameter)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}对轴{parameter.Name}下使能", MessageDegree.INFO);
            motionCardService.AxisServo((short)parameter.AxisIdOnCard, false);
        }

        private DelegateCommand<AxisMonitorModel> _RelMoveCommand;
        public DelegateCommand<AxisMonitorModel> RelMoveCommand =>
            _RelMoveCommand ?? (_RelMoveCommand = new DelegateCommand<AxisMonitorModel>(ExecuteRelMoveCommand).ObservesCanExecute(() => ButtonEnable));

        async void ExecuteRelMoveCommand(AxisMonitorModel parameter)
        {
            try
            {
                ButtonEnable = false;
                logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}启动寸动,寸动距离{parameter.RelMoveDistance}", MessageDegree.INFO);
                motionCardService.AxisServo((short)parameter.AxisIdOnCard, true);
                motionCardService.StartMoveRel((short)parameter.AxisIdOnCard, 0, 0, parameter.JogVel * parameter.Proportion, parameter.HomeAcc * parameter.Proportion, parameter.HomeAcc * parameter.Proportion, parameter.RelMoveDistance * parameter.Proportion);
                relMoveArrivedCancellationToken = new CancellationTokenSource();
                var result = await Task.Run(() =>
                {
                    while (true)
                    {
                        if (relMoveArrivedCancellationToken.IsCancellationRequested)
                        {
                            foreach (var axis in MotionControlResource.AxisResource)
                                motionCardService.StopMove((short)axis.Value.AxisIdOnCard, 1);
                            return false;
                        }
                        if (motionCardService.RealTimeCheckAxisArrivedEx(parameter.Name)) break;
                        Thread.Sleep(100);
                    }
                    return true;
                }, relMoveArrivedCancellationToken.Token);
                var msg = result ? "寸动到位" : "寸动失败";
                logService.WriteLog(LogTypes.DB.ToString(), $@"{msg}", MessageDegree.INFO);
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"寸动异常{ex.Message}", ex);
            }
            finally
            {
                ButtonEnable = true;
            }
        }

        private DelegateCommand<AxisMonitorModel> _RelMoveStopCommand;
        public DelegateCommand<AxisMonitorModel> RelMoveStopCommand =>
            _RelMoveStopCommand ?? (_RelMoveStopCommand = new DelegateCommand<AxisMonitorModel>(ExecuteRelMoveStopCommand));

        void ExecuteRelMoveStopCommand(AxisMonitorModel parameter)
        {
            relMoveArrivedCancellationToken?.Cancel();
        }

        private DelegateCommand<AxisMonitorModel> _ReciprocateStartCommand;
        public DelegateCommand<AxisMonitorModel> ReciprocateStartCommand =>
            _ReciprocateStartCommand ?? (_ReciprocateStartCommand = new DelegateCommand<AxisMonitorModel>(ExecuteReciprocateStartCommand).ObservesCanExecute(() => ReciprocateEnable));
        //往复运动启动
        async void ExecuteReciprocateStartCommand(AxisMonitorModel parameter)
        {
            if (parameter.ReciprocateStartPoint == parameter.ReciprocateEndPoint)
            {
                snackbarMessageQueue.EnqueueEx("往复运动起点与终点不能相同");
                return;
            }

            if (GlobalValues.MachineStatus != FSMStateCode.Idling)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"设备不在待机中不允许往复运动执行", MessageDegree.ERROR);
                return;
            }

            ReciprocateEnable = false;
            reciprocateCancellationToken = new CancellationTokenSource();
            await Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        motionCardService.StartMoveAbs((short)parameter.AxisIdOnCard,
                                                       0,
                                                       0,
                                                       parameter.JogVel * parameter.Proportion,
                                                       parameter.HomeAcc * parameter.Proportion,
                                                       parameter.HomeAcc * parameter.Proportion,
                                                       parameter.ReciprocateStartPoint * parameter.Proportion);
                        while (true)
                        {
                            if (reciprocateCancellationToken.IsCancellationRequested)
                            {
                                motionCardService.StopMove((short)parameter.AxisIdOnCard, 1);
                                return;
                            }
                            if (motionCardService.RealTimeCheckAxisArrivedEx(parameter.Name))
                            {
                                break;
                            }
                            Thread.Sleep(100);
                        }
                        if (parameter.ReciprocatePauseTime != 0)
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(parameter.ReciprocatePauseTime), reciprocateCancellationToken.Token);
                        }
                        else
                        {
                            await Task.Delay(500, reciprocateCancellationToken.Token);
                        }
                        motionCardService.StartMoveAbs((short)parameter.AxisIdOnCard,
                                                       0,
                                                       0,
                                                       parameter.JogVel * parameter.Proportion,
                                                       parameter.HomeAcc * parameter.Proportion,
                                                       parameter.HomeAcc * parameter.Proportion,
                                                       parameter.ReciprocateEndPoint * parameter.Proportion);
                        while (true)
                        {
                            if (reciprocateCancellationToken.IsCancellationRequested)
                            {
                                motionCardService.StopMove((short)parameter.AxisIdOnCard, 1);
                                return;
                            }
                            if (motionCardService.RealTimeCheckAxisArrivedEx(parameter.Name))
                            {
                                break;
                            }
                            Thread.Sleep(100);
                        }
                        if (parameter.ReciprocatePauseTime != 0)
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(parameter.ReciprocatePauseTime), reciprocateCancellationToken.Token);
                        }
                        else
                        {
                            await Task.Delay(500, reciprocateCancellationToken.Token);
                        }
                        Thread.Sleep(100);
                    }
                }
                catch (TaskCanceledException)
                {
                    logService.WriteLog(LogTypes.DB.ToString(), $@"轴{parameter.Name}往复运动取消", MessageDegree.INFO);
                }
                catch (Exception ex)
                {
                    logService.WriteLog(LogTypes.DB.ToString(), $@"轴{parameter.Name}往复运动异常:{ex.Message}", ex);
                }
            }, reciprocateCancellationToken.Token);
            ReciprocateEnable = true;
        }

        private DelegateCommand<AxisMonitorModel> _ReciprocateStopCommand;
        public DelegateCommand<AxisMonitorModel> ReciprocateStopCommand =>
            _ReciprocateStopCommand ?? (_ReciprocateStopCommand = new DelegateCommand<AxisMonitorModel>(ExecuteReciprocateStopCommand));
        //往复运动停止
        void ExecuteReciprocateStopCommand(AxisMonitorModel parameter)
        {
            reciprocateCancellationToken?.Cancel();
        }

        private DelegateCommand<AxisMonitorModel> _HomingStartCommand;
        public DelegateCommand<AxisMonitorModel> HomingStartCommand =>
            _HomingStartCommand ?? (_HomingStartCommand = new DelegateCommand<AxisMonitorModel>(ExecuteHomingStartCommand).ObservesCanExecute(() => HomingButtonEnable));
        //回原启动
        async void ExecuteHomingStartCommand(AxisMonitorModel parameter)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户点击轴{parameter.Name}回原", MessageDegree.INFO);
            HomingButtonEnable = false;
            homingCancellationToken = new CancellationTokenSource();
            await Task.Run(() =>
            {
                try
                {
                    motionCardService.StartHoming((short)parameter.AxisIdOnCard,
                                                  (short)parameter.HomeMethod,
                                                  (int)(parameter.HomeOffset * parameter.Proportion),
                                                  (uint)(parameter.HomeHighVel * parameter.Proportion),
                                                  (uint)(parameter.HomeLowVel * parameter.Proportion),
                                                  (uint)(parameter.HomeAcc * parameter.Proportion),
                                                  (uint)parameter.HomeTimeout,
                                                  0);
                    DateTime startTime = DateTime.Now;
                    Thread.Sleep(100);
                    while (true)
                    {
                        if (homingCancellationToken.IsCancellationRequested)
                        {
                            logService.WriteLog(LogTypes.DB.ToString(), $@"回原取消", MessageDegree.WARN);
                            motionCardService.StopHoming((short)parameter.AxisIdOnCard);
                            return;
                        }
                        if (motionCardService.GetHomingSts((short)parameter.AxisIdOnCard))
                        {
                            logService.WriteLog(LogTypes.DB.ToString(), $@"回原结束", MessageDegree.INFO);
                            motionCardService.FinishHoming((short)parameter.AxisIdOnCard);
                            return;
                        }
                        if ((DateTime.Now - startTime).TotalMilliseconds > parameter.HomeTimeout)
                        {
                            logService.WriteLog(LogTypes.DB.ToString(), $@"回原超时", MessageDegree.ERROR);
                            motionCardService.StopHoming((short)parameter.AxisIdOnCard);
                            return;
                        }
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    logService.WriteLog(LogTypes.DB.ToString(), $@"轴{parameter.Name}回原异常:{ex.Message}", ex);
                }
            }, homingCancellationToken.Token);
            HomingButtonEnable = true;
        }

        private DelegateCommand<AxisMonitorModel> _HomingStopCommand;
        public DelegateCommand<AxisMonitorModel> HomingStopCommand =>
            _HomingStopCommand ?? (_HomingStopCommand = new DelegateCommand<AxisMonitorModel>(ExecuteHomingStopCommand));
        //回原停止
        void ExecuteHomingStopCommand(AxisMonitorModel parameter)
        {
            homingCancellationToken?.Cancel();
        }
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                //添加轴分组
                AxisGroupCollection.Clear();
                GetAxesGroup();
                GetAxesInfo();
            }), System.Windows.Threading.DispatcherPriority.Render);

            //启动刷新线程
            cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    Thread.Sleep(200);
                    try
                    {
                        lock (axisChangeLock)
                        {
                            foreach (var axis in AxisMonitorCollection)
                            {
                                axis.Status = MotionControlResource.AxisResource[axis.Name].Status;
                                axis.PrfPos = MotionControlResource.AxisResource[axis.Name].PrfPos / MotionControlResource.AxisResource[axis.Name].Proportion;
                                axis.PrfVel = MotionControlResource.AxisResource[axis.Name].PrfVel / MotionControlResource.AxisResource[axis.Name].Proportion;
                                axis.EncPos = MotionControlResource.AxisResource[axis.Name].EncPos / MotionControlResource.AxisResource[axis.Name].Proportion;
                                if (motionCardService.InitSuccess)
                                {
                                    var err = motionCardService.GetAxErrorCode((short)axis.AxisIdOnCard);
                                    axis.ErrorCode = err != null ? (short)err : (short)0;
                                }
                            }
                        }
                        motionCardService.GetEcatErrorCode(out uint ecatErrorCode);
                        EcatErrorCode = ecatErrorCode;
                    }
                    catch (Exception ex)
                    {
                        logService.WriteLog(LogTypes.DB.ToString(), $@"轴监控页面刷新轴状态线程异常:{ex.Message}", ex);
                    }
                }
            }, cancellationTokenSource.Token);
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}进入轴监控页面", MessageDegree.INFO);
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            cancellationTokenSource?.Cancel();
            relMoveArrivedCancellationToken?.Cancel();
            reciprocateCancellationToken?.Cancel();
            homingCancellationToken?.Cancel();
        }

        private void GetAxesInfo()
        {
            AxisMonitorCollection.Clear();
            var axisMonitor = dataBaseService.Db.Queryable<AxisInfoEntity>().OrderBy(x => x.Number).OrderBy(x => x.Id).ToList();
            mapper.Map(axisMonitor, AxisMonitorCollection);
        }

        private void GetAxesGroup()
        {
            var axisGroups = dataBaseService.Db.Queryable<AxisInfoEntity>().Distinct().Select(x => x.Group).ToList();
            axisGroups.ForEach(x => { if (!string.IsNullOrWhiteSpace(x) && !AxisGroupCollection.Contains(x)) AxisGroupCollection.Add(x); });
        }
    }
}
