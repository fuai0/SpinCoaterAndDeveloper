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

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class AppleVisionViewModel : BindableBase, INavigationAware
    {
        private MenuBar CurrentSubView { get; set; }

        private readonly IRegionManager regionManager;
        private readonly ILogService logService;
        private readonly IPermissionService permissionService;
        private readonly IEventAggregator eventAggregator;

        public ObservableCollection<MenuBar> VisionMenuBars { get; set; } = new ObservableCollection<MenuBar>();
        public AppleVisionViewModel(IContainerProvider containerProvider)
        {
            this.regionManager = containerProvider.Resolve<IRegionManager>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.permissionService = containerProvider.Resolve<IPermissionService>();
            this.eventAggregator = containerProvider.Resolve<IEventAggregator>();

            VisionMenuBars.Add(new MenuBar() { Icon = "Checkerboard", LanguageKey = "VisionCalibration", NameSpace = nameof(VisionCalibrationView) });
            VisionMenuBars.Add(new MenuBar() { Icon = "CrosshairsGps", LanguageKey = "VisionCompensation", NameSpace = nameof(VisionCompensationView) });
            VisionMenuBars.Add(new MenuBar() { Icon = "CameraFlipOutline", LanguageKey = "VisionTest", NameSpace = nameof(VisionTestView) });

            eventAggregator.GetEvent<MessageEvent>().Subscribe(x => { LanguageChanged(); }, ThreadOption.UIThread, true, x => x.Filter.Equals("LanguageChangeNavigate"));
        }

        private DelegateCommand<MenuBar> _NavigateCommand;
        public DelegateCommand<MenuBar> NavigateCommand =>
            _NavigateCommand ?? (_NavigateCommand = new DelegateCommand<MenuBar>(ExecuteNavigateCommand));

        void ExecuteNavigateCommand(MenuBar parameter)
        {
            if (parameter == null || parameter.NameSpace == CurrentSubView.NameSpace) return;

            regionManager.RequestNavigate("VisionViewRegion", parameter.NameSpace, x =>
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
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}进入Vision页面", MessageDegree.INFO);
            if (CurrentSubView == null)
                regionManager.RequestNavigate("VisionViewRegion", nameof(VisionCalibrationView), x =>
                {
                    if (x.Result.HasValue && (bool)x.Result)
                    {
                        CurrentSubView = VisionMenuBars.Where(v => v.NameSpace == nameof(VisionCalibrationView)).FirstOrDefault();
                        CurrentSubView.IsShow = true;
                    }
                });
            else
                regionManager.RequestNavigate("VisionViewRegion", CurrentSubView.NameSpace);
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}离开Vision页面", MessageDegree.INFO);
            regionManager.RequestNavigate("VisionViewRegion", nameof(AppleNullView));
        }

        private void LanguageChanged()
        {
            VisionMenuBars.ToList().ForEach(x => x.Title = x.LanguageKey.TryFindResourceEx());
        }
    }
}
