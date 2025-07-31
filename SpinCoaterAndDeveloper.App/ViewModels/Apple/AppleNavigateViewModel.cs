using AutoMapper;
using DataBaseServiceInterface;
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
using PrismAutomationPlatformExt;
using SpinCoaterAndDeveloper.App.Extensions;
using SpinCoaterAndDeveloper.App.Views;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using SpinCoaterAndDeveloper.Shared.Event;
using SpinCoaterAndDeveloper.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class AppleNavigateViewModel : BindableBase
    {
        private readonly IRegionManager regionManager;
        private readonly IMapper mapper;
        private readonly IPermissionService permissionService;
        private readonly ISnackbarMessageQueue snackbarMessageQueue;
        private readonly IDialogHostService dialogHostService;
        private readonly ILogService logService;
        private readonly IMotionCardService motionCardService;
        private readonly IDataBaseService dataBaseService;
        private readonly IEventAggregator eventAggregator;
        private readonly IDialogWithNoParentService dialogWithNoParentService;
        private CancellationTokenSource cancellationTokenSource;
        private CancellationTokenSource machineOpBtnEnableCancellationToken;

        #region Binding
        private AppleView appleCurrentView = AppleView.Home;
        public AppleView AppleCurrentView
        {
            get { return appleCurrentView; }
            set { SetProperty(ref appleCurrentView, value); }
        }

        private PermissionLevel permissionSts;
        public PermissionLevel PermissionSts
        {
            get { return permissionSts; }
            set { SetProperty(ref permissionSts, value); }
        }

        private string machineSts = FSMStateCode.PowerUpping.TryFindResourceEx();
        public string MachineSts
        {
            get { return machineSts; }
            set { SetProperty(ref machineSts, value); }
        }

        private bool machineOperationButtonEnable = false;
        public bool MachineOperationButtonEnable
        {
            get { return machineOperationButtonEnable; }
            set { SetProperty(ref machineOperationButtonEnable, value); }
        }

        private bool mcMonitorAndDebugEnable = true;
        public bool MCMonitorAndDebugEnable
        {
            get { return mcMonitorAndDebugEnable; }
            set { SetProperty(ref mcMonitorAndDebugEnable, value); }
        }

        private bool _ActuationMonitorButtonEnable = true;
        public bool ActuationMonitorButtonEnable
        {
            get { return _ActuationMonitorButtonEnable; }
            set { SetProperty(ref _ActuationMonitorButtonEnable, value); }
        }
        #endregion
        public AppleNavigateViewModel(IContainerProvider containerProvider)
        {
            this.mapper = containerProvider.Resolve<IMapper>();
            this.permissionService = containerProvider.Resolve<IPermissionService>();
            this.snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();
            this.dialogHostService = containerProvider.Resolve<IDialogHostService>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.motionCardService = containerProvider.Resolve<IMotionCardService>();
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
            this.eventAggregator = containerProvider.Resolve<IEventAggregator>();
            this.dialogWithNoParentService = containerProvider.Resolve<IDialogWithNoParentService>();
            this.regionManager = containerProvider.Resolve<IRegionManager>();

            //注册设备状态变更事件
            eventAggregator.RegisterMachineStsChangeEvent(x => MachineSts = x.Message.TryFindResourceEx());
            //注册语言切换事件
            eventAggregator.GetEvent<MessageEvent>().Subscribe(x => { MachineSts = GlobalValues.MachineStatus.TryFindResourceEx(); }, ThreadOption.UIThread, true, y => y.Filter.Equals("LanguageChangeNavigate"));
            //注册快捷键登录事件
            eventAggregator.GetEvent<MessageEvent>().Subscribe(x =>
            {
                switch (x.Message)
                {
                    case "QuickLogin_Dev":
                        if (permissionService.CheckPermission(PermissionLevel.Developer)) return;
                        permissionService.CurrentUserName = "QuickDev";
                        permissionService.CurrentPermission = PermissionLevel.Developer;
                        logService.WriteLog(LogTypes.DB.ToString(), $@"快捷登录Dev用户", MessageDegree.INFO);
                        break;
                    case "TimerLoginOut":
                        permissionService.CurrentUserName = "";
                        permissionService.CurrentPermission = PermissionLevel.NoPermission;
                        logService.WriteLog(LogTypes.DB.ToString(), $@"定时权限自动退出", MessageDegree.INFO);
                        break;
                    default:
                        break;
                }
            }, ThreadOption.UIThread, true, m => { return m.Filter.Equals("QuickLoginOrOut"); });
            //注册切换到主页面事件
            eventAggregator.GetEvent<MessageEvent>().Subscribe(x =>
            {
                if (AppleCurrentView == AppleView.Home) return;
                logService.WriteLog(LogTypes.DB.ToString(), $@"自动切换到主页面", MessageDegree.INFO);
                regionManager.RequestNavigate("MainViewRegion", nameof(AppleIndexView), y => { if (y.Result.HasValue && (bool)y.Result) AppleCurrentView = AppleView.Home; });
            }, ThreadOption.UIThread, true, m => { return m.Filter.Equals("ChangeToHomeView"); });
            //板卡启动成功,按钮才可以操作
            machineOpBtnEnableCancellationToken = new CancellationTokenSource();
            Task.Run(() =>
            {
                while (!machineOpBtnEnableCancellationToken.IsCancellationRequested)
                {
                    if (motionCardService.InitSuccess)
                    {
                        Application.Current.Dispatcher.Invoke(() => { MachineOperationButtonEnable = true; });
                        break;
                    }
                    Thread.Sleep(200);
                }
            }, machineOpBtnEnableCancellationToken.Token);

            //启动刷新Apple导航图标状态线程
            cancellationTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(() =>
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        PermissionSts = permissionService.CurrentPermission;
                        //如果AppleLog日志队列中有日志
                        if (GlobalValues.AppleLogQueue.Any())
                        {
                            var count = GlobalValues.AppleLogQueue.Count;
                            for (int i = 0; i < count; i++)
                            {
                                GlobalValues.AppleLogQueue.TryDequeue(out LogMsgEventArgs log);

                                FileStream fs = null;
                                StreamWriter sw = null;
                                try
                                {
                                    //Apple Hive Machine Error Log
                                    if (log != null && log.Message.KeyWord.Contains(LogTypes.AppleHiveMachineError.ToString()))
                                    {
                                        string fileName = string.Format("hour_{0}.txt", DateTime.Now.ToString("HH"));
                                        string filePath = Convert.ToBoolean(ConfigurationManager.AppSettings["AppleRemoteQualificationEnable"]) ? AppleHiveLogPath.HiveMachineErrorWithRemoteQualification + DateTime.Now.ToString("yyyyMMdd") : AppleHiveLogPath.HiveMcahineError + DateTime.Now.ToString("yyyyMMdd");
                                        if (!Directory.Exists(filePath))
                                            Directory.CreateDirectory(filePath);
                                        string fullFileName = Path.Combine(filePath, fileName);
                                        fs = new FileStream(fullFileName, FileMode.Append, FileAccess.Write, FileShare.Read);
                                        sw = new StreamWriter(fs, Encoding.Default);
                                        sw.WriteLine($"{log.Message.Time:HH:mm:ss}-->{log.Message.Text}");
                                        continue;
                                    }
                                    //Apple Hive Machine Data Log
                                    if (log != null && log.Message.KeyWord.Contains(LogTypes.AppleHiveMachineData.ToString()))
                                    {
                                        string fileName = string.Format("hour_{0}.txt", DateTime.Now.ToString("HH"));
                                        string filePath = Convert.ToBoolean(ConfigurationManager.AppSettings["AppleRemoteQualificationEnable"]) ? AppleHiveLogPath.HiveMachineDataWithRemoteQualification + DateTime.Now.ToString("yyyyMMdd") : AppleHiveLogPath.HiveMcahineData + DateTime.Now.ToString("yyyyMMdd");
                                        if (!Directory.Exists(filePath))
                                            Directory.CreateDirectory(filePath);
                                        string fullFileName = Path.Combine(filePath, fileName);
                                        fs = new FileStream(fullFileName, FileMode.Append, FileAccess.Write, FileShare.Read);
                                        sw = new StreamWriter(fs, Encoding.Default);
                                        sw.WriteLine($"{log.Message.Time:HH:mm:ss}-->{log.Message.Text}");
                                        continue;
                                    }
                                    //Apple Hive Machine State
                                    if (log != null && log.Message.KeyWord.Contains(LogTypes.AppleHiveMachieState.ToString()))
                                    {
                                        string fileName = string.Format("{0}.txt", DateTime.Now.ToString("yyyyMMdd"));
                                        string filePath = Convert.ToBoolean(ConfigurationManager.AppSettings["AppleRemoteQualificationEnable"]) ? AppleHiveLogPath.HiveMachineStateWithRemoteQualification : AppleHiveLogPath.HiveMcahineState;
                                        if (!Directory.Exists(filePath))
                                            Directory.CreateDirectory(filePath);
                                        string fullFileName = Path.Combine(filePath, fileName);
                                        fs = new FileStream(fullFileName, FileMode.Append, FileAccess.Write, FileShare.Read);
                                        sw = new StreamWriter(fs, Encoding.Default);
                                        sw.WriteLine($"{log.Message.Time:HH:mm:ss}-->{log.Message.Text}");
                                        continue;
                                    }
                                    //Apple Machine Log
                                    if (log != null && log.Message.KeyWord.Contains(LogTypes.AppleMachineLog.ToString()))
                                    {
                                        string fileName = string.Format("hour_{0}.txt", DateTime.Now.ToString("HH"));
                                        string filePath = AppleHiveLogPath.AppleMachineLog + DateTime.Now.ToString("yyyyMMdd");
                                        if (!Directory.Exists(filePath))
                                            Directory.CreateDirectory(filePath);
                                        string fullFileName = Path.Combine(filePath, fileName);
                                        fs = new FileStream(fullFileName, FileMode.Append, FileAccess.Write, FileShare.Read);
                                        sw = new StreamWriter(fs, Encoding.Default);
                                        sw.WriteLine($"{log.Message.Time:yyyy/MM/dd HH:mm:ss:fff}:{log.Message.Text}");
                                        continue;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    logService.WriteLog(LogTypes.DB.ToString(), $"Apple Hive日志记录失败{ex.Message}", ex);
                                }
                                finally
                                {
                                    sw?.Close();
                                    fs?.Close();
                                }
                            }
                        }
                        Thread.Sleep(300);
                    }
                    catch (Exception ex)
                    {
                        logService.WriteLog(LogTypes.AppleMachineLog.ToString(), $"Apple Navigate刷新线程异常:{ex.Message}", ex);
                    }
                }
            }, cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private DelegateCommand appleUserLoginOutCommand;
        public DelegateCommand AppleUserLoginOutCommand =>
            appleUserLoginOutCommand ?? (appleUserLoginOutCommand = new DelegateCommand(ExecuteAppleUserLoginOutCommand));

        async void ExecuteAppleUserLoginOutCommand()
        {
            if (PermissionSts != PermissionLevel.NoPermission)
            {
                permissionService.CurrentPermission = PermissionLevel.NoPermission;
                permissionService.CurrentUserName = "";
                snackbarMessageQueue.EnqueueEx("登出成功!");
                return;
            }

            var dialogResult = await dialogHostService.ShowHostDialog("DialogHostLoginView", null, "Root");
            if (dialogResult.Result == Prism.Services.Dialogs.ButtonResult.OK)
            {
                UserInfoEntity user = dialogResult.Parameters.GetValue<UserInfoEntity>("user");
                permissionService.CurrentUserName = user.UserName;
                switch (user.Authority)
                {
                    case "管理员":
                        permissionService.CurrentPermission = PermissionLevel.Admin;
                        logService.WriteLog(LogTypes.DB.ToString(), $"用户{user.UserName}登陆管理员账户", MessageDegree.INFO);
                        snackbarMessageQueue.EnqueueEx("登录Level 2成功!");
                        break;
                    case "操作员":
                        permissionService.CurrentPermission = PermissionLevel.Operator;
                        logService.WriteLog(LogTypes.DB.ToString(), $"用户{user.UserName}登陆操作员账户", MessageDegree.INFO);
                        snackbarMessageQueue.EnqueueEx("登录Level 1成功!");
                        break;
                    case "开发人员":
                        permissionService.CurrentPermission = PermissionLevel.Developer;
                        logService.WriteLog(LogTypes.DB.ToString(), $"用户{user.UserName}登陆开发人员账户", MessageDegree.INFO);
                        snackbarMessageQueue.EnqueueEx("登录Level 3成功!");
                        break;
                    default:
                        permissionService.CurrentPermission = PermissionLevel.NoPermission;
                        permissionService.CurrentUserName = "未知用户";
                        break;
                }
            }
        }

        private DelegateCommand<string> appleNavigateCommand;
        public DelegateCommand<string> AppleNavigateCommand =>
            appleNavigateCommand ?? (appleNavigateCommand = new DelegateCommand<string>(ExecuteAppleNavigateCommand));

        void ExecuteAppleNavigateCommand(string parameter)
        {
            if (parameter == AppleCurrentView.ToString()) return;
            //AppleCurrentView赋值
            switch (parameter)
            {
                case "Home":
                    regionManager.RequestNavigate("MainViewRegion", nameof(AppleIndexView), x => { if (x.Result.HasValue && (bool)x.Result) AppleCurrentView = AppleView.Home; });
                    break;
                case "Alarm":
                    regionManager.RequestNavigate("MainViewRegion", nameof(AppleAlarmView), x => { if (x.Result.HasValue && (bool)x.Result) AppleCurrentView = AppleView.Alarm; });
                    break;
                case "Config":
                    regionManager.RequestNavigate("MainViewRegion", nameof(AppleConfigView), x => { if (x.Result.HasValue && (bool)x.Result) AppleCurrentView = AppleView.Config; });
                    break;
                case "Data":
                    regionManager.RequestNavigate("MainViewRegion", nameof(AppleDataView), x => { if (x.Result.HasValue && (bool)x.Result) AppleCurrentView = AppleView.Data; });
                    break;
                case "Vision":
                    regionManager.RequestNavigate("MainViewRegion", nameof(AppleVisionView), x => { if (x.Result.HasValue && (bool)x.Result) AppleCurrentView = AppleView.Vision; });
                    break;
                case "Setting":
                    regionManager.RequestNavigate("MainViewRegion", nameof(AppleSettingView), x => { if (x.Result.HasValue && (bool)x.Result) AppleCurrentView = AppleView.Setting; });
                    break;
                default:
                    break;
            }
        }

        private DelegateCommand<string> machineOperationCommand;
        public DelegateCommand<string> MachineOperationCommand =>
            machineOperationCommand ?? (machineOperationCommand = new DelegateCommand<string>(ExecuteMachineOperationCommand).ObservesCanExecute(() => MachineOperationButtonEnable));

        void ExecuteMachineOperationCommand(string parameter)
        {
            MachineOperationButtonEnable = false;
            switch (parameter)
            {
                case "StartMachine":
                    logService.WriteLog(LogTypes.AppleMachineLog.ToString(), "用户点击启动按钮", MessageDegree.INFO);
                    GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.StartUp));
                    break;
                case "StopMachine":
                    logService.WriteLog(LogTypes.AppleMachineLog.ToString(), "用户点击停止按钮", MessageDegree.INFO);
                    GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.Stop));
                    break;
                case "EMStopMachine":
                    logService.WriteLog(LogTypes.AppleMachineLog.ToString(), "用户点击急停按钮", MessageDegree.INFO);
                    GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                    break;
                default:
                    break;
            }
            MachineOperationButtonEnable = true;
        }

        private DelegateCommand _ActuationMonitorCommand;
        public DelegateCommand ActuationMonitorCommand =>
            _ActuationMonitorCommand ?? (_ActuationMonitorCommand = new DelegateCommand(ExecuteActuationMonitorCommand).ObservesCanExecute(() => ActuationMonitorButtonEnable));

        void ExecuteActuationMonitorCommand()
        {
            if (!permissionService.CheckPermission(PermissionLevel.Admin))
            {
                snackbarMessageQueue.EnqueueEx("请登录有权限账户");
                return;
            }
            ActuationMonitorButtonEnable = false;
            dialogWithNoParentService.ShowWithNoParent(nameof(ActuationMonitorView), null, r =>
            {
                ActuationMonitorButtonEnable = true;
                logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}离开逻辑指令监视页面", MessageDegree.INFO);
            });
        }

        ~AppleNavigateViewModel()
        {
            cancellationTokenSource?.Cancel();
            machineOpBtnEnableCancellationToken?.Cancel();
        }
    }
}
