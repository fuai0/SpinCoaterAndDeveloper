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

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class AppleDataViewModel : BindableBase, INavigationAware
    {
        private MenuBar CurrentSubView { get; set; }

        private readonly IRegionManager regionManager;
        private readonly ILogService logService;
        private readonly IPermissionService permissionService;
        private readonly IEventAggregator eventAggregator;
        public ObservableCollection<MenuBar> DataMenuBars { get; set; } = new ObservableCollection<MenuBar>();
        public AppleDataViewModel(IContainerProvider containerProvider)
        {
            this.regionManager = containerProvider.Resolve<IRegionManager>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.permissionService = containerProvider.Resolve<IPermissionService>();
            this.eventAggregator = containerProvider.Resolve<IEventAggregator>();

            DataMenuBars.Add(new MenuBar() { Icon = "MathLog", LanguageKey = "LogSearch", NameSpace = nameof(LogSearchView) });
            DataMenuBars.Add(new MenuBar() { Icon = "FormatListBulletedType", LanguageKey = "ProductInfoSearch", NameSpace = nameof(ProductInfoSearchView) });
            DataMenuBars.Add(new MenuBar() { Icon = "ChartBar", LanguageKey = "ProductivityListSearch", NameSpace = nameof(ProductivityListSearchView) });
            DataMenuBars.Add(new MenuBar() { Icon = "FormatListBulletedSquare", LanguageKey = "ProductivityChartSearch", NameSpace = nameof(ProductivityChartSearchView) });
            DataMenuBars.Add(new MenuBar() { Icon = "ChartBarStacked", LanguageKey = "CTListSearch", NameSpace = nameof(ProductCTListSearchView) });
            DataMenuBars.Add(new MenuBar() { Icon = "FormatListBulleted", LanguageKey = "CTChartSearch", NameSpace = nameof(ProductCTChartSearchView) });

            eventAggregator.GetEvent<MessageEvent>().Subscribe(x => { LanguageChanged(); }, ThreadOption.UIThread, true, x => x.Filter.Equals("LanguageChangeNavigate"));
        }

        private DelegateCommand<MenuBar> _NavigateCommand;
        public DelegateCommand<MenuBar> NavigateCommand =>
            _NavigateCommand ?? (_NavigateCommand = new DelegateCommand<MenuBar>(ExecuteNavigateCommand));

        void ExecuteNavigateCommand(MenuBar parameter)
        {
            if (parameter == null || parameter.NameSpace == CurrentSubView.NameSpace) return;
            regionManager.RequestNavigate("DataViewRegion", parameter.NameSpace, x =>
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
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}进入数据页面", MessageDegree.INFO);
            if (CurrentSubView == null)
                regionManager.RequestNavigate("DataViewRegion", nameof(LogSearchView), x =>
                {
                    if (x.Result.HasValue && (bool)x.Result)
                    {
                        CurrentSubView = DataMenuBars.Where(v => v.NameSpace == nameof(LogSearchView)).FirstOrDefault();
                        CurrentSubView.IsShow = true;
                    }
                });
            else
                regionManager.RequestNavigate("DataViewRegion", CurrentSubView.NameSpace);
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}离开数据页面", MessageDegree.INFO);
            regionManager.RequestNavigate("DataViewRegion", nameof(AppleNullView));
        }

        private void LanguageChanged()
        {
            DataMenuBars.ToList().ForEach(x => x.Title = x.LanguageKey.TryFindResourceEx());
        }
    }
}
