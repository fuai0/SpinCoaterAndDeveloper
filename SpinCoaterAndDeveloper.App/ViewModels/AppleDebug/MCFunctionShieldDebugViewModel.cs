using AutoMapper;
using DataBaseServiceInterface;
using LogServiceInterface;
using MaterialDesignThemes.Wpf;
using PermissionServiceInterface;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using SpinCoaterAndDeveloper.App.Common.Models;
using SpinCoaterAndDeveloper.App.Extensions;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class MCFunctionShieldDebugViewModel : BindableBase, INavigationAware
    {
        private readonly IMapper mapper;
        private readonly ILogService logService;
        private readonly IPermissionService permissionService;
        private readonly IDataBaseService dataBaseService;
        private readonly ISnackbarMessageQueue snackbarMessageQueue;

        private string _FunctionShieldFilter;
        public string FunctionShieldFilter
        {
            get { return _FunctionShieldFilter; }
            set { SetProperty(ref _FunctionShieldFilter, value); }
        }
        public ObservableCollection<string> FunctionShieldGroupCollection { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<FunctionShieldMonitorModel> FunctionShieldMonitorCollection { get; set; } = new ObservableCollection<FunctionShieldMonitorModel>();

        public MCFunctionShieldDebugViewModel(IContainerProvider containerProvider)
        {
            this.mapper = containerProvider.Resolve<IMapper>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.permissionService = containerProvider.Resolve<IPermissionService>();
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
            this.snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();

            CollectionView collectionView = CollectionViewSource.GetDefaultView(FunctionShieldMonitorCollection) as CollectionView;
            collectionView.Filter = x =>
            {
                if (string.IsNullOrWhiteSpace(FunctionShieldFilter)) return true;
                return (x as FunctionShieldMonitorModel).ShowOnUIName.Contains(FunctionShieldFilter);
            };
        }

        private DelegateCommand<string> _FunctionShieldGroupChangedCommand;
        public DelegateCommand<string> FunctionShieldGroupChangedCommand =>
            _FunctionShieldGroupChangedCommand ?? (_FunctionShieldGroupChangedCommand = new DelegateCommand<string>(ExecuteFunctionShieldGroupChangedCommand));

        void ExecuteFunctionShieldGroupChangedCommand(string parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter)) return;
            var functionShields = dataBaseService.Db.Queryable<FunctionShieldEntity>().Includes(x => x.ProductInfo).Where(x => x.Group == parameter && x.ProductInfo.Select == true).OrderBy(x => x.Id).ToList();
            FunctionShieldMonitorCollection.Clear();
            mapper.Map(functionShields, FunctionShieldMonitorCollection);
            snackbarMessageQueue.EnqueueEx("筛选成功");
        }

        private DelegateCommand _FunctionShieldFilterCommand;
        public DelegateCommand FunctionShieldFilterCommand =>
            _FunctionShieldFilterCommand ?? (_FunctionShieldFilterCommand = new DelegateCommand(ExecuteFunctionShieldFilterCommand));

        void ExecuteFunctionShieldFilterCommand()
        {
            CollectionViewSource.GetDefaultView(FunctionShieldMonitorCollection).Refresh();
        }

        private DelegateCommand _ShowAllFunctionShieldsCommand;
        public DelegateCommand ShowAllFunctionShieldsCommand =>
            _ShowAllFunctionShieldsCommand ?? (_ShowAllFunctionShieldsCommand = new DelegateCommand(ExecuteShowAllFunctionShieldsCommand));

        void ExecuteShowAllFunctionShieldsCommand()
        {
            FunctionShieldFilter = "";
            GetFunctionShields();
            FunctionShieldGroupCollection.Clear();
            GetFunctionShieldGroup();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}显示所有参数成功", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("显示所有屏蔽功能成功");
        }

        private DelegateCommand _SaveAllDataCommand;
        public DelegateCommand SaveAllDataCommand =>
            _SaveAllDataCommand ?? (_SaveAllDataCommand = new DelegateCommand(ExecuteSaveAllDataCommand));

        async void ExecuteSaveAllDataCommand()
        {
            dataBaseService.Db.Queryable<FunctionShieldEntity>().Where(x => x.ProductInfo.Select == true).OrderBy(x => x.Id).ToList().ForEach(orgItem =>
            {
                var updateItem = FunctionShieldMonitorCollection.Where(x => x.Id == orgItem.Id).FirstOrDefault();
                if (updateItem != null) FunctionShieldCompare(orgItem, updateItem);
            });

            await dataBaseService.Db.Updateable(mapper.Map<List<FunctionShieldEntity>>(FunctionShieldMonitorCollection)).ExecuteCommandAsync();
            //更新全局功能屏蔽字典
            foreach (var item in FunctionShieldMonitorCollection)
            {
                if (GlobalValues.MCFunctionShieldDicCollection[item.Name].IsActive != item.IsActive)
                    GlobalValues.MCFunctionShieldDicCollection[item.Name].IsActive = item.IsActive;
            }
            logService.WriteLog(LogTypes.DB.ToString(), $@"保存成功", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("保存成功");
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {

        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                FunctionShieldGroupCollection.Clear();
                GetFunctionShieldGroup();
                GetFunctionShields();
            }), System.Windows.Threading.DispatcherPriority.Render);
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}进入功能屏蔽调试页面", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("共屏蔽调试页面保存,保存后生效.");
        }

        #region PrivateMethod
        private void GetFunctionShields()
        {
            FunctionShieldMonitorCollection.Clear();
            var functionShields = dataBaseService.Db.Queryable<FunctionShieldEntity>().Includes(x => x.ProductInfo).Where(x => x.ProductInfo.Select == true).OrderBy(x => x.Id).ToList();
            mapper.Map(functionShields, FunctionShieldMonitorCollection);
        }

        private void GetFunctionShieldGroup()
        {
            var functionShieldGroups = dataBaseService.Db.Queryable<FunctionShieldEntity>().Where(x => x.ProductInfo.Select == true).Distinct().Select(x => x.Group).ToList();
            functionShieldGroups.ForEach(x => { if (!string.IsNullOrWhiteSpace(x) && !FunctionShieldGroupCollection.Contains(x)) FunctionShieldGroupCollection.Add(x); });
        }

        private void FunctionShieldCompare(FunctionShieldEntity orgItem, FunctionShieldMonitorModel updateItem)
        {
            if (orgItem.Name != updateItem.Name) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Name: {orgItem.Name}=>{updateItem.Name}", MessageDegree.INFO);
            if (orgItem.CNName != updateItem.CNName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} CNName: {orgItem.CNName}=>{updateItem.CNName}", MessageDegree.INFO);
            if (orgItem.ENName != updateItem.ENName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} ENName: {orgItem.ENName}=>{updateItem.ENName}", MessageDegree.INFO);
            if (orgItem.VNName != updateItem.VNName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} VNName: {orgItem.VNName}=>{updateItem.VNName}", MessageDegree.INFO);
            if (orgItem.XXName != updateItem.XXName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} XXName: {orgItem.XXName}=>{updateItem.XXName}", MessageDegree.INFO);
            if (orgItem.IsActive != updateItem.IsActive) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} IsActive: {orgItem.IsActive}=>{updateItem.IsActive}", MessageDegree.INFO);
            if (orgItem.EnableOnUI != updateItem.EnableOnUI) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} EnableOnUI: {orgItem.EnableOnUI}=>{updateItem.EnableOnUI}", MessageDegree.INFO);
            if (orgItem.Group != updateItem.Group) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Group: {orgItem.Group}=>{updateItem.Group}", MessageDegree.INFO);
            if (orgItem.Backup != updateItem.Backup) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Backup: {orgItem.Backup}=>{updateItem.Backup}", MessageDegree.INFO);
        }
        #endregion
    }
}
