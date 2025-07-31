using AutoMapper;
using DataBaseServiceInterface;
using FSM;
using LogServiceInterface;
using MaterialDesignThemes.Wpf;
using MotionCardServiceInterface;
using MotionControlActuation;
using PermissionServiceInterface;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using PrismAutomationPlatformExt;
using SpinCoaterAndDeveloper.App.Common.Models;
using SpinCoaterAndDeveloper.App.Extensions;
using SpinCoaterAndDeveloper.App.Views;
using SpinCoaterAndDeveloper.App.Views.Dialogs;
using SpinCoaterAndDeveloper.FSM.FSM;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using SpinCoaterAndDeveloper.Shared.Event;
using SpinCoaterAndDeveloper.Shared.Extensions;
using SpinCoaterAndDeveloper.Shared.Services.MotionResourceInitService;
using SpinCoaterAndDeveloper.Shared.Services.MouseIdelTimeService;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class MainWindowViewModel : BindableBase, IConfigureService
    {
        private int sameLogRepeatTimes = 0;
        private MessageItem previousLog = new MessageItem();
        private readonly IRegionManager regionManager;
        private readonly IEventAggregator eventAggregator;
        private readonly IDialogHostService dialogHostService;
        private readonly IContainerProvider containerProvider;
        private readonly ISnackbarMessageQueue snackbarMessageQueue;
        private IMapper mapper;
        private ILogService logService;
        private IDialogService dialogService;
        private IDataBaseService dataBaseService;
        private IMotionCardService motionCardService;
        private IPermissionService permissionService;
        private IMouseIdleTime mouseIdleTime;

        private MachineStateMachine machineState;
        private CancellationTokenSource cancellationTokenInputsAndAxisRefresh = null;
        private CancellationTokenSource cancellationTokenOperationCommand = null;
        public ISnackbarMessageQueue GlobalMessageQueue { get; set; }

        #region Property
        private string mainTitle;
        public string MainTitle
        {
            get { return mainTitle; }
            set { SetProperty(ref mainTitle, value); }
        }

        private ObservableCollection<MenuBar> menuBars;
        public ObservableCollection<MenuBar> MenuBars
        {
            get { return menuBars; }
            set { SetProperty(ref menuBars, value); }
        }

        private string userName = "DefaultUserName".TryFindResourceEx();
        public string UserName
        {
            get { return userName; }
            set { SetProperty(ref userName, value); }
        }
        private bool leftDrawerOpen;

        public bool LeftDrawerOpen
        {
            get { return leftDrawerOpen; }
            set
            {
                SetProperty(ref leftDrawerOpen, value);
                eventAggregator.GetEvent<NavigateSyncUpEvent>().Publish(new NavigateSyncModel() { IsOpen = value, Fliter = "NavigateSync" });
            }
        }
        private bool appleUIEnabel;
        public bool AppleUIEnable
        {
            get { return appleUIEnabel; }
            set { SetProperty(ref appleUIEnabel, value); }
        }
        #endregion

        public MainWindowViewModel(IContainerProvider containerProvider)
        {
            //Modules未加载,仅能取到默认服务及主程序中注册服务
            this.containerProvider = containerProvider;
            this.mapper = containerProvider.Resolve<IMapper>();
            this.regionManager = containerProvider.Resolve<IRegionManager>();
            this.dialogService = containerProvider.Resolve<IDialogService>();
            this.eventAggregator = containerProvider.Resolve<IEventAggregator>();
            this.dialogHostService = containerProvider.Resolve<IDialogHostService>();
            this.GlobalMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();
            this.snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();

            MainTitle = Convert.ToBoolean(ConfigurationManager.AppSettings["AppleUIEnable"]) ? $"CETC_{Assembly.GetExecutingAssembly().GetName().Version}_{System.IO.File.GetLastWriteTime(this.GetType().Assembly.Location):yyMMdd}" : "MainWindowTitle".TryFindResourceEx();

            //注册打开导航栏同步事件
            eventAggregator.GetEvent<NavigateSyncDownEvent>().Subscribe(x => { LeftDrawerOpen = x.IsOpen; }, ThreadOption.UIThread, true, x => x.Fliter.Equals("NavigateSync"));
            //注册用户登录修改名字事件
            eventAggregator.RegisterPermissionChangeEvent(x => { UserName = x.Message; });
        }

        #region CloseWindow
        private DelegateCommand<CancelEventArgs> windowClosingCommand;
        public DelegateCommand<CancelEventArgs> WindowClosingCommand =>
            windowClosingCommand ?? (windowClosingCommand = new DelegateCommand<CancelEventArgs>(ExecuteWindowClosingCommand));

        void ExecuteWindowClosingCommand(CancelEventArgs parameter)
        {
            //关闭窗口,弹窗确认
#if DEBUG
            var windows = Application.Current.Windows.OfType<DialogMessageWindow>().ToList();
            windows.ForEach(item => item.Close());
            parameter.Cancel = false;
#else
            var dialogResult = dialogService.QuestionShowDialog("提示", "确认退出系统?");
            if (dialogResult != ButtonResult.Yes)
            {
                parameter.Cancel = true;
                return;
            }
            else
            {
                var windows = Application.Current.Windows.OfType<DialogMessageWindow>().ToList();
                windows.ForEach(item => item.Close());
                parameter.Cancel = false;
            }
#endif
        }
        #endregion

        #region Navigate
        private DelegateCommand<MenuBar> navigateCommand;
        public DelegateCommand<MenuBar> NavigateCommand =>
            navigateCommand ?? (navigateCommand = new DelegateCommand<MenuBar>(ExecuteNavigateCommand));

        //侧边栏导航
        void ExecuteNavigateCommand(MenuBar obj)
        {
            if (obj == null) return;
            regionManager.RequestNavigate("MainViewRegion", obj.NameSpace);
            LeftDrawerOpen = false;
        }
        #endregion

        public void ConfigureAsync()
        {
            //函数内所有内容为异步执行,若需要弹窗需要使用跨线程
            try
            {
                #region 弹窗服务
                eventAggregator.SendLaunchInfo("初始化弹窗服务");
                dialogService = containerProvider.Resolve<IDialogService>();
                #endregion

                #region 初始化日志记录
                eventAggregator.SendLaunchInfo("初始化日志服务");
                logService = containerProvider.Resolve<ILogService>();
#if DEBUG
                logService.ConsoleOutput = true;
#endif
                switch (ConfigurationManager.AppSettings["MessageDegree"])
                {
                    case "DEBUG":
                        logService.SetMessageDegree(MessageDegree.DEBUG);
                        break;
                    case "INFO":
                        logService.SetMessageDegree(MessageDegree.INFO);
                        break;
                    case "WARN":
                        logService.SetMessageDegree(MessageDegree.WARN);
                        break;
                    case "ERROR":
                        logService.SetMessageDegree(MessageDegree.ERROR);
                        break;
                    case "FATAL":
                        logService.SetMessageDegree(MessageDegree.FATAL);
                        break;
                    default:
                        logService.SetMessageDegree(MessageDegree.DEBUG);
                        break;
                }

                //遍历服务并注册日志事件
                IConfigurationStore moduleStore = new ConfigurationStore();
                ModulesConfigurationSection modules = moduleStore.RetrieveModuleConfigurationSection();
                foreach (ModuleConfigurationElement module in modules.Modules)
                {
                    Assembly assembly = Assembly.Load(module.ModuleName);
                    Type[] types = assembly.GetTypes();
                    foreach (Type type in types)
                    {
                        EventInfo eventInfo = type.GetEvent("LogServiceEvent");
                        if (eventInfo != null)
                        {
                            eventAggregator.SendLaunchInfo($"{type} {"注册日志服务".TryFindResourceEx()}");
                            var handler = new Action<string, string, Exception>((level, text, ex) =>
                            {
                                if (ex != null)
                                    logService.WriteLog(LogTypes.DB.ToString(), text, ex);

                                switch (level)
                                {
                                    case "DEBUG":
                                        logService.WriteLog(LogTypes.DB.ToString(), text, MessageDegree.DEBUG);
                                        break;
                                    case "INFO":
                                        logService.WriteLog(LogTypes.DB.ToString(), text, MessageDegree.INFO);
                                        break;
                                    case "WARN":
                                        logService.WriteLog(LogTypes.DB.ToString(), text, MessageDegree.WARN);
                                        break;
                                    case "ERROR":
                                        logService.WriteLog(LogTypes.DB.ToString(), text, MessageDegree.ERROR);
                                        break;
                                    case "FATAL":
                                        logService.WriteLog(LogTypes.DB.ToString(), text, MessageDegree.FATAL);
                                        break;
                                    default:
                                        logService.WriteLog(LogTypes.DB.ToString(), text, MessageDegree.INFO);
                                        break;
                                }
                            });
                            eventInfo.AddEventHandler(type, handler);
                            logService.WriteLog(LogTypes.DB.ToString(), $"{type}模块注册日志服务", MessageDegree.INFO);
                        }
                    }
                }
                //注册逻辑控制指令集日志事件
                MotionControlActuation.LogExtension.LogServiceEvent += (level, text, ex) =>
                {
                    if (ex != null)
                        logService.WriteLog(LogTypes.DB.ToString(), text, ex);

                    switch (level)
                    {
                        case "DEBUG":
                            logService.WriteLog(LogTypes.DB.ToString(), text, MessageDegree.DEBUG);
                            break;
                        case "INFO":
                            logService.WriteLog(LogTypes.DB.ToString(), text, MessageDegree.INFO);
                            break;
                        case "WARN":
                            logService.WriteLog(LogTypes.DB.ToString(), text, MessageDegree.WARN);
                            break;
                        case "ERROR":
                            logService.WriteLog(LogTypes.DB.ToString(), text, MessageDegree.ERROR);
                            break;
                        case "FATAL":
                            logService.WriteLog(LogTypes.DB.ToString(), text, MessageDegree.FATAL);
                            break;
                        default:
                            logService.WriteLog(LogTypes.DB.ToString(), text, MessageDegree.INFO);
                            break;
                    }
                };
                #endregion

                #region 数据库初始化
                eventAggregator.SendLaunchInfo("初始化数据库服务");
                dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
                dataBaseService.ShowSQLStringEvent += (s => { System.Diagnostics.Debug.WriteLine(s); });
                //配置文件中自动创建表AutoCreateDB为True则创建数据库及表
                if (Convert.ToBoolean(ConfigurationManager.AppSettings["AutoCreateDB"]))
                {
                    //创建数据库
                    dataBaseService.CreateDB();
                    //创建表
                    dataBaseService.CreateDBTables(
                        typeof(UserInfoEntity),
                        typeof(LogInfoEntity),
                        typeof(AxisInfoEntity),
                        typeof(IOInputInfoEntity),
                        typeof(IOOutputInfoEntity),
                        typeof(MovementPointInfoEntity),
                        typeof(MovementPointSecurityEntity),
                        typeof(MovementPointPositionEntity),
                        typeof(ParmeterInfoEntity),
                        typeof(ProductInfoEntity),
                        typeof(InterpolationPathCoordinateEntity),
                        typeof(InterpolationPathEditEntity),
                        typeof(CylinderInfoEntity),
                        typeof(FunctionShieldEntity));
                    dataBaseService.Db.CodeFirst.SplitTables().InitTables<ProduceInfoEntity>();
                    //添加表中默认值
                    if (dataBaseService.Db.Queryable<ProductInfoEntity>().Count() == 0)
                    {
                        dataBaseService.Db.Insertable<ProductInfoEntity>(new ProductInfoEntity() { Name = "Default", Select = true }).ExecuteCommand();
                    }
                    if (dataBaseService.Db.Queryable<IOInputInfoEntity>().Count() == 0)
                    {
                        List<IOInputInfoEntity> defaultIOInputs = new List<IOInputInfoEntity>()
                        {
                            new IOInputInfoEntity() { Name = "启动按钮", CNName = "启动按钮", ENName = "Start Button", Number = "0", Backup = "面板启动按钮", ProgramAddressGroup = 0, ProgramAddressPosition = 0 },
                            new IOInputInfoEntity() { Name = "停止按钮", CNName = "停止按钮", ENName = "Stop Button", Number = "1", Backup = "面板停止按钮", ProgramAddressGroup = 0, ProgramAddressPosition = 1 },
                            new IOInputInfoEntity() { Name = "复位按钮", CNName = "复位按钮", ENName = "Reset Button", Number = "2", Backup = "面板复位按钮", ProgramAddressGroup = 0, ProgramAddressPosition = 2 },
                            new IOInputInfoEntity() { Name = "急停按钮", CNName = "急停按钮", ENName = "EmgStop Button", Number = "3", Backup = "面板急停按钮", ProgramAddressGroup = 0, ProgramAddressPosition = 3 }
                        };
                        dataBaseService.Db.Insertable(defaultIOInputs).ExecuteCommand();
                    }
                    if (dataBaseService.Db.Queryable<IOOutputInfoEntity>().Count() == 0)
                    {
                        List<IOOutputInfoEntity> defaultIOOutputs = new List<IOOutputInfoEntity>()
                        {
                            new IOOutputInfoEntity() { Name = "三色灯_红灯", CNName = "三色灯_红灯", ENName = "Tri_color Light_Red", Number = "0", Backup = "三色灯_红灯", ProgramAddressGroup = 0, ProgramAddressPosition = 0 },
                            new IOOutputInfoEntity() { Name = "三色灯_黄灯", CNName = "三色灯_黄灯", ENName = "Tri_color Light_Yellow", Number = "1", Backup = "三色灯_黄灯", ProgramAddressGroup = 0, ProgramAddressPosition = 1 },
                            new IOOutputInfoEntity() { Name = "三色灯_绿灯", CNName = "三色灯_绿灯", ENName = "Tri_color Light_Green", Number = "2", Backup = "三色灯_绿灯", ProgramAddressGroup = 0, ProgramAddressPosition = 2 },
                            new IOOutputInfoEntity() { Name = "三色灯_蜂鸣", CNName = "三色灯_蜂鸣", ENName = "Tri_color Light_Alarm", Number = "3", Backup = "三色灯_蜂鸣", ProgramAddressGroup = 0, ProgramAddressPosition = 3 },
                            new IOOutputInfoEntity() { Name = "启动灯", CNName = "启动灯", ENName = "Start Light", Number = "4", Backup = "启动灯", ProgramAddressGroup = 0, ProgramAddressPosition = 4 },
                            new IOOutputInfoEntity() { Name = "停止灯", CNName = "停止灯", ENName = "Stop Light", Number = "5", Backup = "停止灯", ProgramAddressGroup = 0, ProgramAddressPosition = 5 },
                            new IOOutputInfoEntity() { Name = "复位灯", CNName = "复位灯", ENName = "Reset Light", Number = "6", Backup = "复位灯", ProgramAddressGroup = 0, ProgramAddressPosition = 6 },
                        };
                        dataBaseService.Db.Insertable(defaultIOOutputs).ExecuteCommand();
                    }
                }
                //数据库备份表,仅在Mysql测试通过
                eventAggregator.SendLaunchInfo("数据库表备份");
                List<string> backupDBTableNames = new List<string>();
                new List<Type>()
                {
                    typeof(AxisInfoEntity),
                    typeof(IOInputInfoEntity),
                    typeof(IOOutputInfoEntity),
                    typeof(MovementPointInfoEntity),
                    typeof(MovementPointPositionEntity),
                    typeof(MovementPointSecurityEntity),
                    typeof(ParmeterInfoEntity),
                    typeof(UserInfoEntity),
                    typeof(CylinderInfoEntity),
                    typeof(InterpolationPathCoordinateEntity),
                    typeof(InterpolationPathEditEntity),
                    typeof(ProductInfoEntity),
                    typeof(FunctionShieldEntity),
                }.ForEach(x =>
                {
                    SugarTable attribute = (SugarTable)x.GetCustomAttribute(typeof(SugarTable), false);
                    backupDBTableNames.Add(attribute != null ? attribute.TableName : x.Name.ToLower());
                });
                dataBaseService.BackupDBTables(backupDBTableNames);
                //数据库备份,仅支持Mysql
                eventAggregator.SendLaunchInfo("备份数据库");
                if (!dataBaseService.BackupDatabaseWithTables(backupDBTableNames).GetAwaiter().GetResult())
                    logService.WriteLog(LogTypes.DB.ToString(), $@"备份数据库失败", MessageDegree.ERROR);
                //数据库创建清除数据事件
                eventAggregator.SendLaunchInfo("创建数据库清除事件");
                List<Tuple<string, string, uint>> createClearExpireDataEventTableNames = new List<Tuple<string, string, uint>>();
                new List<Tuple<Type, string, uint>>()
                {
                    new Tuple<Type, string, uint>(typeof(LogInfoEntity), "Clear_Log_Info_Expire_Data", 7),      
                    //添加其他需要删除过期数据的表(表类型,事件名称,过期时间),不支持分表,仅支持单表
                }.ForEach(x =>
                {
                    SugarTable attribute = (SugarTable)x.Item1.GetCustomAttribute(typeof(SugarTable), false);
                    createClearExpireDataEventTableNames.Add(new Tuple<string, string, uint>(attribute != null ? attribute.TableName : x.Item1.Name.ToLower(), x.Item2, x.Item3));
                });
                createClearExpireDataEventTableNames.ForEach(x =>
                {
                    if (!dataBaseService.CreateClearExpireDataEvent(x.Item2, x.Item1, x.Item3, "12:00:00").GetAwaiter().GetResult())
                        logService.WriteLog(LogTypes.DB.ToString(), $@"创建清除事件{x.Item2}失败", MessageDegree.ERROR);
                });
                //清除ProduceInfoEntity分表过期数据,保留6个月
                if (!dataBaseService.CreateClearExpireProduceInfoEvent(6, "12:10:00").GetAwaiter().GetResult())
                    logService.WriteLog(LogTypes.DB.ToString(), $@"创建清除ProduceInfo事件失败", MessageDegree.ERROR);
                #endregion

                #region 初始化权限服务
                eventAggregator.SendLaunchInfo("初始化权限服务");
                permissionService = containerProvider.Resolve<IPermissionService>();
                permissionService.CurrentUserName = string.Empty;
#if DEBUG
                //Debug时默认赋予最高权限
                permissionService.CurrentPermission = PermissionLevel.Developer;
#endif
                #endregion

                #region 初始化Mapper服务
                eventAggregator.SendLaunchInfo("初始化映射服务");
                mapper = containerProvider.Resolve<IMapper>();
                #endregion

                #region AppConfig配置读取
                AppleUIEnable = !Convert.ToBoolean(ConfigurationManager.AppSettings["AppleUIEnable"]);
                //赋值上次生产产品
                var currentProduct = dataBaseService.Db.Queryable<ProductInfoEntity>().Where(it => it.Select == true).First();
                GlobalValues.CurrentProduct = currentProduct != null ? currentProduct.Name : null;
                GlobalValues.GlobalVelPercentage = Convert.ToDouble(ConfigurationManager.AppSettings["GlobalVelPercentage"]);
                #endregion

                #region 日志相关
                //如果AppleUI启用,讲日志写入记录AppleLog队列,进行Apple需求的Log写入
                if (Convert.ToBoolean(ConfigurationManager.AppSettings["AppleUIEnable"]))
                {
                    logService.BeforeSaveToFile += ((log, args) =>
                    {
                        GlobalValues.AppleLogQueue.Enqueue(args);
                    });
                }
                //日志记录进数据库
                logService.BeforeSaveToFile += ((log, args) =>
                {
                    if (logService.GetMessageDegree() < MessageDegree.DEBUG)
                    {
                        //如果日志重复且重复次数小于100,则不进行记录
                        if (previousLog.Text == args.Message.Text)
                        {
                            if (sameLogRepeatTimes < 100)
                            {
                                args.Message.Ignore = true;
                                sameLogRepeatTimes++;
                            }
                            else
                            {
                                args.Message.Text += $"(重复次数:{sameLogRepeatTimes})";
                                sameLogRepeatTimes = 0;
                            }
                        }
                        else
                            sameLogRepeatTimes = 0;
                        previousLog.Text = args.Message.Text;
                    }

                    //写进数据库得数据keyword需要带DB字符且等级大于过滤等级
                    if (args.Message.KeyWord != null && args.Message.KeyWord.Contains("DB") && args.Message.Ignore != true)
                    {
                        try
                        {
                            string data = args.Message.Text;
                            //数据库记录日志长度最大为200,若日志内容超过200则截断字符串,取前200个字符
                            if (args.Message.Text.Length > 200)
                                data = data.Substring(0, 200);
                            dataBaseService.Db.Insertable(new LogInfoEntity()
                            {
                                Level = args.Message.Degree.ToString(),
                                Keyword = args.Message.KeyWord,
                                Message = data,
                                Time = args.Message.Time,
                            }).ExecuteCommand();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("写入数据库异常：" + ex.Source + "@@" + Environment.NewLine + ex.StackTrace + "##" + Environment.NewLine + ex.Message);
                        }
                    }
                });
                #endregion

                #region 导航页面添加
                MenuBars = new ObservableCollection<MenuBar>();
                if (!Convert.ToBoolean(ConfigurationManager.AppSettings["AppleUIEnable"]))
                {
                    MenuBars.Add(new MenuBar() { Icon = "Home", Title = "Home".TryFindResourceEx(), NameSpace = "IndexView" });
                    MenuBars.Add(new MenuBar() { Icon = "AlphaACircleOutline", Title = "AxisCfg".TryFindResourceEx(), NameSpace = nameof(MCAxisConfigView) });
                    MenuBars.Add(new MenuBar() { Icon = "LocationEnter", Title = "IOInputCfg".TryFindResourceEx(), NameSpace = nameof(MCIOInputConfigView) });
                    MenuBars.Add(new MenuBar() { Icon = "LocationExit", Title = "IOOutputCfg".TryFindResourceEx(), NameSpace = nameof(MCIOOutputConfigView) });
                    MenuBars.Add(new MenuBar() { Icon = "ArrowExpandVertical", Title = "CylinderCfg".TryFindResourceEx(), NameSpace = nameof(MCCylinderConfigView) });
                    MenuBars.Add(new MenuBar() { Icon = "ContentSaveEditOutline", Title = "ParmeterCfg".TryFindResourceEx(), NameSpace = nameof(MCParmeterConfigView) });
                    MenuBars.Add(new MenuBar() { Icon = "Cogs", Title = "MCPointCfg".TryFindResourceEx(), NameSpace = nameof(MCMovementPointConfigView) });
                    MenuBars.Add(new MenuBar() { Icon = "BorderOutside", Title = "InterpolationPathView".TryFindResourceEx(), NameSpace = nameof(MCInterpolationPathView) });
                    MenuBars.Add(new MenuBar() { Icon = "BrightnessAuto", Title = "AxisDebug".TryFindResourceEx(), NameSpace = nameof(MCAxisDebugView) });
                    MenuBars.Add(new MenuBar() { Icon = "SwapHorizontalCircleOutline", Title = "IODebug".TryFindResourceEx(), NameSpace = nameof(MCIODebugView) });
                    MenuBars.Add(new MenuBar() { Icon = "FormatParagraphSpacing", Title = "CylinderDebug".TryFindResourceEx(), NameSpace = nameof(MCCylinderDebugView) });
                    MenuBars.Add(new MenuBar() { Icon = "ContentSaveCogOutline", Title = "ParmeterDebug".TryFindResourceEx(), NameSpace = nameof(MCParmeterDebugView) });
                    MenuBars.Add(new MenuBar() { Icon = "CogTransferOutline", Title = "MovementPointDebug".TryFindResourceEx(), NameSpace = nameof(MCMovementPointDebugView) });
                    MenuBars.Add(new MenuBar() { Icon = "AccountMultiple", Title = "UserManager".TryFindResourceEx(), NameSpace = nameof(UserSettingView) });
                    MenuBars.Add(new MenuBar() { Icon = "CookieCogOutline", Title = "SystemCfg".TryFindResourceEx(), NameSpace = nameof(SystemSettingView) });
                    MenuBars.Add(new MenuBar() { Icon = "MathLog", Title = "LogSearch".TryFindResourceEx(), NameSpace = nameof(LogSearchView) });
                    MenuBars.Add(new MenuBar() { Icon = "FormatListBulletedType", Title = "ProductInfoSearch".TryFindResourceEx(), NameSpace = nameof(ProductInfoSearchView) });
                    MenuBars.Add(new MenuBar() { Icon = "ChartBar", Title = "ProductivityListSearch".TryFindResourceEx(), NameSpace = nameof(ProductivityListSearchView) });
                    MenuBars.Add(new MenuBar() { Icon = "FormatListBulletedSquare", Title = "ProductivityChartSearch".TryFindResourceEx(), NameSpace = nameof(ProductivityChartSearchView) });
                    MenuBars.Add(new MenuBar() { Icon = "ChartBarStacked", Title = "CTListSearch".TryFindResourceEx(), NameSpace = nameof(ProductCTListSearchView) });
                    MenuBars.Add(new MenuBar() { Icon = "FormatListBulleted", Title = "CTChartSearch".TryFindResourceEx(), NameSpace = nameof(ProductCTChartSearchView) });
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() => { regionManager.RegisterViewWithRegion("MainWindowLeftDrawerHostRegion", nameof(IndexView)); });
                }
                #endregion

                #region 初始化状态机
                machineState = containerProvider.Resolve<MachineStateMachine>();
                #endregion

                #region 初始化运动控制卡
                eventAggregator.SendLaunchInfo("初始化运动控制卡");
                motionCardService = containerProvider.Resolve<IMotionCardService>();

                if (motionCardService.Init())
                {
                    snackbarMessageQueue.EnqueueEx("初始化运动控制卡成功");
                    logService.WriteLog("初始化运动控制卡成功.", MessageDegree.DEBUG);
                }
                else
                {
                    snackbarMessageQueue.EnqueueEx("初始化运动控制卡失败");
#if !DEBUG
                    dialogService.QuestionShowDialog("错误", "初始化运动控制卡失败", "", "确认");
#endif
                    logService.WriteLog("初始化运动控制卡失败.", MessageDegree.DEBUG);
                }
                #endregion

                IMotionResourceInit motionResourceInit = containerProvider.Resolve<IMotionResourceInit>();

                //初始化轴集合
                motionResourceInit.InitAxisResourceDicCollection();

                //初始化输入点集合
                motionResourceInit.InitIOInputResourceDicCollection();

                //初始化输出点集合
                motionResourceInit.InitIOOutputResourceDicCollection();

                //启动刷新输入点与轴状态线程
                cancellationTokenInputsAndAxisRefresh = new CancellationTokenSource();
                Task.Factory.StartNew(() =>
                {
                    //刷新线程大概花费13毫秒
                    short axisCounts = (short)MotionControlResource.AxisResource.Count;
                    int ioiCounts = MotionControlResource.IOInputResource.Count;
                    int ioiGroupNums = ioiCounts / 8 + (ioiCounts % 8 == 0 ? 0 : 1);
                    short[,] io_Inputs = new short[ioiGroupNums, 8];

                    int iooCounts = MotionControlResource.IOOutputResource.Count;
                    int iooGroupNums = iooCounts / 8 + (iooCounts % 8 == 0 ? 0 : 1);
                    short[,] io_Outputs = new short[iooGroupNums, 8];

                    DateTime threadStableMonitorTime;
                    while (!cancellationTokenInputsAndAxisRefresh.IsCancellationRequested)
                    {
                        try
                        {
                            //检查运动控制卡是否初始化完毕
                            if (!motionCardService.InitSuccess)
                            {
                                Thread.Sleep(50);
                                continue;
                            }
                            threadStableMonitorTime = DateTime.Now;
                            //从运动控制卡获取输入点状态
                            for (int i = 0; i < ioiGroupNums; i++)
                            {
                                short? data = motionCardService.GetEcatGrpDi((short)i);
                                if (data.HasValue)
                                {
                                    for (int j = 0; j < 8; j++)
                                        io_Inputs[i, j] = (short)(data >> j & 0x1);
                                }
                            }
                            lock (MotionControlResource.IOInputResourceDicUpdateLock)
                            {
                                //Bug:如果IO数量增减超过线程开启时计算数量,将出现Bug,但为了提高性能,不重新计算IO组数量.重启即可解决此Bug
                                foreach (var item in MotionControlResource.IOInputResource)
                                    //如果屏蔽,直接使用屏蔽值(不在关注是否取反),屏蔽值不参与取反计算.如果未屏蔽且取反Enable,则取反.
                                    item.Value.Status = item.Value.ShieldEnable ? item.Value.GetShiedlEnableDefaultValue() :
                                                            item.Value.ReverseEnable ?
                                                                !(io_Inputs[item.Value.ProgramAddressGroup, item.Value.ProgramAddressPosition] == 1) :
                                                                    io_Inputs[item.Value.ProgramAddressGroup, item.Value.ProgramAddressPosition] == 1;
                            }

                            //从运动控制卡获取输出点状态
                            for (int i = 0; i < iooGroupNums; i++)
                            {
                                short? data = motionCardService.GetEcatGrpDo((short)i);
                                if (data.HasValue)
                                {
                                    for (int j = 0; j < 8; j++)
                                        io_Outputs[i, j] = (short)(data >> j & 0x1);
                                }
                            }
                            lock (MotionControlResource.IOOutputResourceDicUpdateLock)
                            {
                                //Bug:如果IO数量增减超过线程开启时计算数量,将出现Bug,但为了提高性能,不重新计算IO组数量.重启即可解决此Bug
                                foreach (var item in MotionControlResource.IOOutputResource)
                                    //如果屏蔽,直接返回屏蔽值,设定输出时默认输出屏蔽默认值.屏蔽值不参与取反计算.如果未屏蔽且取反Enable,则返回取反值.设定输出时也取反后设定输出.
                                    item.Value.Status = item.Value.ShieldEnable ? item.Value.GetShiedlEnableDefaultValue() :
                                                            item.Value.ReverseEnable ?
                                                                !(io_Outputs[item.Value.ProgramAddressGroup, item.Value.ProgramAddressPosition] == 1) :
                                                                    io_Outputs[item.Value.ProgramAddressGroup, item.Value.ProgramAddressPosition] == 1;
                            }

                            //如果轴数量不为0则从运动控制卡获取轴状态,位置为编码器值
                            lock (MotionControlResource.AxisResourceDicUpdateLock)
                            {
                                if (MotionControlResource.AxisResource.Count != 0)
                                {
                                    int[] axisSts = motionCardService.GetAxSts(0, axisCounts);
                                    double[] axisEncPos = motionCardService.GetAxEncPos(0, axisCounts);
                                    double[] axisPrfPos = motionCardService.GetAxPrfPos(0, axisCounts);
                                    double[] axisPrfVel = motionCardService.GetAxPrfVel(0, axisCounts);
                                    foreach (var item in MotionControlResource.AxisResource)
                                    {
                                        item.Value.Status = axisSts[item.Value.AxisIdOnCard];
                                        item.Value.EncPos = axisEncPos[item.Value.AxisIdOnCard];
                                        item.Value.PrfPos = axisPrfPos[item.Value.AxisIdOnCard];
                                        item.Value.PrfVel = axisPrfVel[item.Value.AxisIdOnCard];
                                    }
                                }
                            }

                            Thread.Sleep(5);
                            //刷新线程稳定程度监视,理论8ms
                            if ((DateTime.Now - threadStableMonitorTime).TotalMilliseconds > 20)
                                logService.WriteLog(LogTypes.DB.ToString(), $"轴状态及点位刷新线程刷新周期过高", MessageDegree.WARN);
                        }
                        catch (Exception ex)
                        {
                            logService.WriteLog(LogTypes.DB.ToString(), "从板卡获取数据异常", MessageDegree.FATAL);
                            logService.WriteLog("Exception", $"从板卡获取数据异常:{ex.Message}", ex);
                        }
                    }
                }, cancellationTokenInputsAndAxisRefresh.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                cancellationTokenOperationCommand = new CancellationTokenSource();
                //面板按钮监视处理线程
                Task.Factory.StartNew(() =>
                {
                    //边沿触发维持时间为一个赋值周期
                    EdgeTrigger startButtonSingle = new EdgeTrigger();
                    EdgeTrigger stopButtonSingle = new EdgeTrigger();
                    EdgeTrigger resetButtonSingle = new EdgeTrigger();

                    while (!cancellationTokenOperationCommand.IsCancellationRequested)
                    {
                        try
                        {
                            //检查运动控制卡是否初始化完毕
                            if (!motionCardService.InitSuccess)
                            {
                                Thread.Sleep(1000);
                                //await Task.Delay(1000, cancellationTokenOperationCommand.Token);
                                continue;
                            }
                            #region 监视面板按钮
                            //启动,停止,复位监视上升沿触发
                            startButtonSingle.Current = motionCardService.GetInputStsEx("启动按钮");
                            stopButtonSingle.Current = motionCardService.GetInputStsEx("停止按钮");
                            resetButtonSingle.Current = motionCardService.GetInputStsEx("复位按钮");
                            if (startButtonSingle.UpTrigger == true) GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.StartUp));
                            if (stopButtonSingle.UpTrigger == true) GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.Stop));
                            if (resetButtonSingle.UpTrigger == true) GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.Reset));
                            //急停按钮监视电平触发
                            if (motionCardService.GetInputStsEx("急停按钮") == false) GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                            #endregion
                            #region 监视门禁
                            //门禁使用电平触发

                            #endregion
                            #region 轴报警监视=>急停
                            if (GlobalValues.MachineStatus == FSMStateCode.Running && motionCardService.GetAxesErrorEx(logService))
                            {
                                logService.WriteLog(LogTypes.DB.ToString(), $"轴报警,急停", MessageDegree.FATAL);
                                GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.EmergencyStop));
                            }
                            #endregion
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            logService.WriteLog(LogTypes.DB.ToString(), "监视面板按钮,门禁及光栅线程异常", MessageDegree.FATAL);
                            logService.WriteLog("Exception", $"监视面板按钮,门禁及光栅线程异常:{ex.Message}", ex);
                            throw ex;
                        }

                    }
                }, cancellationTokenOperationCommand.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                //状态机状态流转线程
                Task.Factory.StartNew(() =>
                {
                    while (!cancellationTokenOperationCommand.IsCancellationRequested)
                    {
                        try
                        {
                            //检查运动控制卡是否初始化完毕
                            if (!motionCardService.InitSuccess)
                            {
                                Thread.Sleep(1000);
                                //await Task.Delay(1000, cancellationTokenOperationCommand.Token);
                                continue;
                            }
                            Thread.Sleep(50);
                            //处理队列流转事件
                            if (GlobalValues.OperationCommandQueue.TryDequeue(out FSMEvent fsmEvent))
                            {
                                if (GlobalValues.MachineStatus == FSMStateCode.EmergencyStopping && fsmEvent.GetEventCode() == FSMEventCode.EmergencyStop)
                                {
                                    continue;
                                }
                                if (GlobalValues.MachineStatus == FSMStateCode.Pausing && fsmEvent.GetEventCode() == FSMEventCode.Stop)
                                {
                                    continue;
                                }
                                logService.WriteLog($"当前设备状态为{GlobalValues.MachineStatus},事件{fsmEvent.GetEventCode()}出列~", MessageDegree.DEBUG);
                                GlobalValues.MachineStatus = machineState.Execute(GlobalValues.MachineStatus, fsmEvent).GetStateCode();
                                //发送设备状态变更事件
                                eventAggregator.SendMachineStsChangeEvent(GlobalValues.MachineStatus);
                            }
                        }
                        catch (Exception ex)
                        {
                            logService.WriteLog(LogTypes.DB.ToString(), "状态机流转处理线程异常", MessageDegree.FATAL);
                            logService.WriteLog("Exception", $"状态机流转处理线程异常:{ex.Message}", ex);
                            throw ex;
                        }
                    }
                }, cancellationTokenOperationCommand.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                //面板灯,三色灯,蜂鸣器状态切换,定时退出登录时间监视线程
                Task.Factory.StartNew(() =>
                {
                    bool timerSts = false;
                    Timer BlinkTimer = new Timer(new TimerCallback(obj =>
                    {
                        motionCardService.SetOuputStsEx("三色灯_红灯", true);
                        motionCardService.SetOuputStsEx("停止灯", true);
                        motionCardService.SetOuputStsEx("三色灯_蜂鸣", GlobalValues.SilenceAlarm ? false : true);
                        Thread.Sleep(500);
                        motionCardService.SetOuputStsEx("三色灯_红灯", false);
                        motionCardService.SetOuputStsEx("停止灯", false);
                        motionCardService.SetOuputStsEx("三色灯_蜂鸣", false);
                    }), null, Timeout.Infinite, 500);

                    bool autoLogoutEnable = Convert.ToBoolean(ConfigurationManager.AppSettings["AutoLogoutEnable"]);
                    double autoLogoutTime = Convert.ToDouble(ConfigurationManager.AppSettings["AutoLogoutTime"]);
                    if (autoLogoutEnable) mouseIdleTime = containerProvider.Resolve<IMouseIdleTime>();

                    while (!cancellationTokenOperationCommand.IsCancellationRequested)
                    {
                        try
                        {
                            //检测鼠标是否有动作来决定是否登出,条件为:启用自动登出功能,权限高于Admin,到达登出时间
                            if (autoLogoutEnable && permissionService.CheckPermission(PermissionLevel.Admin) && mouseIdleTime.GetMouseIdleTimeSecondes() > autoLogoutTime)
                                eventAggregator.GetEvent<MessageEvent>().Publish(new MessageModel() { Filter = "QuickLoginOrOut", Message = "TimerLoginOut", });

                            //检查运动控制卡是否初始化完毕
                            if (!motionCardService.InitSuccess)
                            {
                                Thread.Sleep(500);
                                continue;
                            }
                            //三色灯控制
                            switch (GlobalValues.MachineStatus)
                            {
                                case FSMStateCode.PowerUpping:
                                    if (motionCardService.GetOutputStsEx("三色灯_红灯") == true) motionCardService.SetOuputStsEx("三色灯_红灯", false);
                                    if (motionCardService.GetOutputStsEx("三色灯_黄灯") == false) motionCardService.SetOuputStsEx("三色灯_黄灯", true);
                                    if (motionCardService.GetOutputStsEx("三色灯_绿灯") == true) motionCardService.SetOuputStsEx("三色灯_绿灯", false);
                                    if (timerSts != false) { timerSts = false; BlinkTimer.Change(Timeout.Infinite, 0); }
                                    if (motionCardService.GetOutputStsEx("三色灯_蜂鸣") == true) motionCardService.SetOuputStsEx("三色灯_蜂鸣", false);
                                    if (motionCardService.GetOutputStsEx("停止灯") == true) motionCardService.SetOuputStsEx("停止灯", false);
                                    if (motionCardService.GetOutputStsEx("复位灯") == true) motionCardService.SetOuputStsEx("复位灯", false);
                                    if (motionCardService.GetOutputStsEx("启动灯") == true) motionCardService.SetOuputStsEx("启动灯", false);
                                    break;
                                case FSMStateCode.GlobleResetting:
                                    if (motionCardService.GetOutputStsEx("三色灯_红灯") == true) motionCardService.SetOuputStsEx("三色灯_红灯", false);
                                    if (motionCardService.GetOutputStsEx("三色灯_黄灯") == false) motionCardService.SetOuputStsEx("三色灯_黄灯", true);
                                    if (motionCardService.GetOutputStsEx("三色灯_绿灯") == true) motionCardService.SetOuputStsEx("三色灯_绿灯", false);
                                    if (timerSts != false) { timerSts = false; BlinkTimer.Change(Timeout.Infinite, 0); }
                                    if (motionCardService.GetOutputStsEx("三色灯_蜂鸣") == true) motionCardService.SetOuputStsEx("三色灯_蜂鸣", false);
                                    if (motionCardService.GetOutputStsEx("停止灯") == true) motionCardService.SetOuputStsEx("停止灯", false);
                                    if (motionCardService.GetOutputStsEx("复位灯") == false) motionCardService.SetOuputStsEx("复位灯", true);
                                    if (motionCardService.GetOutputStsEx("启动灯") == true) motionCardService.SetOuputStsEx("启动灯", false);
                                    break;
                                case FSMStateCode.Idling:
                                    if (motionCardService.GetOutputStsEx("三色灯_红灯") == true) motionCardService.SetOuputStsEx("三色灯_红灯", false);
                                    if (motionCardService.GetOutputStsEx("三色灯_黄灯") == false) motionCardService.SetOuputStsEx("三色灯_黄灯", true);
                                    if (motionCardService.GetOutputStsEx("三色灯_绿灯") == true) motionCardService.SetOuputStsEx("三色灯_绿灯", false);
                                    if (timerSts != false) { timerSts = false; BlinkTimer.Change(Timeout.Infinite, 0); }
                                    if (motionCardService.GetOutputStsEx("三色灯_蜂鸣") == true) motionCardService.SetOuputStsEx("三色灯_蜂鸣", false);
                                    if (motionCardService.GetOutputStsEx("停止灯") == false) motionCardService.SetOuputStsEx("停止灯", true);
                                    if (motionCardService.GetOutputStsEx("复位灯") == true) motionCardService.SetOuputStsEx("复位灯", false);
                                    if (motionCardService.GetOutputStsEx("启动灯") == true) motionCardService.SetOuputStsEx("启动灯", false);
                                    break;
                                case FSMStateCode.BurnInTesting:
                                case FSMStateCode.Running:
                                    if (motionCardService.GetOutputStsEx("三色灯_红灯") == true) motionCardService.SetOuputStsEx("三色灯_红灯", false);
                                    if (motionCardService.GetOutputStsEx("三色灯_黄灯") == true) motionCardService.SetOuputStsEx("三色灯_黄灯", false);
                                    if (motionCardService.GetOutputStsEx("三色灯_绿灯") == false) motionCardService.SetOuputStsEx("三色灯_绿灯", true);
                                    if (timerSts != false) { timerSts = false; BlinkTimer.Change(Timeout.Infinite, 0); }
                                    if (motionCardService.GetOutputStsEx("三色灯_蜂鸣") == true) motionCardService.SetOuputStsEx("三色灯_蜂鸣", false);
                                    if (motionCardService.GetOutputStsEx("停止灯") == true) motionCardService.SetOuputStsEx("停止灯", false);
                                    if (motionCardService.GetOutputStsEx("复位灯") == true) motionCardService.SetOuputStsEx("复位灯", false);
                                    if (motionCardService.GetOutputStsEx("启动灯") == false) motionCardService.SetOuputStsEx("启动灯", true);
                                    break;
                                case FSMStateCode.BurnInAlarming:
                                case FSMStateCode.Alarming:
                                    if (motionCardService.GetOutputStsEx("三色灯_黄灯") == true) motionCardService.SetOuputStsEx("三色灯_黄灯", false);
                                    if (motionCardService.GetOutputStsEx("三色灯_绿灯") == true) motionCardService.SetOuputStsEx("三色灯_绿灯", false);
                                    //给信号三色灯蜂鸣叫-停-叫实现,若timerSts里面启用控制蜂鸣(既三色灯给信号叫不给信号停)则需要注释此句.
                                    //if (motionCardService.GetOutputStsEx("三色灯_蜂鸣") == false) motionCardService.SetOuputStsEx("三色灯_蜂鸣", true);
                                    if (timerSts != true) { timerSts = true; BlinkTimer.Change(0, 1000); }
                                    if (motionCardService.GetOutputStsEx("复位灯") == true) motionCardService.SetOuputStsEx("复位灯", false);
                                    if (motionCardService.GetOutputStsEx("启动灯") == true) motionCardService.SetOuputStsEx("启动灯", false);
                                    break;
                                case FSMStateCode.BurnInPausing:
                                case FSMStateCode.Pausing:
                                    //如果需要暂停的时候报警
                                    if (GlobalValues.InterlockPauseWithAlarm)
                                    {
                                        if (motionCardService.GetOutputStsEx("三色灯_黄灯") == true) motionCardService.SetOuputStsEx("三色灯_黄灯", false);
                                        if (motionCardService.GetOutputStsEx("三色灯_绿灯") == true) motionCardService.SetOuputStsEx("三色灯_绿灯", false);
                                        //给信号三色灯蜂鸣叫-停-叫实现,若timerSts里面启用控制蜂鸣(既三色灯给信号叫不给信号停)则需要注释此句.
                                        //if (motionCardService.GetOutputStsEx("三色灯_蜂鸣") == false) motionCardService.SetOuputStsEx("三色灯_蜂鸣", true);
                                        if (timerSts != true) { timerSts = true; BlinkTimer.Change(0, 1000); }
                                        if (motionCardService.GetOutputStsEx("复位灯") == true) motionCardService.SetOuputStsEx("复位灯", false);
                                        if (motionCardService.GetOutputStsEx("启动灯") == true) motionCardService.SetOuputStsEx("启动灯", false);
                                    }
                                    else
                                    {
                                        if (motionCardService.GetOutputStsEx("三色灯_红灯") == true) motionCardService.SetOuputStsEx("三色灯_红灯", false);
                                        if (motionCardService.GetOutputStsEx("三色灯_黄灯") == false) motionCardService.SetOuputStsEx("三色灯_黄灯", true);
                                        if (motionCardService.GetOutputStsEx("三色灯_绿灯") == true) motionCardService.SetOuputStsEx("三色灯_绿灯", false);
                                        if (timerSts != false) { timerSts = false; BlinkTimer.Change(Timeout.Infinite, 0); }
                                        if (motionCardService.GetOutputStsEx("三色灯_蜂鸣") == true) motionCardService.SetOuputStsEx("三色灯_蜂鸣", false);
                                        if (motionCardService.GetOutputStsEx("停止灯") == true) motionCardService.SetOuputStsEx("停止灯", false);
                                        if (motionCardService.GetOutputStsEx("复位灯") == true) motionCardService.SetOuputStsEx("复位灯", false);
                                        if (motionCardService.GetOutputStsEx("启动灯") == true) motionCardService.SetOuputStsEx("启动灯", false);
                                    }
                                    break;
                                case FSMStateCode.EmergencyStopping:
                                    if (motionCardService.GetOutputStsEx("三色灯_黄灯") == true) motionCardService.SetOuputStsEx("三色灯_黄灯", false);
                                    if (motionCardService.GetOutputStsEx("三色灯_绿灯") == true) motionCardService.SetOuputStsEx("三色灯_绿灯", false);
                                    //给信号三色灯蜂鸣叫-停-叫实现,若timerSts里面启用控制蜂鸣(既三色灯给信号叫不给信号停)则需要注释此句.
                                    //if (motionCardService.GetOutputSts("三色灯_蜂鸣") == false) motionCardService.SetOuputSts("三色灯_蜂鸣", true);
                                    if (timerSts != true) { timerSts = true; BlinkTimer.Change(0, 1000); }
                                    if (motionCardService.GetOutputStsEx("复位灯") == true) motionCardService.SetOuputStsEx("复位灯", false);
                                    if (motionCardService.GetOutputStsEx("启动灯") == true) motionCardService.SetOuputStsEx("启动灯", false);
                                    break;
                                default:
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            logService.WriteLog($"刷新灯线程异常{ex.Message}", ex);
                        }
                        Thread.Sleep(500);
                    }

                }, cancellationTokenOperationCommand.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                //初始化全局运动参数集合
                eventAggregator.SendLaunchInfo("初始运动参数集合");
                motionResourceInit.InitMCParmeterDicCollection();
                //初始化全局运动点位集合
                eventAggregator.SendLaunchInfo("初始运动点位集合");
                motionResourceInit.InitMCPointDicCollection();
                //初始化气缸集合
                eventAggregator.SendLaunchInfo("初始化气缸集合");
                motionResourceInit.InitCylinderDicCollection();
                // 初始化插补路径
                eventAggregator.SendLaunchInfo("初始化插补数据");
                motionResourceInit.InitInterpolationPaths();
                //初始化屏蔽功能
                eventAggregator.SendLaunchInfo("初始化屏蔽功能");
                motionResourceInit.InitFunctionShieldDicCollection();

                snackbarMessageQueue.EnqueueEx("InitSuccess");
            }
            catch (Exception ex)
            {
                logService.WriteLog("Exception", $"程序启动失败:{ex.Message}=>{ex}", MessageDegree.FATAL);
                MessageBox.Show($"程序启动失败:{ex.Message}", $"错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        ~MainWindowViewModel()
        {
            cancellationTokenInputsAndAxisRefresh?.Cancel();
            cancellationTokenOperationCommand?.Cancel();
            motionCardService?.CloseWithReset();
            //motionCardService?.Close();
        }
    }
}
