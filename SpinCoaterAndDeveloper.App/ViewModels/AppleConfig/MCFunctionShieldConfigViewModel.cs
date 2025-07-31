using AutoMapper;
using DataBaseServiceInterface;
using LogServiceInterface;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;
using NPOI.SS.UserModel;
using PermissionServiceInterface;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using PrismAutomationPlatformExt;
using SpinCoaterAndDeveloper.App.Common.Models;
using SpinCoaterAndDeveloper.App.Extensions;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using SpinCoaterAndDeveloper.Shared.Models.MotionControlModels;
using SpinCoaterAndDeveloper.Shared.Services.MotionResourceInitService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class MCFunctionShieldConfigViewModel : BindableBase, INavigationAware
    {
        private readonly IDialogHostService dialogHostService;
        private readonly IMapper mapper;
        private readonly IDataBaseService dataBaseService;
        private readonly ISnackbarMessageQueue snackbarMessageQueue;
        private readonly ILogService logService;
        private readonly IPermissionService permissionService;
        private readonly IMotionResourceInit motionResourceInit;

        #region Binding
        private bool _IsRightDrawerOpen;
        public bool IsRightDrawerOpen
        {
            get { return _IsRightDrawerOpen; }
            set { SetProperty(ref _IsRightDrawerOpen, value); }
        }
        private FunctionShieldInfoModel _NewFunctionShield;
        public FunctionShieldInfoModel NewFunctionShield
        {
            get { return _NewFunctionShield; }
            set { SetProperty(ref _NewFunctionShield, value); }
        }
        private FunctionShieldInfoModel _CurrentSelectFunctionShield;
        public FunctionShieldInfoModel CurrentSelectFunctionShield
        {
            get { return _CurrentSelectFunctionShield; }
            set { SetProperty(ref _CurrentSelectFunctionShield, value); }
        }
        #endregion

        public ObservableCollection<FunctionShieldInfoModel> FunctionShieldCollection { get; set; } = new ObservableCollection<FunctionShieldInfoModel>();
        public ObservableCollection<string> FunctionShieldGroupCollection { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<MessageItem> MessageList { get; set; } = new ObservableCollection<MessageItem>();
        public MCFunctionShieldConfigViewModel(IContainerProvider containerProvider)
        {
            this.mapper = containerProvider.Resolve<IMapper>();
            this.dialogHostService = containerProvider.Resolve<IDialogHostService>();
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
            this.snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.permissionService = containerProvider.Resolve<IPermissionService>();
            this.motionResourceInit = containerProvider.Resolve<IMotionResourceInit>();
        }

        private DelegateCommand _AddFunctionShieldCfgCommand;
        public DelegateCommand AddFunctionShieldCfgCommand =>
            _AddFunctionShieldCfgCommand ?? (_AddFunctionShieldCfgCommand = new DelegateCommand(ExecuteAddFunctionShieldCfgCommand));

        void ExecuteAddFunctionShieldCfgCommand()
        {
            IsRightDrawerOpen = true;
        }

        private DelegateCommand _AddFunctionShieldCommand;
        public DelegateCommand AddFunctionShieldCommand =>
            _AddFunctionShieldCommand ?? (_AddFunctionShieldCommand = new DelegateCommand(ExecuteAddFunctionShieldCommand));

        void ExecuteAddFunctionShieldCommand()
        {
            if (string.IsNullOrWhiteSpace(NewFunctionShield.Name))
            {
                snackbarMessageQueue.EnqueueEx("请输入名称");
                return;
            }
            if (Regex.IsMatch(NewFunctionShield.Name, "^\\d"))
            {
                snackbarMessageQueue.EnqueueEx("名称不合法,名称不能以数字开头");
                return;
            }
            if (dataBaseService.Db.Queryable<FunctionShieldEntity>().Where(x => x.Name == NewFunctionShield.Name && x.ProductInfo.Select == true).Any())
            {
                snackbarMessageQueue.EnqueueEx("名称重复");
                return;
            }
            var product = dataBaseService.Db.Queryable<ProductInfoEntity>().Where(x => x.Select == true).First();
            NewFunctionShield.ProductId = product.Id;
            dataBaseService.Db.Insertable(mapper.Map<FunctionShieldEntity>(NewFunctionShield)).ExecuteCommand();
            GetFunctionShields();
            GetFunctionShieldGroup();
            logService.WriteLog(LogTypes.DB.ToString(), $"用户{permissionService.CurrentUserName}创建轴{NewFunctionShield.Name}成功.\r\n{JsonConvert.SerializeObject(NewFunctionShield)}", MessageDegree.INFO);
            NewFunctionShield = new FunctionShieldInfoModel();
            snackbarMessageQueue.EnqueueEx("添加成功");
        }

        private DelegateCommand _DeleteFunctionShieldCommand;
        public DelegateCommand DeleteFunctionShieldCommand =>
            _DeleteFunctionShieldCommand ?? (_DeleteFunctionShieldCommand = new DelegateCommand(ExecuteDeleteFunctionShieldCommand));

        async void ExecuteDeleteFunctionShieldCommand()
        {
            if (CurrentSelectFunctionShield == null)
            {
                snackbarMessageQueue.EnqueueEx("请选中需要删除的功能");
                return;
            }
            var result = await dialogHostService.ShowHostDialog("DialogHostMessageView", new DialogParameters() { { "Title", "警告" }, { "Content", $"确认删除参数{CurrentSelectFunctionShield.Name}?" }, { "CancelInfo", "取消" }, { "SaveInfo", "确定" } });
            if (result.Result != ButtonResult.OK)
            {
                return;
            }
            dataBaseService.Db.Deleteable(mapper.Map<FunctionShieldEntity>(CurrentSelectFunctionShield)).ExecuteCommand();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}删除参数{CurrentSelectFunctionShield.Name}成功", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("删除成功");
            GetFunctionShields();
            GetFunctionShieldGroup();
            NewFunctionShield = new FunctionShieldInfoModel();
            CurrentSelectFunctionShield = null;
            UpdateGlobalMCFunctionShieldDic();
        }

        private DelegateCommand _SaveSelectedFunctionShieldCommand;
        public DelegateCommand SaveSelectedFunctionShieldCommand =>
            _SaveSelectedFunctionShieldCommand ?? (_SaveSelectedFunctionShieldCommand = new DelegateCommand(ExecuteSaveSelectedFunctionShieldCommand));

        void ExecuteSaveSelectedFunctionShieldCommand()
        {
            if (CurrentSelectFunctionShield == null)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"请选中需要保存的功能", MessageDegree.WARN);
                return;
            }
            if (Regex.IsMatch(CurrentSelectFunctionShield.Name, "^\\d"))
            {
                snackbarMessageQueue.EnqueueEx("名称不合法,名称不能以数字开头");
                return;
            }
            if (FunctionShieldCollection.GroupBy(x => x.Name).Where(x => x.Count() > 1).Count() > 0)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"名字重复,无法保存", MessageDegree.ERROR);
                snackbarMessageQueue.EnqueueEx("保存失败");
                return;
            }
            var org = dataBaseService.Db.Queryable<FunctionShieldEntity>().Where(x => x.Id == CurrentSelectFunctionShield.Id).First();
            FunctionShieldCompare(org, CurrentSelectFunctionShield);

            dataBaseService.Db.Updateable(mapper.Map<FunctionShieldEntity>(CurrentSelectFunctionShield)).ExecuteCommand();
            GetFunctionShieldGroup();
            UpdateGlobalMCFunctionShieldDic();
            logService.WriteLog(LogTypes.DB.ToString(), $"用户{permissionService.CurrentUserName}保存参数信息成功.", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("保存成功");
        }

        private DelegateCommand _SaveAllFunctionShiledCommand;
        public DelegateCommand SaveAllFunctionShiledCommand =>
            _SaveAllFunctionShiledCommand ?? (_SaveAllFunctionShiledCommand = new DelegateCommand(ExecuteSaveAllFunctionShiledCommand));

        void ExecuteSaveAllFunctionShiledCommand()
        {
            bool repeatName = FunctionShieldCollection.GroupBy(x => x.Name).Where(x => x.Count() > 1).Count() > 0;
            if (repeatName)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"名字重复,无法保存", MessageDegree.ERROR);
                snackbarMessageQueue.EnqueueEx("保存失败");
                return;
            }
            foreach (var item in FunctionShieldCollection)
            {
                if (Regex.IsMatch(item.Name, "^\\d"))
                {
                    if (Regex.IsMatch(item.Name, "^\\d"))
                    {
                        snackbarMessageQueue.EnqueueEx("名称不合法,名称不能以数字开头");
                        return;
                    }
                }
            }
            dataBaseService.Db.Queryable<FunctionShieldEntity>().Where(x => x.ProductInfo.Select == true).OrderBy(x => x.Id).ToList().ForEach(orgitem =>
            {
                var updateItem = FunctionShieldCollection.Where(x => x.Id == orgitem.Id).FirstOrDefault();
                if (updateItem != null) FunctionShieldCompare(orgitem, updateItem);
            });

            dataBaseService.Db.Updateable(mapper.Map<List<FunctionShieldEntity>>(FunctionShieldCollection)).ExecuteCommand();
            GetFunctionShieldGroup();
            UpdateGlobalMCFunctionShieldDic();
            logService.WriteLog(LogTypes.DB.ToString(), $@"保存成功", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("保存成功");
        }

        private DelegateCommand<string> _GroupChangedCommand;
        public DelegateCommand<string> GroupChangedCommand =>
            _GroupChangedCommand ?? (_GroupChangedCommand = new DelegateCommand<string>(ExecuteGroupChangedCommand));

        void ExecuteGroupChangedCommand(string parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter)) return;
            var functionShields = dataBaseService.Db.Queryable<FunctionShieldEntity>().Includes(x => x.ProductInfo).Where(x => x.Group == parameter && x.ProductInfo.Select == true).OrderBy(x => x.Id).ToList();
            FunctionShieldCollection.Clear();
            mapper.Map(functionShields, FunctionShieldCollection);
            snackbarMessageQueue.EnqueueEx("筛选成功");
        }

        private DelegateCommand _ShowAllFunctionShieldCommand;
        public DelegateCommand ShowAllFunctionShieldCommand =>
            _ShowAllFunctionShieldCommand ?? (_ShowAllFunctionShieldCommand = new DelegateCommand(ExecuteShowAllFunctionShieldCommand));

        void ExecuteShowAllFunctionShieldCommand()
        {
            GetFunctionShields();
            NewFunctionShield = new FunctionShieldInfoModel();
            FunctionShieldGroupCollection.Clear();
            GetFunctionShieldGroup();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}显示所有参数成功", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("显示所有参数成功");
        }

        private DelegateCommand _MsgClearCommand;
        public DelegateCommand MsgClearCommand =>
            _MsgClearCommand ?? (_MsgClearCommand = new DelegateCommand(ExecuteMsgClearCommand));

        void ExecuteMsgClearCommand()
        {
            MessageList.Clear();
        }

        private DelegateCommand _OpenLogDirectoryCommand;
        public DelegateCommand OpenLogDirectoryCommand =>
            _OpenLogDirectoryCommand ?? (_OpenLogDirectoryCommand = new DelegateCommand(ExecuteOpenLogDirectoryCommand));

        void ExecuteOpenLogDirectoryCommand()
        {
            if (Directory.Exists(logService.GetLogSavePath()))
            {
                System.Diagnostics.Process.Start(logService.GetLogSavePath());
            }
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                FunctionShieldCollection.Clear();
                GetFunctionShieldGroup();
                GetFunctionShields();
            }), System.Windows.Threading.DispatcherPriority.Render);

            NewFunctionShield = new FunctionShieldInfoModel();
            logService.ShowOnUI += LogService_ShowOnUI;
            MessageList.Clear();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}进入功能屏蔽配置页面", MessageDegree.INFO);
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            //更新全局功能屏蔽集合,不使用重新初始化,防止设备运行中异常
            UpdateGlobalMCFunctionShieldDic();
            logService.ShowOnUI -= LogService_ShowOnUI;
        }

        #region PrivateMethod
        private void UpdateGlobalMCFunctionShieldDic()
        {
            var newFunctionShields = dataBaseService.Db.Queryable<FunctionShieldEntity>().Includes(x => x.ProductInfo).Where(x => x.ProductInfo.Select == true).OrderBy(x => x.Id).ToList();
            var willRemoveItems = new List<FunctionShieldInfo>();
            var willAddItems = new List<FunctionShieldInfo>();
            foreach (var item in GlobalValues.MCFunctionShieldDicCollection)
            {
                var result = newFunctionShields.Where(x => x.Id == item.Value.Id).FirstOrDefault();
                if (result == null)
                {
                    willRemoveItems.Add(item.Value);
                }
                else
                {
                    if (item.Key == result.Name)
                        mapper.Map(result, item.Value);
                    else
                        willRemoveItems.Add(item.Value);
                }
            }
            willRemoveItems.ForEach(removeItem => GlobalValues.MCFunctionShieldDicCollection.Remove(removeItem.Name));
            foreach (var item in newFunctionShields)
            {
                var result = GlobalValues.MCFunctionShieldDicCollection.Values.ToList().Where(x => x.Id == item.Id).FirstOrDefault();
                if (result == null)
                    willAddItems.Add(mapper.Map<FunctionShieldInfo>(item));
            }
            willAddItems.ForEach(addItem => GlobalValues.MCFunctionShieldDicCollection.Add(addItem.Name, addItem));
        }

        private void GetFunctionShields()
        {
            FunctionShieldCollection.Clear();
            var functionShields = dataBaseService.Db.Queryable<FunctionShieldEntity>().Includes(x => x.ProductInfo).Where(x => x.ProductInfo.Select == true).OrderBy(x => x.Id).ToList();
            mapper.Map(functionShields, FunctionShieldCollection);
        }

        private void GetFunctionShieldGroup()
        {
            var functionShieldsGroups = dataBaseService.Db.Queryable<FunctionShieldEntity>().Where(x => x.ProductInfo.Select == true).Distinct().Select(x => x.Group).ToList();
            functionShieldsGroups.ForEach(x => { if (!string.IsNullOrWhiteSpace(x) && !FunctionShieldGroupCollection.Contains(x)) FunctionShieldGroupCollection.Add(x); });
        }

        private void LogService_ShowOnUI(MessageItem obj)
        {
            MessageList.Add(obj);
            if (MessageList.Count > 1000) MessageList.RemoveAt(0);
        }

        private void FunctionShieldCompare(FunctionShieldEntity orgItem, FunctionShieldInfoModel updateItem)
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
