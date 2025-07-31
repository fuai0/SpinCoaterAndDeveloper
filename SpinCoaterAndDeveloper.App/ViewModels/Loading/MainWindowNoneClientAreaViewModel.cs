using LogServiceInterface;
using MaterialDesignThemes.Wpf;
using PermissionServiceInterface;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using PrismAutomationPlatformExt;
using SpinCoaterAndDeveloper.App.Common.Models;
using SpinCoaterAndDeveloper.App.Extensions;
using SpinCoaterAndDeveloper.App.Views;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using SpinCoaterAndDeveloper.Shared.Event;
using SpinCoaterAndDeveloper.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Windows;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class MainWindowNoneClientAreaViewModel : BindableBase
    {
        private readonly IEventAggregator eventAggregator;
        private readonly IDialogHostService dialogHostService;
        private readonly IPermissionService permissionService;
        private readonly ILogService logService;
        private readonly ISnackbarMessageQueue snackbarMessageQueue;
        private readonly IDialogWithNoParentService dialogWithNoParentService;

        private string userName = "DefaultUserName".TryFindResourceEx();
        private string loginContent = "Login".TryFindResourceEx();

        private bool _ButtonEnable = true;
        public bool ButtonEnable
        {
            get { return _ButtonEnable; }
            set { SetProperty(ref _ButtonEnable, value); }
        }
        private bool toggleButtonIsCheck;
        public bool ToggleButtonIsCheck
        {
            get { return toggleButtonIsCheck; }
            set { SetProperty(ref toggleButtonIsCheck, value); }
        }
        public string UserName
        {
            get { return userName; }
            set { SetProperty(ref userName, value); }
        }
        public string LoginContent
        {
            get { return loginContent; }
            set { SetProperty(ref loginContent, value); }
        }
        private bool appleUIEnable;
        public bool AppleUIEnable
        {
            get { return appleUIEnable; }
            set { SetProperty(ref appleUIEnable, value); }
        }
        private LanguageBar _CurrentLanguageSelect;
        public LanguageBar CurrentLanguageSelect
        {
            get { return _CurrentLanguageSelect; }
            set { SetProperty(ref _CurrentLanguageSelect, value); }
        }

        public ObservableCollection<LanguageBar> LanguageList { get; set; } = new ObservableCollection<LanguageBar>();
        public MainWindowNoneClientAreaViewModel(IContainerProvider containerProvider)
        {
            this.eventAggregator = containerProvider.Resolve<IEventAggregator>();
            this.dialogHostService = containerProvider.Resolve<IDialogHostService>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.permissionService = containerProvider.Resolve<IPermissionService>();
            this.snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();
            this.dialogWithNoParentService = containerProvider.Resolve<IDialogWithNoParentService>();

            AppleUIEnable = !Convert.ToBoolean(ConfigurationManager.AppSettings["AppleUIEnable"]);

            LanguageList.Add(new LanguageBar() { Name = "中文", Mark = "zh-CN" });
            LanguageList.Add(new LanguageBar() { Name = "English", Mark = "en-US" });
            LanguageList.Add(new LanguageBar() { Name = "Tiếng Việt", Mark = "vi-VN" });

            CurrentLanguageSelect = LanguageList.Where(x => x.Mark == System.Threading.Thread.CurrentThread.CurrentUICulture.Name).FirstOrDefault() ?? LanguageList.First();

            eventAggregator.GetEvent<NavigateSyncUpEvent>().Subscribe(x =>
            {
                if (ToggleButtonIsCheck != x.IsOpen)
                {
                    ToggleButtonIsCheck = x.IsOpen;
                }
            }, ThreadOption.UIThread, true, x => x.Fliter.Equals("NavigateSync"));

            eventAggregator.GetEvent<MessageEvent>().Subscribe(x =>
            {
                switch (x.Message)
                {
                    case "QuickLogin_Dev":
                        if (permissionService.CheckPermission(PermissionLevel.Developer)) return;
                        permissionService.CurrentUserName = "QuickDev";
                        permissionService.CurrentPermission = PermissionLevel.Developer;
                        logService.WriteLog(LogTypes.DB.ToString(), $@"快捷登录Dev用户", MessageDegree.INFO);
                        eventAggregator.SendPremissionChangeEvent(message: permissionService.CurrentUserName);
                        break;
                    case "TimerLoginOut":
                        permissionService.CurrentUserName = "";
                        permissionService.CurrentPermission = PermissionLevel.NoPermission;
                        logService.WriteLog(LogTypes.DB.ToString(), $@"定时权限自动退出", MessageDegree.INFO);
                        eventAggregator.SendPremissionChangeEvent(message: "未登录");
                        break;
                    default:
                        break;
                }
            }, ThreadOption.UIThread, true, m => { return m.Filter.Equals("QuickLoginOrOut") && appleUIEnable; });

            eventAggregator.GetEvent<MessageEvent>().Subscribe(x =>
            {
                CurrentLanguageSelect = LanguageList.Where(y => y.Mark == x.Message).FirstOrDefault() ?? LanguageList.First();
                ExecuteLanguageChangeCommand();
            }, ThreadOption.UIThread, true, x => x.Filter.Equals("LanguageChanged"));
        }

        private DelegateCommand changeLeftDrawerCommand;
        public DelegateCommand ChangeLeftDrawerCommand =>
            changeLeftDrawerCommand ?? (changeLeftDrawerCommand = new DelegateCommand(ExecuteChangeLeftDrawerCommand));

        void ExecuteChangeLeftDrawerCommand()
        {
            if (!permissionService.CheckPermission(PermissionLevel.Developer))
            {
                snackbarMessageQueue.EnqueueEx("权限不足");
                ToggleButtonIsCheck = false;
                return;
            }
            eventAggregator.GetEvent<NavigateSyncDownEvent>().Publish(new NavigateSyncModel() { IsOpen = ToggleButtonIsCheck, Fliter = "NavigateSync" });
        }
        private DelegateCommand loginInOutCommand;
        public DelegateCommand LoginInOutCommand =>
            loginInOutCommand ?? (loginInOutCommand = new DelegateCommand(ExecuteLoginInOutCommand));
        //登录命令
        async void ExecuteLoginInOutCommand()
        {
            if (loginContent == "Login".TryFindResourceEx())
            {
                var dialogResult = await dialogHostService.ShowHostDialog("DialogHostLoginView", null, "Root");
                if (dialogResult.Result == ButtonResult.OK)
                {
                    switch (dialogResult.Parameters.GetValue<UserInfoEntity>("user").Authority)
                    {
                        case "管理员":
                            permissionService.CurrentPermission = PermissionLevel.Admin;
                            logService.WriteLog(LogTypes.DB.ToString(), "登陆管理员账户", MessageDegree.INFO);
                            break;
                        case "操作员":
                            permissionService.CurrentPermission = PermissionLevel.Operator;
                            logService.WriteLog(LogTypes.DB.ToString(), "登陆操作员账户", MessageDegree.INFO);
                            break;
                        case "开发人员":
                            permissionService.CurrentPermission = PermissionLevel.Developer;
                            logService.WriteLog(LogTypes.DB.ToString(), "登陆开发人员账户", MessageDegree.INFO);
                            break;
                        default:
                            permissionService.CurrentPermission = PermissionLevel.NoPermission;
                            break;
                    }
                    UserName = dialogResult.Parameters.GetValue<UserInfoEntity>("user").UserName;
                    permissionService.CurrentUserName = UserName;
                    LoginContent = "LoginOut".TryFindResourceEx();
                }
            }
            else
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"用户{UserName}登出", MessageDegree.INFO);
                permissionService.CurrentPermission = PermissionLevel.NoPermission;
                LoginContent = "Login".TryFindResourceEx();
                UserName = "DefaultUserName".TryFindResourceEx();
                permissionService.CurrentUserName = UserName;
            }
            eventAggregator.SendPremissionChangeEvent(message: UserName);
        }

        private DelegateCommand _GlobalLogCommand;
        public DelegateCommand GlobalLogCommand =>
            _GlobalLogCommand ?? (_GlobalLogCommand = new DelegateCommand(ExecuteGlobalLogCommand).ObservesCanExecute(() => ButtonEnable));

        void ExecuteGlobalLogCommand()
        {
            ButtonEnable = false;
            dialogWithNoParentService.ShowWithNoParent(nameof(GlobalLogView), null, r => { ButtonEnable = true; });
        }

        private DelegateCommand _LanguageChangeCommand;
        public DelegateCommand LanguageChangeCommand =>
            _LanguageChangeCommand ?? (_LanguageChangeCommand = new DelegateCommand(ExecuteLanguageChangeCommand));

        void ExecuteLanguageChangeCommand()
        {
            List<ResourceDictionary> dictionaryList = new List<ResourceDictionary>();
            foreach (ResourceDictionary dictionary in Application.Current.Resources.MergedDictionaries)
            {
                if (dictionary.Source != null)
                {
                    dictionaryList.Add(dictionary);
                }
            }
            string requestedCulture = "";
            switch (CurrentLanguageSelect.Mark)
            {
                case "zh-CN":
                    requestedCulture = @"Resources\Language\zh-CN.xaml";
                    System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("zh-CN");
                    break;
                case "en-US":
                    requestedCulture = @"Resources\Language\en-US.xaml";
                    System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
                    break;
                case "vi-VN":
                    requestedCulture = @"Resources\Language\vi-VN.xaml";
                    System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("vi-VN");
                    break;
                default:
                    requestedCulture = @"Resources\Language\zh-CN.xaml";
                    System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("zh-CN");
                    break;
            }
            ResourceDictionary resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString.Equals(requestedCulture));
            Application.Current.Resources.MergedDictionaries.Remove(resourceDictionary);
            Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
            SaveSetting("Language", CurrentLanguageSelect.Mark);
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}切换到语言{CurrentLanguageSelect.Name}", MessageDegree.INFO);
            eventAggregator.GetEvent<MessageEvent>().Publish(new MessageModel() { Filter = "LanguageChangeNavigate" });
        }

        private void SaveSetting(string name, string data)
        {
            Configuration cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            cfg.AppSettings.Settings[name].Value = data;
            cfg.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
