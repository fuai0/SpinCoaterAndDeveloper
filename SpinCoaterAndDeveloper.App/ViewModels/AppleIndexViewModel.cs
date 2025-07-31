using LogServiceInterface;
using MaterialDesignThemes.Wpf;
using PermissionServiceInterface;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using SpinCoaterAndDeveloper.App.Extensions;
using SpinCoaterAndDeveloper.App.Views;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class AppleIndexViewModel : BindableBase, IConfirmNavigationRequest
    {
        private readonly IRegionManager regionManager;
        private readonly ILogService logService;
        private readonly IPermissionService permissionService;
        private readonly ISnackbarMessageQueue snackbarMessageQueue;
        public AppleIndexViewModel(IContainerProvider containerProvider)
        {
            this.regionManager = containerProvider.Resolve<IRegionManager>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.permissionService = containerProvider.Resolve<IPermissionService>();
            this.snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();

            //注册首页默认页面
            regionManager.RegisterViewWithRegion("AppleIndexLeft1Region", nameof(OptionUphView));
            regionManager.RegisterViewWithRegion("AppleIndexLeft2Region", nameof(OptionLogView));
            regionManager.RegisterViewWithRegion("AppleIndexRight1Region", nameof(OptionFunctionShieldView));
            regionManager.RegisterViewWithRegion("AppleIndexRight2Region", nameof(OptionProduceCTView));
            regionManager.RegisterViewWithRegion("AppleIndexRight3Region", nameof(OptionCpuDimmUsageView));
            regionManager.RegisterViewWithRegion("AppleIndexRight4Region", nameof(OptionHddUsageView));
            //如果需要导航页面到主界面,请到App.xaml.cs中OnInitialized()方法内使用导航方法
        }

        public void ConfirmNavigationRequest(NavigationContext navigationContext, Action<bool> continuationCallback)
        {
            if (GlobalValues.MachineStatus == FSMStateCode.Alarming || GlobalValues.MachineStatus == FSMStateCode.Running)
            {
                snackbarMessageQueue.EnqueueEx("设备运行中,请勿切换页面");
                logService.WriteLog(LogTypes.DB.ToString(), $@"设备运行中,请勿切换页面", MessageDegree.WARN);
                continuationCallback(false);
                return;
            }
            continuationCallback(true);
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}离开首页", MessageDegree.INFO);
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}进入首页", MessageDegree.INFO);
        }

        ~AppleIndexViewModel()
        {

        }
    }
}
