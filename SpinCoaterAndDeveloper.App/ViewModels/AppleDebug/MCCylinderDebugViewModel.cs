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
using System.Windows.Data;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class MCCylinderDebugViewModel : BindableBase, INavigationAware
    {
        private static readonly object cylinderChangeLock = new object();

        private readonly IMapper mapper;
        private readonly ILogService logService;
        private readonly IPermissionService permissionService;
        private readonly IDataBaseService dataBaseService;
        private readonly IMotionCardService motionCardService;
        private readonly ISnackbarMessageQueue snackbarMessageQueue;

        private CancellationTokenSource reciprocateCancellationToken = null;

        private string _CylinderFilter;
        public string CylinderFilter
        {
            get { return _CylinderFilter; }
            set { SetProperty(ref _CylinderFilter, value); }
        }
        private bool _ReciprocateEnable = true;
        public bool ReciprocateEnable
        {
            get { return _ReciprocateEnable; }
            set { SetProperty(ref _ReciprocateEnable, value); }
        }

        private CancellationTokenSource cancellationTokenSource;
        public ObservableCollection<string> CylinderGroupCollection { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<CylinderMonitorModel> CylinderMonitorCollection { get; set; } = new ObservableCollection<CylinderMonitorModel>();
        public MCCylinderDebugViewModel(IContainerProvider containerProvider)
        {
            this.mapper = containerProvider.Resolve<IMapper>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.permissionService = containerProvider.Resolve<IPermissionService>();
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
            this.motionCardService = containerProvider.Resolve<IMotionCardService>();
            this.snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();

            CollectionView collectionView = CollectionViewSource.GetDefaultView(CylinderMonitorCollection) as CollectionView;
            collectionView.Filter = x =>
            {
                if (string.IsNullOrWhiteSpace(CylinderFilter)) return true;
                return (x as CylinderMonitorModel).ShowOnUIName.Contains(CylinderFilter);
            };
        }

        private DelegateCommand _CylinderFilterCommand;
        public DelegateCommand CylinderFilterCommand =>
            _CylinderFilterCommand ?? (_CylinderFilterCommand = new DelegateCommand(ExecuteCylinderFilterCommand));

        void ExecuteCylinderFilterCommand()
        {
            CollectionViewSource.GetDefaultView(CylinderMonitorCollection).Refresh();
        }

        private DelegateCommand<string> _CylinderGroupChangedCommand;
        public DelegateCommand<string> CylinderGroupChangedCommand =>
            _CylinderGroupChangedCommand ?? (_CylinderGroupChangedCommand = new DelegateCommand<string>(ExecuteCylinderGroupChangedCommand));

        void ExecuteCylinderGroupChangedCommand(string parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter))
            {
                return;
            }
            var cylinders = dataBaseService.Db.Queryable<CylinderInfoEntity>()
                                              .Includes(x => x.SingleValveOutputInfo)
                                              .Includes(x => x.DualValveOriginOutputInfo)
                                              .Includes(x => x.DualValveMovingOutputInfo)
                                              .Includes(x => x.SensorOriginInputInfo)
                                              .Includes(x => x.SensorMovingInputInfo)
                                              .Where(x => x.Group == parameter)
                                              .OrderBy(x => x.Number)
                                              .OrderBy(x => x.Id)
                                              .ToList();
            lock (cylinderChangeLock)
            {
                CylinderMonitorCollection.Clear();
                mapper.Map(cylinders, CylinderMonitorCollection);
            }
            snackbarMessageQueue.EnqueueEx("筛选成功");
        }

        private DelegateCommand _ShowAllCylinderCommand;
        public DelegateCommand ShowAllCylinderCommand =>
            _ShowAllCylinderCommand ?? (_ShowAllCylinderCommand = new DelegateCommand(ExecuteShowAllCylinderCommand));

        void ExecuteShowAllCylinderCommand()
        {
            CylinderFilter = "";
            GetCylinderInfo();
            CylinderGroupCollection.Clear();
            GetCylinderGroups();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}显示所有气缸成功", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("显示所有气缸成功");
        }

        private DelegateCommand<CylinderMonitorModel> _DualValveMovingOutputCommand;
        public DelegateCommand<CylinderMonitorModel> DualValveMovingOutputCommand =>
            _DualValveMovingOutputCommand ?? (_DualValveMovingOutputCommand = new DelegateCommand<CylinderMonitorModel>(ExecuteDualValveMovingOutputCommand));

        void ExecuteDualValveMovingOutputCommand(CylinderMonitorModel parameter)
        {
            if (parameter.DualValveMovingOutputInfo != null)
            {
                motionCardService.SetOuputStsEx(parameter.DualValveMovingOutputInfo.Name, !MotionControlResource.IOOutputResource[parameter.DualValveMovingOutputInfo.Name].Status);
            }
        }

        private DelegateCommand<CylinderMonitorModel> _SingleValveOutputCommand;
        public DelegateCommand<CylinderMonitorModel> SingleValveOutputCommand =>
            _SingleValveOutputCommand ?? (_SingleValveOutputCommand = new DelegateCommand<CylinderMonitorModel>(ExecuteSingleValveOutputCommand));

        void ExecuteSingleValveOutputCommand(CylinderMonitorModel parameter)
        {
            if (parameter.SingleValveOutputInfo != null)
            {
                motionCardService.SetOuputStsEx(parameter.SingleValveOutputInfo.Name, !MotionControlResource.IOOutputResource[parameter.SingleValveOutputInfo.Name].Status);
            }
        }

        private DelegateCommand<CylinderMonitorModel> _DualValveOriginOutputCommand;
        public DelegateCommand<CylinderMonitorModel> DualValveOriginOutputCommand =>
            _DualValveOriginOutputCommand ?? (_DualValveOriginOutputCommand = new DelegateCommand<CylinderMonitorModel>(ExecuteDualValveOriginOutputCommand));

        void ExecuteDualValveOriginOutputCommand(CylinderMonitorModel parameter)
        {
            if (parameter.DualValveOriginOutputInfo != null)
            {
                motionCardService.SetOuputStsEx(parameter.DualValveOriginOutputInfo.Name, !MotionControlResource.IOOutputResource[parameter.DualValveOriginOutputInfo.Name].Status);
            }
        }


        private DelegateCommand<CylinderMonitorModel> _ShiedOrignalSensorCommand;
        public DelegateCommand<CylinderMonitorModel> ShiedOrignalSensorCommand =>
            _ShiedOrignalSensorCommand ?? (_ShiedOrignalSensorCommand = new DelegateCommand<CylinderMonitorModel>(ExecuteShiedOrignalSensorCommand));

        async void ExecuteShiedOrignalSensorCommand(CylinderMonitorModel parameter)
        {
            //屏蔽原点传感器
            if (parameter.ShiedSensorOriginInput == false && parameter.ShiedSensorOriginInputDelayTime == 0)
            {
                snackbarMessageQueue.EnqueueEx("原点传感器延时时间为0,不能屏蔽,请设定正确的原点传感器屏蔽后延时时间.");
                return;
            }
            parameter.ShiedSensorOriginInput = !parameter.ShiedSensorOriginInput;
            foreach (var cylinder in GlobalValues.CylinderDicCollection)
            {
                if (cylinder.Value.Name == parameter.Name)
                {
                    cylinder.Value.ShiedSensorOriginInput = parameter.ShiedSensorOriginInput;
                    break;
                }
            }
            await dataBaseService.Db.Updateable(mapper.Map<CylinderInfoEntity>(parameter)).ExecuteCommandAsync();
        }

        private DelegateCommand<CylinderMonitorModel> _ShiedMovingSensorCommand;
        public DelegateCommand<CylinderMonitorModel> ShiedMovingSensorCommand =>
            _ShiedMovingSensorCommand ?? (_ShiedMovingSensorCommand = new DelegateCommand<CylinderMonitorModel>(ExecuteShiedMovingSensorCommand));

        async void ExecuteShiedMovingSensorCommand(CylinderMonitorModel parameter)
        {
            //屏蔽动点传感器
            if (parameter.ShiedSensorMovingInput == false && parameter.ShiedSensorMovingInputDelayTime == 0)
            {
                snackbarMessageQueue.EnqueueEx("动点传感器延时时间为0,不能屏蔽,请设定正确的动点传感器屏蔽后延时时间.");
                return;
            }
            parameter.ShiedSensorMovingInput = !parameter.ShiedSensorMovingInput;
            foreach (var cylinder in GlobalValues.CylinderDicCollection)
            {
                if (cylinder.Value.Name == parameter.Name)
                {
                    cylinder.Value.ShiedSensorMovingInput = parameter.ShiedSensorMovingInput;
                    break;
                }
            }
            await dataBaseService.Db.Updateable(mapper.Map<CylinderInfoEntity>(parameter)).ExecuteCommandAsync();
        }

        private DelegateCommand<CylinderMonitorModel> _ReciprocateStartCommand;
        public DelegateCommand<CylinderMonitorModel> ReciprocateStartCommand =>
            _ReciprocateStartCommand ?? (_ReciprocateStartCommand = new DelegateCommand<CylinderMonitorModel>(ExecuteReciprocateStartCommand).ObservesCanExecute(() => ReciprocateEnable));

        async void ExecuteReciprocateStartCommand(CylinderMonitorModel parameter)
        {
            if (GlobalValues.MachineStatus != FSMStateCode.Idling && GlobalValues.MachineStatus != FSMStateCode.PowerUpping)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"设备不在待机或上电中不允许气缸往复运动执行", MessageDegree.ERROR);
                return;
            }
            ReciprocateEnable = false;
            reciprocateCancellationToken = new CancellationTokenSource();
            await Task.Run(async () =>
            {
                try
                {
                    while (!reciprocateCancellationToken.IsCancellationRequested)
                    {
                        motionCardService.CylinderControlEx(parameter.Name, true);
                        while (!reciprocateCancellationToken.IsCancellationRequested)
                        {
                            if (motionCardService.CylinderArrviedCheckWithCancellationEx(cancellationTokenSource, parameter.Name, true))
                            {
                                break;
                            }
                            Thread.Sleep(100);
                        }
                        await Task.Delay(500, reciprocateCancellationToken.Token);

                        motionCardService.CylinderControlEx(parameter.Name, false);
                        while (!reciprocateCancellationToken.IsCancellationRequested)
                        {
                            if (motionCardService.CylinderArrviedCheckWithCancellationEx(cancellationTokenSource, parameter.Name, false))
                            {
                                break;
                            }
                            Thread.Sleep(100);
                        }
                        await Task.Delay(500, reciprocateCancellationToken.Token);
                    }
                }
                catch (TaskCanceledException)
                {
                    logService.WriteLog(LogTypes.DB.ToString(), $@"气缸{parameter.Name}往复运动取消", MessageDegree.INFO);
                }
                catch (Exception ex)
                {
                    logService.WriteLog(LogTypes.DB.ToString(), $@"气缸{parameter.Name}往复运动异常:{ex.Message}", ex);
                }
            }, reciprocateCancellationToken.Token);
            ReciprocateEnable = true;
        }

        private DelegateCommand<CylinderMonitorModel> _ReciprocateStopCommand;
        public DelegateCommand<CylinderMonitorModel> ReciprocateStopCommand =>
            _ReciprocateStopCommand ?? (_ReciprocateStopCommand = new DelegateCommand<CylinderMonitorModel>(ExecuteReciprocateStopCommand));

        void ExecuteReciprocateStopCommand(CylinderMonitorModel parameter)
        {
            reciprocateCancellationToken?.Cancel();
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            CylinderGroupCollection.Clear();
            GetCylinderGroups();
            GetCylinderInfo();
            cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    Thread.Sleep(200);
                    try
                    {
                        lock (cylinderChangeLock)
                        {
                            foreach (var cyliner in CylinderMonitorCollection)
                            {
                                if (cyliner.SingleValveOutputInfo != null)
                                    cyliner.SingleValveOutputInfo.Status = MotionControlResource.IOOutputResource[cyliner.SingleValveOutputInfo.Name].Status;
                                if (cyliner.DualValveOriginOutputInfo != null)
                                    cyliner.DualValveOriginOutputInfo.Status = MotionControlResource.IOOutputResource[cyliner.DualValveOriginOutputInfo.Name].Status;
                                if (cyliner.DualValveMovingOutputInfo != null)
                                    cyliner.DualValveMovingOutputInfo.Status = MotionControlResource.IOOutputResource[cyliner.DualValveMovingOutputInfo.Name].Status;
                                if (cyliner.SensorOriginInputInfo != null)
                                    cyliner.SensorOriginInputInfo.Status = MotionControlResource.IOInputResource[cyliner.SensorOriginInputInfo.Name].Status;
                                if (cyliner.SensorMovingInputInfo != null)
                                    cyliner.SensorMovingInputInfo.Status = MotionControlResource.IOInputResource[cyliner.SensorMovingInputInfo.Name].Status;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logService.WriteLog(LogTypes.DB.ToString(), $@"气缸调试页面刷新状态线程异常:{ex.Message}", ex);
                    }
                }
            }, cancellationTokenSource.Token);
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}进入气缸调试页面", MessageDegree.INFO);
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            cancellationTokenSource?.Cancel();
            reciprocateCancellationToken?.Cancel();
        }

        private void GetCylinderInfo()
        {
            CylinderMonitorCollection.Clear();
            var cylinderMonitor = dataBaseService.Db.Queryable<CylinderInfoEntity>()
                .Includes(x => x.SingleValveOutputInfo)
                .Includes(x => x.DualValveOriginOutputInfo)
                .Includes(x => x.DualValveMovingOutputInfo)
                .Includes(x => x.SensorOriginInputInfo)
                .Includes(x => x.SensorMovingInputInfo)
                .OrderBy(x => x.Number)
                .OrderBy(x => x.Id)
                .ToList();
            mapper.Map(cylinderMonitor, CylinderMonitorCollection);
        }

        private void GetCylinderGroups()
        {
            var cylinderGroup = dataBaseService.Db.Queryable<CylinderInfoEntity>().Distinct().Select(x => x.Group).ToList();
            cylinderGroup.ForEach(x => { if (!string.IsNullOrWhiteSpace(x) && !CylinderGroupCollection.Contains(x)) CylinderGroupCollection.Add(x); });
        }
    }
}
