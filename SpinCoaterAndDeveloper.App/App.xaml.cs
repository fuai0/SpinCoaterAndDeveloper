using AutoMapper;
using LiveChartsCore.SkiaSharpView;
using LogServiceInterface;
using MaterialDesignThemes.Wpf;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using Prism.Services.Dialogs;
using PrismAutomationPlatformExt;
using SkiaSharp;
using SpinCoaterAndDeveloper.Actuation.Actuation;
using SpinCoaterAndDeveloper.App.Services.MachineInterlockService;
using SpinCoaterAndDeveloper.App.ViewModels;
using SpinCoaterAndDeveloper.App.ViewModels.Dialogs;
using SpinCoaterAndDeveloper.App.Views;
using SpinCoaterAndDeveloper.App.Views.Dialogs;
using SpinCoaterAndDeveloper.App.Views.Loading;
using SpinCoaterAndDeveloper.FSM.FSM;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.Extensions;
using SpinCoaterAndDeveloper.Shared.Services.MachineInterlockService;
using SpinCoaterAndDeveloper.Shared.Services.MotionResourceInitService;
using SpinCoaterAndDeveloper.Shared.Services.MouseIdelTimeService;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SpinCoaterAndDeveloper.App
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            FrameworkCompatibilityPreferences.KeepTextBoxDisplaySynchronizedWithTextProperty = false;
            //多语言初始化
            switch (ConfigurationManager.AppSettings["Language"])
            {
                case "zh-CN":
                    LanguageInit(@"Resources\Language\zh-CN.xaml");
                    System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("zh-CN");
                    break;
                case "en-US":
                    LanguageInit(@"Resources\Language\en-US.xaml");
                    System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
                    break;
                case "vi-VN":
                    LanguageInit(@"Resources\Language\vi-VN.xaml");
                    System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("vi-VN");
                    break;
                default:
                    LanguageInit(@"Resources\Language\zh-CN.xaml");
                    System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("zh-CN");
                    break;
            }
            base.OnStartup(e);
            //全局捕获异常
            RegisterEvents();
            LiveChartsCore.LiveCharts.Configure(config => config.HasGlobalSKTypeface(SKFontManager.Default.MatchCharacter('汉')));
        }
        protected override IModuleCatalog CreateModuleCatalog()
        {
            return new ConfigurationModuleCatalog();
        }
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            //注册AutoMapper
            containerRegistry.RegisterSingleton<IAutoMapperProvider, AutoMapperProvider>();
            containerRegistry.Register(typeof(IMapper), providerContent => providerContent.Resolve<IAutoMapperProvider>().GetMapper());
            containerRegistry.RegisterInstance<ISnackbarMessageQueue>(new MaterialDesignThemes.Wpf.SnackbarMessageQueue(TimeSpan.FromSeconds(2)));
            containerRegistry.RegisterDialog<LaunchView, LaunchViewModel>();
            containerRegistry.RegisterForNavigation<AppleNullView, AppleNullViewModel>();

            containerRegistry.Register<IDialogWithNoParentService, DialogWithNoParentService>();

            containerRegistry.Register<IDialogHostService, DialogHostService>();
            containerRegistry.RegisterForNavigation<DialogHostLoginView, DialogHostLoginViewModel>();
            containerRegistry.RegisterForNavigation<DialogHostMessageView, DialogHostMessageViewModel>();
            containerRegistry.RegisterDialog<DialogMessageView, DialogMessageViewModel>();
            containerRegistry.RegisterDialogWindow<DialogMessageWindow>();

            containerRegistry.RegisterForNavigation<MainWindowNoneClientAreaView, MainWindowNoneClientAreaViewModel>();
            containerRegistry.RegisterForNavigation<IndexView, IndexViewModel>();
            containerRegistry.RegisterForNavigation<UserSettingView, UserSettingViewModel>();
            containerRegistry.RegisterForNavigation<MCAxisConfigView, MCAxisConfigViewModel>();
            containerRegistry.RegisterForNavigation<MCAxisDebugView, MCAxisDebugViewModel>();
            containerRegistry.RegisterForNavigation<MCIOInputConfigView, MCIOInputConfigViewModel>();
            containerRegistry.RegisterForNavigation<MCIOOutputConfigView, MCIOOutputConfigViewModel>();
            containerRegistry.RegisterForNavigation<MCIODebugView, MCIODebugViewModel>();
            containerRegistry.RegisterForNavigation<MCCylinderConfigView, MCCylinderConfigViewModel>();
            containerRegistry.RegisterForNavigation<MCCylinderDebugView, MCCylinderDebugViewModel>();
            containerRegistry.RegisterForNavigation<MCParmeterConfigView, MCParmeterConfigViewModel>();
            containerRegistry.RegisterForNavigation<MCParmeterDebugView, MCParmeterDebugViewModel>();
            containerRegistry.RegisterForNavigation<AddParmeterView, AddParmeterViewModel>();
            containerRegistry.RegisterForNavigation<MCMovementPointConfigView, MCMovementPointConfigViewModel>();
            containerRegistry.RegisterForNavigation<MCMovementPointDebugView, MCMovementPointDebugViewModel>();
            containerRegistry.RegisterDialog<MCMovementPointSecurityView, MCMovementPointSecurityViewModel>();
            containerRegistry.RegisterForNavigation<SystemSettingView, SystemSettingViewModel>();
            containerRegistry.RegisterForNavigation<LogSearchView, LogSearchViewModel>();
            containerRegistry.RegisterForNavigation<ProductInfoSearchView, ProductInfoSearchViewModel>();
            containerRegistry.RegisterSingleton<ProductivitySearchViewModel>();
            containerRegistry.RegisterForNavigation<ProductivityListSearchView, ProductivitySearchViewModel>();
            containerRegistry.RegisterForNavigation<ProductivityChartSearchView, ProductivitySearchViewModel>();
            containerRegistry.RegisterSingleton<ProductCTSearchViewModel>();
            containerRegistry.RegisterForNavigation<ProductCTListSearchView, ProductCTSearchViewModel>();
            containerRegistry.RegisterForNavigation<ProductCTChartSearchView, ProductCTSearchViewModel>();
            containerRegistry.RegisterForNavigation<MCInterpolationPathView, MCInterpolationPathViewModel>();
            containerRegistry.RegisterForNavigation<AddInterpolationPathView, AddInterpolationPathViewModel>();
            containerRegistry.RegisterForNavigation<AddNewProductView, AddNewProductViewModel>();
            containerRegistry.RegisterForNavigation<ChangeProductNameView, ChangeProductNameViewModel>();
            containerRegistry.RegisterForNavigation<ActuationMonitorView, ActuationMonitorViewModel>();
            containerRegistry.RegisterDialog<GlobalLogView, GlobalLogViewModel>();
            containerRegistry.RegisterDialog<InterlockDialogMessageView, InterlockDialogMessageViewModel>();
            containerRegistry.RegisterDialog<DialogSystemMessageView, DialogSystemMessageViewModel>();
            containerRegistry.RegisterForNavigation<MCFunctionShieldConfigView, MCFunctionShieldConfigViewModel>();
            containerRegistry.RegisterForNavigation<MCFunctionShieldDebugView, MCFunctionShieldDebugViewModel>();
            containerRegistry.RegisterForNavigation<VisionCalibrationView, VisionCalibrationViewModel>();
            containerRegistry.RegisterForNavigation<VisionCompensationView, VisionCompensationViewModel>();
            containerRegistry.RegisterForNavigation<VisionTestView, VisionTestViewModel>();

            //Options
            containerRegistry.RegisterForNavigation<OptionLogView, OptionLogViewModel>();
            containerRegistry.RegisterForNavigation<OptionProduceCTView, OptionProduceCTViewModel>();
            containerRegistry.RegisterForNavigation<OptionCpuDimmUsageView, OptionCpuDimmUsageViewModel>();
            containerRegistry.RegisterForNavigation<OptionHddUsageView, OptionHddUsageViewModel>();
            containerRegistry.RegisterForNavigation<OptionUphView, OptionUphViewModel>();
            containerRegistry.RegisterForNavigation<OptionFunctionShieldView, OptionFunctionShieldViewModel>();

            //Apple
            containerRegistry.RegisterForNavigation<AppleNavigateView, AppleNavigateViewModel>();
            containerRegistry.RegisterForNavigation<AppleIndexView, AppleIndexViewModel>();
            containerRegistry.RegisterForNavigation<AppleAlarmView, AppleAlarmViewModel>();
            containerRegistry.RegisterForNavigation<AppleConfigView, AppleConfigViewModel>();
            containerRegistry.RegisterForNavigation<AppleDataView, AppleDataViewModel>();
            containerRegistry.RegisterForNavigation<AppleVisionView, AppleVisionViewModel>();
            containerRegistry.RegisterForNavigation<AppleSettingView, AppleSettingViewModel>();

            containerRegistry.RegisterSingleton<IMotionResourceInit, MotionResourceInit>();
            containerRegistry.RegisterSingleton<IMouseIdleTime, MouseIdleTime>();
            containerRegistry.RegisterSingleton<IMachineInterlock, MachineInterlock>();
            //注册状态机
            containerRegistry.RegisterSingleton<MachineStateMachine>();
            //注册设备动作组
            containerRegistry.RegisterSingleton<ActuationGlobleResetGroupManager>();
            containerRegistry.RegisterSingleton<ActuationRunningGroupManager>();
            containerRegistry.RegisterSingleton<ActuationBurnInGroupManager>();
        }
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindowView>();
        }
        protected override void InitializeShell(Window shell)
        {
            base.InitializeShell(shell);
        }
        protected override void InitializeModules()
        {
            base.InitializeModules();
        }
        protected override void OnInitialized()
        {
            #region 根据多开设定检测程序只能运行一次
            if (Convert.ToBoolean(ConfigurationManager.AppSettings["OnlyRunOneInstance"]))
            {
                string mName = System.Diagnostics.Process.GetCurrentProcess().MainModule.ModuleName;
                string pName = System.IO.Path.GetFileNameWithoutExtension(mName);
                System.Diagnostics.Process[] myProcess = System.Diagnostics.Process.GetProcessesByName(pName);
                if (myProcess.Length > 1)
                {
                    DialogParameters dialogPars = new DialogParameters
                        {
                            { "Title", "提示".TryFindResourceEx()},
                            { "Content", "本程序一次只能运行一个实例!".TryFindResourceEx()},
                            { "IgnoreInfo", "" },  //Ignore按钮默认不显示
                            { "CancelInfo", "" },
                            { "YesInfo", "确认".TryFindResourceEx()},
                            { "RedirectInfo", "" }
                        };
                    Container.Resolve<IDialogService>().ShowDialog("DialogMessageView", dialogPars, r => { });
                    Application.Current.Shutdown();
                    return;
                }
            }
            #endregion

            #region 加载窗口 
            IConfigureService configureService = Application.Current.MainWindow.DataContext as IConfigureService;
            Container.Resolve<IDialogService>().ShowDialog("LaunchView", new DialogParameters() { { "ConfigureService", configureService } }, null);
            #endregion

            base.OnInitialized();

            #region 主页面及标题栏区域内容
            Container.Resolve<IRegionManager>().RegisterViewWithRegion("MainViewNoneClientAreaRegion", "MainWindowNoneClientAreaView");
            if (Convert.ToBoolean(ConfigurationManager.AppSettings["AppleUIEnable"]) == true)
            {
                //首页加载服务导航方式(AppleIndex需要完成导航之后在进行导航)
                //Container.Resolve<IRegionManager>().RequestNavigate("MainViewRegion", nameof(AppleIndexView), x => { Container.Resolve<IRegionManager>().RequestNavigate("AppleIndexRightRegion1", nameof(OptionProduceCTView)); });
                Container.Resolve<IRegionManager>().RequestNavigate("MainViewRegion", nameof(AppleIndexView));
                Container.Resolve<IRegionManager>().RequestNavigate("MainViewAppleNavigateRegion", nameof(AppleNavigateView));
            }
            else
                Container.Resolve<IRegionManager>().RequestNavigate("MainViewRegion", nameof(IndexView));
            #endregion
        }
        protected override void OnExit(ExitEventArgs e)
        {
            //清理退出时需要处理的内容
            base.OnExit(e);
        }
        #region 捕获异常
        private void RegisterEvents()
        {
            //Task线程内未捕获异常处理事件
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            //UI线程未捕获异常处理事件（UI主线程）
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            //非UI线程未捕获异常处理事件(例如自己创建的一个子线程)
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                var exception = e.Exception as Exception;
                if (exception != null)
                {
                    HandleException(exception);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                e.SetObserved();
            }
        }

        //非UI线程未捕获异常处理事件(例如自己创建的一个子线程)      
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var exception = e.ExceptionObject as Exception;
                if (exception != null)
                {
                    HandleException(exception);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                //ignore
            }
        }

        //UI线程未捕获异常处理事件（UI主线程）
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                HandleException(e.Exception);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                //处理完后，我们需要将Handler=true表示已此异常已处理过
                e.Handled = true;
            }
        }
        private void HandleException(Exception e)
        {
            ILogService logService = Container.Resolve<ILogService>();
            string innerExString = string.Empty;
            if (e is AggregateException ex)
            {
                foreach (var item in ex.InnerExceptions)
                {
                    innerExString += item.Message;
                    logService.WriteLog(LogTypes.DB.ToString(), $@"InnerException: {item.Message}", item);
                }
            }
            //记录日志
            logService.WriteLog(LogTypes.DB.ToString() + " GlobalException", $@"全局捕获异常:{e.Message}", e);
            //弹窗
            MessageBox.Show("程序异常：" + e.Source + "@@" + Environment.NewLine + e.StackTrace + "##" + Environment.NewLine + (string.IsNullOrEmpty(innerExString) ? "" : innerExString) + Environment.NewLine + e.Message);
        }
        #endregion

        #region 多语言
        private void LanguageInit(string url)
        {
            List<ResourceDictionary> dictionaryList = new List<ResourceDictionary>();
            foreach (ResourceDictionary dictionary in Application.Current.Resources.MergedDictionaries)
            {
                if (dictionary.Source != null)
                {
                    dictionaryList.Add(dictionary);
                }
            }
            ResourceDictionary resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString.Equals(url));
            Application.Current.Resources.MergedDictionaries.Remove(resourceDictionary);
            Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
        }
        #endregion
    }
}
