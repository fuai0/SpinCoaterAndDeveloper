using LogServiceInterface;
using PermissionServiceInterface;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using SpinCoaterAndDeveloper.App.Common.Models;
using SpinCoaterAndDeveloper.App.Views;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.Event;
using SpinCoaterAndDeveloper.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using static Org.BouncyCastle.Crypto.Digests.SkeinEngine;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class AppleConfigViewModel : BindableBase, INavigationAware
    {
        private MenuBar CurrentSubView { get; set; }

        private readonly IRegionManager regionManager;
        private readonly ILogService logService;
        private readonly IPermissionService permissionService;
        private readonly IEventAggregator eventAggregator;

        public ObservableCollection<MenuBar> ConfigMenuBars { get; set; } = new ObservableCollection<MenuBar>();

        public AppleConfigViewModel(IContainerProvider containerProvider)
        {
            this.regionManager = containerProvider.Resolve<IRegionManager>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.permissionService = containerProvider.Resolve<IPermissionService>();
            this.eventAggregator = containerProvider.Resolve<IEventAggregator>();

            ConfigMenuBars.Add(new MenuBar() { Icon = "AlphaACircleOutline", LanguageKey = "AxisCfg", NameSpace = nameof(MCAxisConfigView) });
            ConfigMenuBars.Add(new MenuBar() { Icon = "LocationEnter", LanguageKey = "IOInputCfg", NameSpace = nameof(MCIOInputConfigView) });
            ConfigMenuBars.Add(new MenuBar() { Icon = "LocationExit", LanguageKey = "IOOutputCfg", NameSpace = nameof(MCIOOutputConfigView) });
            ConfigMenuBars.Add(new MenuBar() { Icon = "ArrowExpandVertical", LanguageKey = "CylinderCfg", NameSpace = nameof(MCCylinderConfigView) });
            ConfigMenuBars.Add(new MenuBar() { Icon = "ContentSaveEditOutline", LanguageKey = "ParmeterCfg", NameSpace = nameof(MCParmeterConfigView) });
            ConfigMenuBars.Add(new MenuBar() { Icon = "Cogs", LanguageKey = "MCPointCfg", NameSpace = nameof(MCMovementPointConfigView) });
            ConfigMenuBars.Add(new MenuBar() { Icon = "LayersOffOutline", LanguageKey = "FunctionShieldCfg", NameSpace = nameof(MCFunctionShieldConfigView) });
            ConfigMenuBars.Add(new MenuBar() { Icon = "BorderOutside", LanguageKey = "InterpolationPathView", NameSpace = nameof(MCInterpolationPathView) });
            ConfigMenuBars.Add(new MenuBar() { Icon = "AccountDetailsOutline", LanguageKey = "UserManager", NameSpace = nameof(UserSettingView) });

            eventAggregator.GetEvent<MessageEvent>().Subscribe(x => { LanguageChanged(); }, ThreadOption.UIThread, true, x => x.Filter.Equals("LanguageChangeNavigate"));
        }

        private DelegateCommand<MenuBar> _NavigateCommand;
        public DelegateCommand<MenuBar> NavigateCommand =>
            _NavigateCommand ?? (_NavigateCommand = new DelegateCommand<MenuBar>(ExecuteNavigateCommand));

        void ExecuteNavigateCommand(MenuBar parameter)
        {
            if (parameter == null || parameter.NameSpace == CurrentSubView.NameSpace) return;

            regionManager.RequestNavigate("ConfigViewRegion", parameter.NameSpace, x =>
            {
                if (x.Result.HasValue && (bool)x.Result)
                {
                    CurrentSubView.IsShow = false;
                    CurrentSubView = parameter;
                    CurrentSubView.IsShow = true;
                }
            });
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            LanguageChanged();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}进入配置页面", MessageDegree.INFO);
            if (CurrentSubView == null)
                regionManager.RequestNavigate("ConfigViewRegion", nameof(MCAxisConfigView), x =>
                {
                    if (x.Result.HasValue && (bool)x.Result)
                    {
                        CurrentSubView = ConfigMenuBars.Where(v => v.NameSpace == nameof(MCAxisConfigView)).FirstOrDefault();
                        CurrentSubView.IsShow = true;
                    }
                });
            else
                regionManager.RequestNavigate("ConfigViewRegion", CurrentSubView.NameSpace);
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}离开配置页面", MessageDegree.INFO);
            regionManager.RequestNavigate("ConfigViewRegion", nameof(AppleNullView));
        }

        private void LanguageChanged()
        {
            ConfigMenuBars.ToList().ForEach(x => x.Title = x.LanguageKey.TryFindResourceEx());
        }
    }
}
