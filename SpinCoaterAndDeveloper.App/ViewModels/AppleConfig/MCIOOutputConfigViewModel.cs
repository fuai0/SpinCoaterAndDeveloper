using AutoMapper;
using DataBaseServiceInterface;
using LogServiceInterface;
using MaterialDesignThemes.Wpf;
using MotionControlActuation;
using Newtonsoft.Json;
using PermissionServiceInterface;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using PrismAutomationPlatformExt;
using SpinCoaterAndDeveloper.App.Common.Models;
using SpinCoaterAndDeveloper.App.Extensions;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.DataEntity;
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
    public class MCIOOutputConfigViewModel : BindableBase, INavigationAware
    {
        private readonly IDialogHostService dialogHostService;
        private readonly IDataBaseService dataBaseService;
        private readonly IMapper mapper;
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

        private IOOutputInfoModel _NewIOOutPut;
        public IOOutputInfoModel NewIOOutput
        {
            get { return _NewIOOutPut; }
            set { SetProperty(ref _NewIOOutPut, value); }
        }

        private IOOutputInfoModel _CurrentSelectIOOutput;
        public IOOutputInfoModel CurrentSelectIOOutput
        {
            get { return _CurrentSelectIOOutput; }
            set { SetProperty(ref _CurrentSelectIOOutput, value); }
        }
        #endregion

        public ObservableCollection<IOOutputInfoModel> IOOutputCfgCollections { get; set; } = new ObservableCollection<IOOutputInfoModel>();
        public ObservableCollection<string> IOOutputGroupCollection { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<MessageItem> MessageList { get; set; } = new ObservableCollection<MessageItem>();
        public MCIOOutputConfigViewModel(IContainerProvider containerProvider)
        {
            this.dialogHostService = containerProvider.Resolve<IDialogHostService>();
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
            this.mapper = containerProvider.Resolve<IMapper>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.permissionService = containerProvider.Resolve<IPermissionService>();
            this.snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();
            this.motionResourceInit = containerProvider.Resolve<IMotionResourceInit>();
        }

        private DelegateCommand _AddIOCfgCommand;
        public DelegateCommand AddIOCfgCommand =>
            _AddIOCfgCommand ?? (_AddIOCfgCommand = new DelegateCommand(ExecuteAddIOCfgCommand));

        void ExecuteAddIOCfgCommand()
        {
            IsRightDrawerOpen = true;
        }

        private DelegateCommand _AddOutputCommand;
        public DelegateCommand AddOutputCommand =>
            _AddOutputCommand ?? (_AddOutputCommand = new DelegateCommand(ExecuteAddOutputCommand));

        void ExecuteAddOutputCommand()
        {
            if (string.IsNullOrEmpty(NewIOOutput.Name))
            {
                snackbarMessageQueue.EnqueueEx("请输入名称.");
                return;
            }
            if (Regex.IsMatch(NewIOOutput.Name, "^\\d"))
            {
                snackbarMessageQueue.EnqueueEx("名称不合法,名称不能以数字开头");
                return;
            }
            if (dataBaseService.Db.Queryable<IOOutputInfoEntity>().Where(x => x.Name == NewIOOutput.Name).Any())
            {
                snackbarMessageQueue.EnqueueEx("已存在同名输出点名称.");
                return;
            }
            int totalCount = IOOutputCfgCollections.Count;
            NewIOOutput.ProgramAddressGroup = totalCount / 8;
            NewIOOutput.ProgramAddressPosition = totalCount % 8;
            dataBaseService.Db.Insertable(mapper.Map<IOOutputInfoEntity>(NewIOOutput)).ExecuteCommand();
            GetIOOutputs();
            GetIOOutputGroups();
            UpdateGlobalIOOutputDic();
            logService.WriteLog(LogTypes.DB.ToString(), $"用户{permissionService.CurrentUserName}创建输出点位{NewIOOutput.Name}成功.\r\n{JsonConvert.SerializeObject(NewIOOutput)}", MessageDegree.INFO);
            NewIOOutput = new IOOutputInfoModel();
            snackbarMessageQueue.EnqueueEx("添加成功");
        }

        private DelegateCommand _DeleteLastOutputCommand;
        public DelegateCommand DeleteLastOutputCommand =>
            _DeleteLastOutputCommand ?? (_DeleteLastOutputCommand = new DelegateCommand(ExecuteDeleteLastOutputCommand));

        async void ExecuteDeleteLastOutputCommand()
        {
            if (IOOutputCfgCollections.Count == 0)
            {
                snackbarMessageQueue.EnqueueEx("无可删除点位");
                return;
            }
            var confirmResult = await dialogHostService.HostQuestion("提示", "确认删除最后一个点位?\r\n删除成功后,请重启程序.", "取消", "确认");
            if (confirmResult.Result != Prism.Services.Dialogs.ButtonResult.OK)
                return;
            var lastItem = dataBaseService.Db.Queryable<IOOutputInfoEntity>().OrderBy(x => x.Id, SqlSugar.OrderByType.Desc).First();
            await dataBaseService.Db.Deleteable(lastItem).ExecuteCommandAsync();
            //删除点位时,取消气缸关联的IO点位
            var relateCylinder = await dataBaseService.Db.Queryable<CylinderInfoEntity>().Where(x => x.SingleValveOutputId == CurrentSelectIOOutput.Id || x.DualValveOriginOutputId == CurrentSelectIOOutput.Id || x.DualValveMovingOutputId == CurrentSelectIOOutput.Id).ToListAsync();
            relateCylinder.ForEach(x =>
            {
                if (x.SingleValveOutputId == CurrentSelectIOOutput.Id) x.SingleValveOutputId = 0;
                if (x.DualValveOriginOutputId == CurrentSelectIOOutput.Id) x.DualValveOriginOutputId = 0;
                if (x.DualValveMovingOutputId == CurrentSelectIOOutput.Id) x.DualValveMovingOutputId = 0;
            });
            await dataBaseService.Db.Updateable(relateCylinder).ExecuteCommandAsync();
            GetIOOutputs();
            NewIOOutput = new IOOutputInfoModel();
            GetIOOutputGroups();
            CurrentSelectIOOutput = null;
            UpdateGlobalIOOutputDic();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}删除输出点位{lastItem.Name}成功", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("删除成功");
        }

        private DelegateCommand _ShowAllOutputsCommand;
        public DelegateCommand ShowAllOutputsCommand =>
            _ShowAllOutputsCommand ?? (_ShowAllOutputsCommand = new DelegateCommand(ExecuteShowAllOutputsCommand));

        void ExecuteShowAllOutputsCommand()
        {
            GetIOOutputs();
            NewIOOutput = new IOOutputInfoModel();
            IOOutputGroupCollection.Clear();
            GetIOOutputGroups();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}显示所有输出点位成功", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("显示所有输出点位成功");
        }

        private DelegateCommand<string> _GroupChangedCommand;
        public DelegateCommand<string> GroupChangedCommand =>
            _GroupChangedCommand ?? (_GroupChangedCommand = new DelegateCommand<string>(ExecuteGroupChangedCommand));

        void ExecuteGroupChangedCommand(string parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter))
            {
                return;
            }
            var outputs = dataBaseService.Db.Queryable<IOOutputInfoEntity>().Where(x => x.Group == parameter).OrderBy(x => x.Number).OrderBy(x => x.Id).ToList();
            IOOutputCfgCollections.Clear();
            mapper.Map(outputs, IOOutputCfgCollections);
            snackbarMessageQueue.EnqueueEx("筛选输出点位成功");
        }

        private DelegateCommand _SaveSelectedIOOutputCommand;
        public DelegateCommand SaveSelectedIOOutputCommand =>
            _SaveSelectedIOOutputCommand ?? (_SaveSelectedIOOutputCommand = new DelegateCommand(ExecuteSaveSelectedIOOutputCommand));

        void ExecuteSaveSelectedIOOutputCommand()
        {
            if (CurrentSelectIOOutput == null)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"请选中需要保存的输出点位", MessageDegree.WARN);
                return;
            }
            if (Regex.IsMatch(CurrentSelectIOOutput.Name, "^\\d"))
            {
                snackbarMessageQueue.EnqueueEx("名称不合法,名称不能以数字开头");
                return;
            }
            if (dataBaseService.Db.Queryable<IOOutputInfoEntity>().Where(x => x.Name == CurrentSelectIOOutput.Name && x.Id != CurrentSelectIOOutput.Id).Any())
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"名字{CurrentSelectIOOutput.Name}重复!保存失败,数据已恢复.", MessageDegree.INFO);
                snackbarMessageQueue.EnqueueEx("保存失败");
                var orgin = dataBaseService.Db.Queryable<IOOutputInfoEntity>().Where(x => x.Id == CurrentSelectIOOutput.Id).First();
                mapper.Map(orgin, CurrentSelectIOOutput);
                return;
            }
            var org = dataBaseService.Db.Queryable<IOOutputInfoEntity>().Where(x => x.Id == CurrentSelectIOOutput.Id).First();
            ParCompare(org, CurrentSelectIOOutput);

            dataBaseService.Db.Updateable(mapper.Map<IOOutputInfoEntity>(CurrentSelectIOOutput)).ExecuteCommand();
            GetIOOutputGroups();
            UpdateGlobalIOOutputDic();
            logService.WriteLog(LogTypes.DB.ToString(), $"用户{permissionService.CurrentUserName}保存输入点位信息成功.", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("保存成功");
        }

        private DelegateCommand _SaveAllIOOutputsCommand;
        public DelegateCommand SaveAllIOOutputsCommand =>
            _SaveAllIOOutputsCommand ?? (_SaveAllIOOutputsCommand = new DelegateCommand(ExecuteSaveAllIOOutputsCommand));

        void ExecuteSaveAllIOOutputsCommand()
        {
            bool repeatName = IOOutputCfgCollections.GroupBy(x => x.Name).Where(x => x.Count() > 1).Count() > 0;
            if (repeatName)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"名字重复,无法保存", MessageDegree.ERROR);
                snackbarMessageQueue.EnqueueEx("保存失败");
                return;
            }
            foreach (var item in IOOutputCfgCollections)
            {
                if (Regex.IsMatch(item.Name, "^\\d"))
                {
                    snackbarMessageQueue.EnqueueEx("名称不合法,名称不能以数字开头");
                    return;
                }
            }
            dataBaseService.Db.Queryable<IOOutputInfoEntity>().ToList().ForEach(orgItem =>
            {
                var updateItem = IOOutputCfgCollections.Where(x => x.Id == orgItem.Id).FirstOrDefault();
                if (updateItem != null) ParCompare(orgItem, updateItem);
            });

            dataBaseService.Db.Updateable(mapper.Map<List<IOOutputInfoEntity>>(IOOutputCfgCollections)).ExecuteCommand();
            GetIOOutputGroups();
            UpdateGlobalIOOutputDic();
            logService.WriteLog(LogTypes.DB.ToString(), $@"保存成功", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("保存成功");
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
                IOOutputGroupCollection.Clear();
                GetIOOutputGroups();
                GetIOOutputs();
            }), System.Windows.Threading.DispatcherPriority.Render);

            NewIOOutput = new IOOutputInfoModel();

            logService.ShowOnUI += LogService_ShowOnUI;
            MessageList.Clear();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}进入输出点位配置页面", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("设备运转中,请勿修改参数");
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            //离开页面刷新全局输出点位信息集合
            //UpdateGlobalIOOutputDic();
            logService.ShowOnUI -= LogService_ShowOnUI;
        }
        #region PrivateMethod
        private void UpdateGlobalIOOutputDic()
        {
            lock (MotionControlResource.IOOutputResourceDicUpdateLock)
            {
                motionResourceInit.InitIOOutputResourceDicCollection();
            }
        }

        private void GetIOOutputs()
        {
            IOOutputCfgCollections.Clear();
            var outputs = dataBaseService.Db.Queryable<IOOutputInfoEntity>().OrderBy(x => x.Number).OrderBy(x => x.Id).ToList();
            mapper.Map(outputs, IOOutputCfgCollections);
        }

        private void GetIOOutputGroups()
        {
            var outputGroups = dataBaseService.Db.Queryable<IOOutputInfoEntity>().Distinct().Select(x => x.Group).ToList();
            outputGroups.ForEach(x => { if (!string.IsNullOrWhiteSpace(x) && !IOOutputGroupCollection.Contains(x)) IOOutputGroupCollection.Add(x); });
        }

        private void LogService_ShowOnUI(MessageItem obj)
        {
            MessageList.Add(obj);
            if (MessageList.Count > 1000) MessageList.RemoveAt(0);
        }

        private void ParCompare(IOOutputInfoEntity orgItem, IOOutputInfoModel updateItem)
        {
            if (orgItem.Name != updateItem.Name) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Name:{orgItem.Name}=>{updateItem.Name}", MessageDegree.INFO);
            if (orgItem.Number != updateItem.Number) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Number:{orgItem.Number}=>{updateItem.Number}", MessageDegree.INFO);
            if (orgItem.PhysicalLocation != updateItem.PhysicalLocation) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} PhysicalLocation:{orgItem.PhysicalLocation}=>{updateItem.PhysicalLocation}", MessageDegree.INFO);
            if (orgItem.CNName != updateItem.CNName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} CNName:{orgItem.CNName}=>{updateItem.CNName}", MessageDegree.INFO);
            if (orgItem.ENName != updateItem.ENName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} ENName:{orgItem.ENName}=>{updateItem.ENName}", MessageDegree.INFO);
            if (orgItem.VNName != updateItem.VNName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} VNName:{orgItem.VNName}=>{updateItem.VNName}", MessageDegree.INFO);
            if (orgItem.ReverseEnable != updateItem.ReverseEnable) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} ReverseEnable:{orgItem.ReverseEnable}=>{updateItem.ReverseEnable}", MessageDegree.INFO);
            if (orgItem.ShieldEnable != updateItem.ShieldEnable) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} ShieldEnable:{orgItem.ShieldEnable}=>{updateItem.ShieldEnable}", MessageDegree.INFO);
            if (orgItem.ShiedlEnableDefaultValue != updateItem.ShiedlEnableDefaultValue) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} ShiedlEnableDefaultValue:{orgItem.ShiedlEnableDefaultValue}=>{updateItem.ShiedlEnableDefaultValue}", MessageDegree.INFO);
            if (orgItem.Backup != updateItem.Backup) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Backup:{orgItem.Backup}=>{updateItem.Backup}", MessageDegree.INFO);
            if (orgItem.Group != updateItem.Group) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Group:{orgItem.Group}=>{updateItem.Group}", MessageDegree.INFO);
            if (orgItem.Tag != updateItem.Tag) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Tag:{orgItem.Tag}=>{updateItem.Tag}", MessageDegree.INFO);
        }
        #endregion
    }
}
