using AutoMapper;
using DataBaseServiceInterface;
using LogServiceInterface;
using MaterialDesignThemes.Wpf;
using MotionCardServiceInterface;
using PermissionServiceInterface;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using Renci.SshNet.Messages;
using SpinCoaterAndDeveloper.App.Common.Models;
using SpinCoaterAndDeveloper.App.Extensions;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using SpinCoaterAndDeveloper.Shared.Extensions;
using SpinCoaterAndDeveloper.Shared.Models.MotionControlModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class MCParmeterDebugViewModel : BindableBase, INavigationAware
    {
        private readonly IMapper mapper;
        private readonly ILogService logService;
        private readonly IPermissionService permissionService;
        private readonly IDataBaseService dataBaseService;
        private readonly ISnackbarMessageQueue snackbarMessageQueue;

        private string _ParmeterFilter;
        public string ParmeterFilter
        {
            get { return _ParmeterFilter; }
            set { SetProperty(ref _ParmeterFilter, value); }
        }
        public ObservableCollection<string> ParmeterGroupCollection { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<ParmeterMonitorModel> ParmeterMonitorCollection { get; set; } = new ObservableCollection<ParmeterMonitorModel>();
        public MCParmeterDebugViewModel(IContainerProvider containerProvider)
        {
            this.mapper = containerProvider.Resolve<IMapper>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.permissionService = containerProvider.Resolve<IPermissionService>();
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
            this.snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();

            CollectionView collectionView = CollectionViewSource.GetDefaultView(ParmeterMonitorCollection) as CollectionView;
            collectionView.Filter = x =>
            {
                if (string.IsNullOrWhiteSpace(ParmeterFilter)) return true;
                return (x as ParmeterMonitorModel).ShowOnUIName.Contains(ParmeterFilter);
            };
        }

        private DelegateCommand _ParmeterFilterCommand;
        public DelegateCommand ParmeterFilterCommand =>
            _ParmeterFilterCommand ?? (_ParmeterFilterCommand = new DelegateCommand(ExecuteParmeterFilterCommand));

        void ExecuteParmeterFilterCommand()
        {
            CollectionViewSource.GetDefaultView(ParmeterMonitorCollection).Refresh();
        }

        private DelegateCommand<string> _ParmeterGroupChangedCommand;
        public DelegateCommand<string> ParmeterGroupChangedCommand =>
            _ParmeterGroupChangedCommand ?? (_ParmeterGroupChangedCommand = new DelegateCommand<string>(ExecuteParmeterGroupChangedCommand));

        void ExecuteParmeterGroupChangedCommand(string parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter))
            {
                return;
            }
            var pars = dataBaseService.Db.Queryable<ParmeterInfoEntity>().Includes(x => x.ProductInfo).Where(x => x.Group == parameter && x.ProductInfo.Select == true).OrderBy(x => x.Number).OrderBy(x => x.Id).ToList();
            ParmeterMonitorCollection.Clear();
            mapper.Map(pars, ParmeterMonitorCollection);
            snackbarMessageQueue.EnqueueEx("筛选成功");
        }

        private DelegateCommand _ShowAllParmetersCommand;
        public DelegateCommand ShowAllParmetersCommand =>
            _ShowAllParmetersCommand ?? (_ShowAllParmetersCommand = new DelegateCommand(ExecuteShowAllParmetersCommand));

        void ExecuteShowAllParmetersCommand()
        {
            ParmeterFilter = "";
            GetParmeters();
            ParmeterGroupCollection.Clear();
            GetParmeterGroup();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}显示所有参数成功", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("显示所有参数成功");
        }

        private DelegateCommand _SaveAllDataCommand;
        public DelegateCommand SaveAllDataCommand =>
            _SaveAllDataCommand ?? (_SaveAllDataCommand = new DelegateCommand(ExecuteSaveAllDataCommand));

        async void ExecuteSaveAllDataCommand()
        {
            foreach (var par in ParmeterMonitorCollection)
            {
                if (par.ErrorMark == false)
                {
                    logService.WriteLog(LogTypes.DB.ToString(), $@"{par.Name}数据非法,无法保存", MessageDegree.ERROR);
                    snackbarMessageQueue.EnqueueEx($@"{par.ShowOnUIName}{"数据非法,无法保存".TryFindResourceEx()}");
                    return;
                }
            }

            dataBaseService.Db.Queryable<ParmeterInfoEntity>().Where(x => x.ProductInfo.Select == true).OrderBy(x => x.Number).OrderBy(x => x.Id).ToList().ForEach(orgItem =>
            {
                var updateItem = ParmeterMonitorCollection.Where(x => x.Id == orgItem.Id).FirstOrDefault();
                if (updateItem != null) ParCompare(orgItem, updateItem);
            });

            await dataBaseService.Db.Updateable(mapper.Map<List<ParmeterInfoEntity>>(ParmeterMonitorCollection)).ExecuteCommandAsync();
            //更新全局参数字典中的值
            foreach (var item in ParmeterMonitorCollection)
            {
                if (GlobalValues.MCParmeterDicCollection[item.Name].Data != item.Data)
                    GlobalValues.MCParmeterDicCollection[item.Name].Data = item.Data;
            }
            logService.WriteLog(LogTypes.DB.ToString(), $@"保存成功", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("保存成功");
        }

        private DelegateCommand _DataCheckCommand;
        public DelegateCommand DataCheckCommand =>
            _DataCheckCommand ?? (_DataCheckCommand = new DelegateCommand(ExecuteDataCheckCommand));

        void ExecuteDataCheckCommand()
        {
            foreach (var par in ParmeterMonitorCollection)
            {
                switch (par.DataType)
                {
                    case ParmeterType.String:
                        par.ErrorMark = true;
                        break;
                    case ParmeterType.Bool:
                        par.ErrorMark = bool.TryParse(par.Data, out _);
                        break;
                    case ParmeterType.Byte:
                        par.ErrorMark = byte.TryParse(par.Data, out _);
                        break;
                    case ParmeterType.Short:
                        par.ErrorMark = short.TryParse(par.Data, out _);
                        break;
                    case ParmeterType.Int:
                        par.ErrorMark = int.TryParse(par.Data, out _);
                        break;
                    case ParmeterType.Double:
                        par.ErrorMark = double.TryParse(par.Data, out _);
                        break;
                    case ParmeterType.Float:
                        par.ErrorMark = float.TryParse(par.Data, out _);
                        break;
                    case ParmeterType.DateTime:
                        par.ErrorMark = DateTime.TryParse(par.Data, out _);
                        break;
                    default:
                        break;
                }
            }
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                ParmeterGroupCollection.Clear();
                GetParmeterGroup();
                GetParmeters();
            }), System.Windows.Threading.DispatcherPriority.Render);
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}进入参数调试页面", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("参数调试页面保存,保存后生效.");
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {

        }

        private void GetParmeters()
        {
            ParmeterMonitorCollection.Clear();
            var pars = dataBaseService.Db.Queryable<ParmeterInfoEntity>().Includes(x => x.ProductInfo).Where(x => x.ProductInfo.Select == true).OrderBy(x => x.Number).OrderBy(x => x.Id).ToList();
            mapper.Map(pars, ParmeterMonitorCollection);
        }

        private void GetParmeterGroup()
        {
            var parGroups = dataBaseService.Db.Queryable<ParmeterInfoEntity>().Where(x => x.ProductInfo.Select == true).Distinct().Select(x => x.Group).ToList();
            parGroups.ForEach(x => { if (!string.IsNullOrWhiteSpace(x) && !ParmeterGroupCollection.Contains(x)) ParmeterGroupCollection.Add(x); });
        }

        private void ParCompare(ParmeterInfoEntity orgItem, ParmeterMonitorModel updateItem)
        {
            if (orgItem.Number != updateItem.Number) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Number: {orgItem.Number}=>{updateItem.Number}", MessageDegree.INFO);
            if (orgItem.Name != updateItem.Name) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Name: {orgItem.Name}=>{updateItem.Name}", MessageDegree.INFO);
            if (orgItem.CNName != updateItem.CNName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} CNName: {orgItem.CNName}=>{updateItem.CNName}", MessageDegree.INFO);
            if (orgItem.ENName != updateItem.ENName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} ENName: {orgItem.ENName}=>{updateItem.ENName}", MessageDegree.INFO);
            if (orgItem.VNName != updateItem.VNName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} VNName: {orgItem.VNName}=>{updateItem.VNName}", MessageDegree.INFO);
            if (orgItem.XXName != updateItem.XXName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} XXName: {orgItem.XXName}=>{updateItem.XXName}", MessageDegree.INFO);
            if (orgItem.Data != updateItem.Data) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Data: {orgItem.Data}=>{updateItem.Data}", MessageDegree.INFO);
            if (orgItem.DataType != updateItem.DataType) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} DataType: {orgItem.DataType}=>{updateItem.DataType}", MessageDegree.INFO);
            if (orgItem.Unit != updateItem.Unit) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Unit: {orgItem.Unit}=>{updateItem.Unit}", MessageDegree.INFO);
            if (orgItem.Group != updateItem.Group) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Group: {orgItem.Group}=>{updateItem.Group}", MessageDegree.INFO);
            if (orgItem.Backup != updateItem.Backup) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Backup: {orgItem.Backup}=>{updateItem.Backup}", MessageDegree.INFO);
            if (orgItem.Tag != updateItem.Tag) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Tag: {orgItem.Tag}=>{updateItem.Tag}", MessageDegree.INFO);
        }
    }
}
