using LogServiceInterface;
using PermissionServiceInterface;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using SpinCoaterAndDeveloper.App.Common.Models;
using SpinCoaterAndDeveloper.App.Views;
using SpinCoaterAndDeveloper.App.Views.Loading;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.Event;
using SpinCoaterAndDeveloper.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class AppleSettingViewModel : BindableBase, INavigationAware
    {
        private MenuBar CurrentSubView { get; set; }

        private readonly IRegionManager regionManager;
        private readonly ILogService logService;
        private readonly IPermissionService permissionService;
        private readonly IEventAggregator eventAggregator;
        public ObservableCollection<MenuBar> SettingMenuBars { get; set; } = new ObservableCollection<MenuBar>();
        public AppleSettingViewModel(IContainerProvider containerProvider)
        {
            this.regionManager = containerProvider.Resolve<IRegionManager>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.permissionService = containerProvider.Resolve<IPermissionService>();
            this.eventAggregator = containerProvider.Resolve<IEventAggregator>();

            SettingMenuBars.Add(new MenuBar() { Icon = "AlphaABoxOutline", LanguageKey = "AxisDebug", NameSpace = nameof(MCAxisDebugView) });
            SettingMenuBars.Add(new MenuBar() { Icon = "SwapHorizontalCircleOutline", LanguageKey = "IODebug", NameSpace = nameof(MCIODebugView) });
            SettingMenuBars.Add(new MenuBar() { Icon = "FormatParagraphSpacing", LanguageKey = "CylinderDebug", NameSpace = nameof(MCCylinderDebugView) });
            SettingMenuBars.Add(new MenuBar() { Icon = "ContentSaveCogOutline", LanguageKey = "ParmeterDebug", NameSpace = nameof(MCParmeterDebugView) });
            SettingMenuBars.Add(new MenuBar() { Icon = "CogTransferOutline", LanguageKey = "MovementPointDebug", NameSpace = nameof(MCMovementPointDebugView) });
            SettingMenuBars.Add(new MenuBar() { Icon = "LayersOffOutline", LanguageKey = "FunctionShieldDebug", NameSpace = nameof(MCFunctionShieldDebugView) });
            SettingMenuBars.Add(new MenuBar() { Icon = "TextSearch", LanguageKey = "SerialPort", NameSpace = "SerialPortDebugView" });
            SettingMenuBars.Add(new MenuBar() { Icon = "Thermometer", LanguageKey = "TemperatureController", NameSpace = "TemperatureControllerView" });
            SettingMenuBars.Add(new MenuBar() { Icon = "Thermometer", LanguageKey = "GluePump", NameSpace = "GluePumpView" });
            SettingMenuBars.Add(new MenuBar() { Icon = "CookieCogOutline", LanguageKey = "SystemCfg", NameSpace = nameof(SystemSettingView) });

            eventAggregator.GetEvent<MessageEvent>().Subscribe(x => { LanguageChanged(); }, ThreadOption.UIThread, true, x => x.Filter.Equals("LanguageChangeNavigate"));
        }

        private DelegateCommand<MenuBar> _NavigateCommand;
        public DelegateCommand<MenuBar> NavigateCommand =>
            _NavigateCommand ?? (_NavigateCommand = new DelegateCommand<MenuBar>(ExecuteNavigateCommand));

        void ExecuteNavigateCommand(MenuBar parameter)
        {
            if (parameter == null || parameter.NameSpace == CurrentSubView.NameSpace) return;
            regionManager.RequestNavigate("SettingViewRegion", parameter.NameSpace, x =>
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
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}进入调试页面", MessageDegree.INFO);
            if (CurrentSubView == null)
                regionManager.RequestNavigate("SettingViewRegion", nameof(MCAxisDebugView), x =>
                {
                    if (x.Result.HasValue && (bool)x.Result)
                    {
                        CurrentSubView = SettingMenuBars.Where(v => v.NameSpace == nameof(MCAxisDebugView)).FirstOrDefault();
                        CurrentSubView.IsShow = true;
                    }
                });
            else
                regionManager.RequestNavigate("SettingViewRegion", CurrentSubView.NameSpace);
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}离开调试页面", MessageDegree.INFO);
            regionManager.RequestNavigate("SettingViewRegion", nameof(AppleNullView));
        }

        private void LanguageChanged()
        {
            SettingMenuBars.ToList().ForEach(x => x.Title = x.LanguageKey.TryFindResourceEx());
        }
    }
}
