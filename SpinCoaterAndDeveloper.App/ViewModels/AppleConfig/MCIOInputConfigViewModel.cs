using AutoMapper;
using DataBaseServiceInterface;
using LogServiceInterface;
using MaterialDesignThemes.Wpf;
using MotionControlActuation;
using Newtonsoft.Json;
using PermissionServiceInterface;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using PrismAutomationPlatformExt;
using SpinCoaterAndDeveloper.App.Common;
using SpinCoaterAndDeveloper.App.Common.Models;
using SpinCoaterAndDeveloper.App.Extensions;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using SpinCoaterAndDeveloper.Shared.Extensions;
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
    public class MCIOInputConfigViewModel : BindableBase, INavigationAware
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

        private IOInputInfoModel _NewIOInput;
        public IOInputInfoModel NewIOInput
        {
            get { return _NewIOInput; }
            set { SetProperty(ref _NewIOInput, value); }
        }

        private IOInputInfoModel _CurrentSelectIOInput;
        public IOInputInfoModel CurrentSelectIOInput
        {
            get { return _CurrentSelectIOInput; }
            set { SetProperty(ref _CurrentSelectIOInput, value); }
        }
        #endregion

        public ObservableCollection<IOInputInfoModel> IOInputCfgCollections { get; set; } = new ObservableCollection<IOInputInfoModel>();
        public ObservableCollection<string> IOInputGroupCollection { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<MessageItem> MessageList { get; set; } = new ObservableCollection<MessageItem>();

        public MCIOInputConfigViewModel(IContainerProvider containerProvider)
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

        private DelegateCommand _AddInputCommand;
        public DelegateCommand AddInputCommand =>
            _AddInputCommand ?? (_AddInputCommand = new DelegateCommand(ExecuteAddInputCommand));

        void ExecuteAddInputCommand()
        {
            if (string.IsNullOrEmpty(NewIOInput.Name))
            {
                snackbarMessageQueue.EnqueueEx("请输入名称.");
                return;
            }
            if (Regex.IsMatch(NewIOInput.Name, "^\\d"))
            {
                snackbarMessageQueue.EnqueueEx("名称不合法,名称不能以数字开头");
                return;
            }
            if (dataBaseService.Db.Queryable<IOInputInfoEntity>().Where(x => x.Name == NewIOInput.Name).Any())
            {
                snackbarMessageQueue.EnqueueEx("已存在同名输入点名称.");
                return;
            }
            int totalCount = IOInputCfgCollections.Count;
            NewIOInput.ProgramAddressGroup = totalCount / 8;
            NewIOInput.ProgramAddressPosition = totalCount % 8;
            dataBaseService.Db.Insertable(mapper.Map<IOInputInfoEntity>(NewIOInput)).ExecuteCommand();
            GetIOInputs();
            GetIOInputGroups();
            NewIOInput = new IOInputInfoModel();
            UpdateGlobalIOInputDic();
            logService.WriteLog(LogTypes.DB.ToString(), $"用户{permissionService.CurrentUserName}创建输入点位{NewIOInput.Name}成功.\r\n{JsonConvert.SerializeObject(NewIOInput)}", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("添加成功");
        }
        private DelegateCommand _DeleteLastInputCommand;
        public DelegateCommand DeleteLastInputCommand =>
            _DeleteLastInputCommand ?? (_DeleteLastInputCommand = new DelegateCommand(ExecuteDeleteLastInputCommand));

        async void ExecuteDeleteLastInputCommand()
        {
            if (IOInputCfgCollections.Count == 0)
            {
                snackbarMessageQueue.EnqueueEx("无可删除点位");
                return;
            }
            var confirmResult = await dialogHostService.HostQuestion("提示", "确认删除最后一个点位?\r\n删除成功后,请重启程序.", "取消", "确认");
            if (confirmResult.Result != Prism.Services.Dialogs.ButtonResult.OK)
                return;
            var lastItem = dataBaseService.Db.Queryable<IOInputInfoEntity>().OrderBy(x => x.Id, SqlSugar.OrderByType.Desc).First();
            await dataBaseService.Db.Deleteable(lastItem).ExecuteCommandAsync();
            //删除点位时,取消Jog关联的IO点位
            var relateMovementPointPosition = await dataBaseService.Db.Queryable<MovementPointPositionEntity>().Where(x => x.JogIOInputId == CurrentSelectIOInput.Id).ToListAsync();
            relateMovementPointPosition.ForEach(x => x.JogIOInputId = 0);
            await dataBaseService.Db.Updateable(relateMovementPointPosition).ExecuteCommandAsync();
            //删除点位时,取消气缸关联的IO点位
            var relateCylinder = await dataBaseService.Db.Queryable<CylinderInfoEntity>().Where(x => x.SensorOriginInputId == CurrentSelectIOInput.Id || x.SensorMovingInputId == CurrentSelectIOInput.Id).ToListAsync();
            relateCylinder.ForEach(x =>
            {
                if (x.SensorOriginInputId == CurrentSelectIOInput.Id) x.SensorOriginInputId = 0;
                if (x.SensorMovingInputId == CurrentSelectIOInput.Id) x.SensorMovingInputId = 0;
            });
            await dataBaseService.Db.Updateable(relateCylinder).ExecuteCommandAsync();
            GetIOInputs();
            NewIOInput = new IOInputInfoModel();
            GetIOInputGroups();
            CurrentSelectIOInput = null;
            UpdateGlobalIOInputDic();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}删除输入点位{lastItem.Name}成功", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("删除成功");
        }

        private DelegateCommand _ShowAllInputsCommand;
        public DelegateCommand ShowAllInputsCommand =>
            _ShowAllInputsCommand ?? (_ShowAllInputsCommand = new DelegateCommand(ExecuteShowAllInputsCommand));

        void ExecuteShowAllInputsCommand()
        {
            GetIOInputs();
            NewIOInput = new IOInputInfoModel();
            IOInputGroupCollection.Clear();
            GetIOInputGroups();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}显示所有输入点位成功", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("显示所有输入点位成功");
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
            var inputs = dataBaseService.Db.Queryable<IOInputInfoEntity>().Where(x => x.Group == parameter).OrderBy(x => x.Number).OrderBy(x => x.Id).ToList();
            IOInputCfgCollections.Clear();
            mapper.Map(inputs, IOInputCfgCollections);
            snackbarMessageQueue.EnqueueEx("筛选输入点位成功");
        }

        private DelegateCommand _SaveSelectedIOInputCommand;
        public DelegateCommand SaveSelectedIOInputCommand =>
            _SaveSelectedIOInputCommand ?? (_SaveSelectedIOInputCommand = new DelegateCommand(ExecuteSaveSelectedIOInputCommand));

        void ExecuteSaveSelectedIOInputCommand()
        {
            if (CurrentSelectIOInput == null)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"请选中需要保存的输入点位", MessageDegree.WARN);
                return;
            }
            if (Regex.IsMatch(CurrentSelectIOInput.Name, "^\\d"))
            {
                snackbarMessageQueue.EnqueueEx("名称不合法,名称不能以数字开头");
                return;
            }
            if (dataBaseService.Db.Queryable<IOInputInfoEntity>().Where(x => x.Name == CurrentSelectIOInput.Name && x.Id != CurrentSelectIOInput.Id).Any())
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"名字{CurrentSelectIOInput.Name}重复!保存失败,数据已恢复.", MessageDegree.INFO);
                snackbarMessageQueue.EnqueueEx("保存失败");
                var orgin = dataBaseService.Db.Queryable<IOInputInfoEntity>().Where(x => x.Id == CurrentSelectIOInput.Id).First();
                mapper.Map(orgin, CurrentSelectIOInput);
                return;
            }

            var org = dataBaseService.Db.Queryable<IOInputInfoEntity>().Where(x => x.Id == CurrentSelectIOInput.Id).First();
            ParCompare(org, CurrentSelectIOInput);

            dataBaseService.Db.Updateable(mapper.Map<IOInputInfoEntity>(CurrentSelectIOInput)).ExecuteCommand();
            GetIOInputGroups();
            UpdateGlobalIOInputDic();
            logService.WriteLog(LogTypes.DB.ToString(), $"用户{permissionService.CurrentUserName}保存输入点位信息成功.", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("保存成功");
        }

        private DelegateCommand _SaveAllIOInputsCommand;
        public DelegateCommand SaveAllIOInputsCommand =>
            _SaveAllIOInputsCommand ?? (_SaveAllIOInputsCommand = new DelegateCommand(ExecuteSaveAllIOInputsCommand));

        void ExecuteSaveAllIOInputsCommand()
        {
            bool repeatNmae = IOInputCfgCollections.GroupBy(x => x.Name).Where(x => x.Count() > 1).Count() > 0;
            if (repeatNmae)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"名字重复,无法保存", MessageDegree.ERROR);
                snackbarMessageQueue.EnqueueEx("保存失败");
                return;
            }
            foreach (var item in IOInputCfgCollections)
            {
                if (Regex.IsMatch(item.Name, "^\\d"))
                {
                    snackbarMessageQueue.EnqueueEx("名称不合法,名称不能以数字开头");
                    return;
                }
            }

            dataBaseService.Db.Queryable<IOInputInfoEntity>().ToList().ForEach(orgItem =>
            {
                var updateItem = IOInputCfgCollections.Where(x => x.Id == orgItem.Id).FirstOrDefault();
                if (updateItem != null) ParCompare(orgItem, updateItem);
            });

            dataBaseService.Db.Updateable(mapper.Map<List<IOInputInfoEntity>>(IOInputCfgCollections)).ExecuteCommand();
            GetIOInputGroups();
            UpdateGlobalIOInputDic();
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
                IOInputGroupCollection.Clear();
                GetIOInputGroups();
                //从数据库获取输入点集合
                GetIOInputs();
            }), System.Windows.Threading.DispatcherPriority.Render);

            NewIOInput = new IOInputInfoModel();

            logService.ShowOnUI += LogService_ShowOnUI;
            MessageList.Clear();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}进入输入点位配置页面", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("设备运转中,请勿修改参数");
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            //离开页面刷新全局输入点位信息集合
            //UpdateGlobalIOInputDic();
            logService.ShowOnUI -= LogService_ShowOnUI;
        }
        #region PrivateMethod
        private void UpdateGlobalIOInputDic()
        {
            lock (MotionControlResource.IOInputResourceDicUpdateLock)
            {
                motionResourceInit.InitIOInputResourceDicCollection();
            }
        }

        private void GetIOInputs()
        {
            IOInputCfgCollections.Clear();
            var Inputs = dataBaseService.Db.Queryable<IOInputInfoEntity>().OrderBy(x => x.Number).OrderBy(x => x.Id).ToList();
            mapper.Map(Inputs, IOInputCfgCollections);
        }

        private void GetIOInputGroups()
        {
            var inputGroups = dataBaseService.Db.Queryable<IOInputInfoEntity>().Distinct().Select(x => x.Group).ToList();
            inputGroups.ForEach(x => { if (!string.IsNullOrWhiteSpace(x) && !IOInputGroupCollection.Contains(x)) IOInputGroupCollection.Add(x); });
        }

        private void LogService_ShowOnUI(MessageItem obj)
        {
            MessageList.Add(obj);
            if (MessageList.Count > 1000) MessageList.RemoveAt(0);
        }

        private void ParCompare(IOInputInfoEntity orgItem, IOInputInfoModel updateItem)
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
