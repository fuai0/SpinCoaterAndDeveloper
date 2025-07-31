using DataBaseServiceInterface;
using DryIoc;
using FSM;
using LogServiceInterface;
using MaterialDesignThemes.Wpf;
using MotionCardServiceInterface;
using PermissionServiceInterface;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using PrismAutomationPlatformExt;
using SpinCoaterAndDeveloper.Actuation.Actuation;
using SpinCoaterAndDeveloper.App.Extensions;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.Extensions;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class IndexViewModel : BindableBase, IConfirmNavigationRequest
    {
        private readonly IDialogWithNoParentService dialogWithNoParentService;
        private readonly IPermissionService permissionService;
        private readonly ILogService logService;
        private readonly IMotionCardService motionCardService;
        private readonly IEventAggregator eventAggregator;
        private readonly ISnackbarMessageQueue snackbarMessageQueue;
        private readonly ActuationRunningGroupManager actuationRunningGroupManager;
        private CancellationTokenSource MachineOpBtnEnableCancellationToken = null;

        #region Binding
        private string _MachineStatus = FSMStateCode.PowerUpping;
        public string MachineStatus
        {
            get { return _MachineStatus; }
            set { SetProperty(ref _MachineStatus, value); }
        }
        private bool mcMonitorAndDebugEnable = true;
        /// <summary>
        /// 打开运动控制调试页面按钮是否可用
        /// </summary>
        public bool MCMonitorAndDebugEnable
        {
            get { return mcMonitorAndDebugEnable; }
            set { SetProperty(ref mcMonitorAndDebugEnable, value); }
        }
        private string softwareVersion;
        /// <summary>
        /// 软件版本号
        /// </summary>
        public string SoftwareVersion
        {
            get { return softwareVersion; }
            set { SetProperty(ref softwareVersion, value); }
        }
        private string softwareCreateTime;
        /// <summary>
        /// 软件编译时间
        /// </summary>
        public string SoftwareCreateTime
        {
            get { return softwareCreateTime; }
            set { SetProperty(ref softwareCreateTime, value); }
        }
        private bool machineOperationButtonEnable = false;
        /// <summary>
        /// 复位，启动，停止，急停按钮是否可用
        /// </summary>
        public bool MachineOperationButtonEnable
        {
            get { return machineOperationButtonEnable; }
            set { SetProperty(ref machineOperationButtonEnable, value); }
        }

        private bool singleStepBtnEnable = false;
        /// <summary>
        /// 单步执行按钮是否启用
        /// </summary>
        public bool SingleStepBtnEnable
        {
            get { return singleStepBtnEnable; }
            set { SetProperty(ref singleStepBtnEnable, value); }
        }
        #endregion
        public IndexViewModel(IContainerProvider containerProvider)
        {
            this.dialogWithNoParentService = containerProvider.Resolve<IDialogWithNoParentService>();
            this.permissionService = containerProvider.Resolve<IPermissionService>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.motionCardService = containerProvider.Resolve<IMotionCardService>();
            this.actuationRunningGroupManager = containerProvider.Resolve<ActuationRunningGroupManager>();
            this.snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();
            this.eventAggregator = containerProvider.Resolve<IEventAggregator>();

            //版本号
            SoftwareVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            //版本编译时间
            SoftwareCreateTime = System.IO.File.GetLastWriteTime(this.GetType().Assembly.Location).ToString();
            //注册设备状态变更事件
            eventAggregator.RegisterMachineStsChangeEvent(x => MachineStatus = x.Message);

            //板卡启动成功，按钮才可操作
            MachineOpBtnEnableCancellationToken = new CancellationTokenSource();
            Task.Run(() =>
            {
                while (!MachineOpBtnEnableCancellationToken.IsCancellationRequested)
                {
                    if (motionCardService.InitSuccess)
                    {
                        Application.Current.Dispatcher.Invoke(() => { MachineOperationButtonEnable = true; });
                        break;
                    }
                    Thread.Sleep(200);
                }
            }, MachineOpBtnEnableCancellationToken.Token);
        }

        private DelegateCommand<string> machineOperationCommand;
        public DelegateCommand<string> MachineOperationCommand =>
            machineOperationCommand ?? (machineOperationCommand = new DelegateCommand<string>(ExecuteMachineOperationCommand).ObservesCanExecute(() => MachineOperationButtonEnable));
        //设备操作按钮操作逻辑
        void ExecuteMachineOperationCommand(string parameter)
        {
            MachineOperationButtonEnable = false;
            switch (parameter)
            {
                case "ResetMachine":
                    logService.WriteLog(LogTypes.DB.ToString(), "用户点击复位按钮", MessageDegree.INFO);
                    GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.Reset));
                    break;
                case "StartMachine":
                    logService.WriteLog(LogTypes.DB.ToString(), "用户点击启动按钮", MessageDegree.INFO);
                    GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.StartUp));
                    break;
                case "StopMachine":
                    logService.WriteLog(LogTypes.DB.ToString(), "用户点击停止按钮", MessageDegree.INFO);
                    GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.Stop));
                    break;
                case "EMStopMachine":
                    logService.WriteLog(LogTypes.DB.ToString(), "用户点击急停按钮", MessageDegree.INFO);
                    GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                    break;
                default:
                    break;
            }
            MachineOperationButtonEnable = true;
        }

        private DelegateCommand enterBurnInModeCommand;
        public DelegateCommand EnterBurnInModeCommand =>
            enterBurnInModeCommand ?? (enterBurnInModeCommand = new DelegateCommand(ExecuteBurnInchangeCommand));
        //进入空跑模式
        void ExecuteBurnInchangeCommand()
        {
            GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EnterBurnIn));
        }
        private DelegateCommand leaveBurnInModeCommand;
        public DelegateCommand LeaveBurnInModeCommand =>
            leaveBurnInModeCommand ?? (leaveBurnInModeCommand = new DelegateCommand(ExecuteLeaveBurnInModeCommand));
        //离开空跑
        void ExecuteLeaveBurnInModeCommand()
        {
            if (GlobalValues.MachineStatus != FSMStateCode.BurnInTesting)
            {
                snackbarMessageQueue.EnqueueEx("当前状态不在BurnInTesting状态,无法离开BrunIn.可使用急停停止BurnInTesting.");
                return;
            }
            GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.LeaveBurnIn));
        }
        private DelegateCommand machineOperationSingleStepCommand;
        public DelegateCommand MachineOperationSingleStepCommand =>
            machineOperationSingleStepCommand ?? (machineOperationSingleStepCommand = new DelegateCommand(ExecuteMachineOperationSingleStepCommand).ObservesCanExecute(() => SingleStepBtnEnable));
        //单步命令
        void ExecuteMachineOperationSingleStepCommand()
        {
            actuationRunningGroupManager.ProcessByStepRunOneStep();
        }

        private DelegateCommand startSingleStepDebugCommand;
        public DelegateCommand StartSingleStepDebugCommand =>
            startSingleStepDebugCommand ?? (startSingleStepDebugCommand = new DelegateCommand(ExecuteSingleStepCheckChangeCommand));
        //仅支持生产过程单步调试
        void ExecuteSingleStepCheckChangeCommand()
        {
            if (GlobalValues.MachineStatus == FSMStateCode.Running)
            {
                //设置单步启用
                actuationRunningGroupManager.SetProcessByStep(true);
                SingleStepBtnEnable = true;
            }
            else
            {
                snackbarMessageQueue.EnqueueEx("单步模式仅支持生产模式单步");
            }
        }
        private DelegateCommand stopSingleStepDebugCommand;
        public DelegateCommand StopSingleStepDebugCommand =>
            stopSingleStepDebugCommand ?? (stopSingleStepDebugCommand = new DelegateCommand(ExecuteStopSingleStepDebugCommand).ObservesCanExecute(() => SingleStepBtnEnable));

        void ExecuteStopSingleStepDebugCommand()
        {
            //单步取消继续运行
            actuationRunningGroupManager.ProcessByStepResume();
            SingleStepBtnEnable = false;
        }

        public void ConfirmNavigationRequest(NavigationContext navigationContext, Action<bool> continuationCallback)
        {
            //导航时,确认是否有权限,无权限,则不能导航
            //if (navigationContext.Uri.OriginalString == "mcspacepointconfigview")
            //{
            //    if (!permissionService.CheckPermission(PermissionLevel.Admin))
            //    {
            //        eventAggregator.SendMessage("请登陆有权限账户!");
            //        continuationCallback(false);
            //        return;
            //    }
            //}
            continuationCallback(true);
        }

        //AppleUI启用时,因加载顺序问题,未使用导航加载此页面到左抽屉栏,故导航相关函数不起作用
        public void OnNavigatedTo(NavigationContext navigationContext)
        {

        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {

        }

        ~IndexViewModel()
        {
            MachineOpBtnEnableCancellationToken?.Cancel();
        }
    }
}
