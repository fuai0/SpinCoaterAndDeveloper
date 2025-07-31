using AutoMapper;
using DataBaseServiceInterface;
using ImTools;
using LiveChartsCore.SkiaSharpView;
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
using Prism.Services.Dialogs;
using PrismAutomationPlatformExt;
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
    public class MCAxisConfigViewModel : BindableBase, INavigationAware
    {
        private readonly IDataBaseService dataBaseService;
        private readonly IMapper mapper;
        private readonly IEventAggregator eventAggregator;
        private readonly ISnackbarMessageQueue snackbarMessageQueue;
        private readonly ILogService logService;
        private readonly IPermissionService permissionService;
        private readonly IDialogHostService dialogHostService;
        private readonly IMotionResourceInit motionResourceInit;

        #region Binding
        private bool isRightDrawerOpen;
        public bool IsRightDrawerOpen
        {
            get { return isRightDrawerOpen; }
            set { SetProperty(ref isRightDrawerOpen, value); }
        }

        private AxisInfoModel _NewAxis;
        public AxisInfoModel NewAxis
        {
            get { return _NewAxis; }
            set { SetProperty(ref _NewAxis, value); }
        }

        private AxisInfoModel _CurrentSelectAxis;
        public AxisInfoModel CurrentSelectAxis
        {
            get { return _CurrentSelectAxis; }
            set { SetProperty(ref _CurrentSelectAxis, value); }
        }

        #endregion

        public ObservableCollection<AxisInfoModel> AxisCfgCollection { get; set; } = new ObservableCollection<AxisInfoModel>();
        public ObservableCollection<string> AxisGroupCollection { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<MessageItem> MessageList { get; set; } = new ObservableCollection<MessageItem>();

        public MCAxisConfigViewModel(IContainerProvider containerProvider)
        {
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
            this.mapper = containerProvider.Resolve<IMapper>();
            this.eventAggregator = containerProvider.Resolve<IEventAggregator>();
            this.snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.permissionService = containerProvider.Resolve<IPermissionService>();
            this.dialogHostService = containerProvider.Resolve<IDialogHostService>();
            this.motionResourceInit = containerProvider.Resolve<IMotionResourceInit>();
        }

        private DelegateCommand addAxisCfgCommand;
        public DelegateCommand AddAxisCfgCommand =>
            addAxisCfgCommand ?? (addAxisCfgCommand = new DelegateCommand(ExecuteAddAxisCfgCommand));

        void ExecuteAddAxisCfgCommand()
        {
            IsRightDrawerOpen = true;
        }
        private DelegateCommand addAxisCommand;
        public DelegateCommand AddAxisCommand =>
            addAxisCommand ?? (addAxisCommand = new DelegateCommand(ExecuteAddAxisCommand));

        async void ExecuteAddAxisCommand()
        {
            if (string.IsNullOrWhiteSpace(NewAxis.Name))
            {
                snackbarMessageQueue.EnqueueEx("请输入轴名称");
                return;
            }
            if (Regex.IsMatch(NewAxis.Name, "^\\d"))
            {
                snackbarMessageQueue.EnqueueEx("名称不合法,名称不能以数字开头");
                return;
            }
            if (dataBaseService.Db.Queryable<AxisInfoEntity>().Where(x => x.Name == NewAxis.Name).Any())
            {
                snackbarMessageQueue.EnqueueEx("轴名称重复");
                return;
            }
            if (dataBaseService.Db.Queryable<AxisInfoEntity>().Where(x => x.AxisIdOnCard == NewAxis.AxisIdOnCard).Any())
            {
                snackbarMessageQueue.EnqueueEx("轴卡映射ID重复");
                return;
            }
            if (dataBaseService.Db.Queryable<AxisInfoEntity>().Where(x => x.Number == NewAxis.Number).Any())
            {
                snackbarMessageQueue.EnqueueEx("轴编号重复");
                return;
            }
            if (NewAxis.HomeMethod == 0 || NewAxis.HomeMethod == 15 || NewAxis.HomeMethod == 16 || NewAxis.HomeMethod == 30 || NewAxis.HomeMethod == 31 || NewAxis.HomeMethod == 32)
            {
                snackbarMessageQueue.EnqueueEx("请选择回原方式");
                return;
            }
            if (NewAxis.HomeHighVel == 0)
            {
                snackbarMessageQueue.EnqueueEx("请输入回原高速度");
                return;
            }
            if (NewAxis.HomeLowVel == 0)
            {
                snackbarMessageQueue.EnqueueEx("请输入回原低速度");
                return;
            }
            if (NewAxis.HomeAcc == 0)
            {
                snackbarMessageQueue.EnqueueEx("请输入回原加速度");
                return;
            }
            if (NewAxis.HomeTimeout == 0)
            {
                snackbarMessageQueue.EnqueueEx("请输入回原超时时间");
                return;
            }
            if (NewAxis.Proportion == 0)
            {
                snackbarMessageQueue.EnqueueEx("请输入轴当量");
                return;
            }
            NewAxis.JogVel = 2;
            NewAxis.CNName = NewAxis.Name;
            var newAxisId = await dataBaseService.Db.Insertable(mapper.Map<AxisInfoEntity>(NewAxis)).ExecuteReturnIdentityAsync();
            var movementPoints = await dataBaseService.Db.Queryable<MovementPointInfoEntity>().ToListAsync();
            List<MovementPointPositionEntity> needAddMovementPointPositionEntity = new List<MovementPointPositionEntity>();
            movementPoints.ForEach(x =>
            {
                needAddMovementPointPositionEntity.Add(new MovementPointPositionEntity() { MovementPointNameId = x.Id, AxisInfoId = newAxisId, InvolveAxis = false });
            });
            //运动关联点位中增加新增轴
            if (needAddMovementPointPositionEntity.Count != 0)
                await dataBaseService.Db.Insertable(needAddMovementPointPositionEntity).ExecuteCommandAsync();
            GetAxesInfo();
            GetAxisGroups();
            UpdateGlobalAxisDic();
            NewAxis = new AxisInfoModel();
            logService.WriteLog(LogTypes.DB.ToString(), $"用户{permissionService.CurrentUserName}创建轴{NewAxis.Name}成功.\r\n{JsonConvert.SerializeObject(NewAxis)}", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("添加轴成功");
        }

        private DelegateCommand _ShowAllAxesCommand;
        public DelegateCommand ShowAllAxesCommand =>
            _ShowAllAxesCommand ?? (_ShowAllAxesCommand = new DelegateCommand(ExecuteShowAllAxesCommand));

        void ExecuteShowAllAxesCommand()
        {
            GetAxesInfo();
            NewAxis = new AxisInfoModel();
            AxisGroupCollection.Clear();
            GetAxisGroups();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}显示所有轴成功", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("显示所有轴成功");
        }

        private DelegateCommand _DeleteAxisCommand;
        public DelegateCommand DeleteAxisCommand =>
            _DeleteAxisCommand ?? (_DeleteAxisCommand = new DelegateCommand(ExecuteDeleteAxisCommand));

        async void ExecuteDeleteAxisCommand()
        {
            if (CurrentSelectAxis == null)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"请选中需要删除的轴", MessageDegree.WARN);
                return;
            }
            var result = await dialogHostService.ShowHostDialog("DialogHostMessageView", new DialogParameters() { { "Title", "警告" }, { "Content", $"确认删除轴{CurrentSelectAxis.Name}?\r\n请注意与轴关联运动点位将会失去关联,需要重新创建点位!\r\n删除成功后,请重启程序." }, { "CancelInfo", "取消" }, { "SaveInfo", "确定" } });
            if (result.Result != ButtonResult.OK)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}取消删除轴", MessageDegree.INFO);
                return;
            }
            dataBaseService.Db.Deleteable(mapper.Map<AxisInfoEntity>(CurrentSelectAxis)).ExecuteCommand();
            //删除运动点位中的轴
            await dataBaseService.Db.Deleteable<MovementPointPositionEntity>().Where(x => x.AxisInfoId == CurrentSelectAxis.Id).ExecuteCommandAsync();
            UpdateGlobalAxisDic();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}删除轴{CurrentSelectAxis.Name}成功", MessageDegree.INFO);
            GetAxesInfo();
            NewAxis = new AxisInfoModel();
            GetAxisGroups();
            CurrentSelectAxis = null;
            snackbarMessageQueue.EnqueueEx("删除轴成功");
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
            var axes = dataBaseService.Db.Queryable<AxisInfoEntity>().Where(x => x.Group == parameter).OrderBy(x => x.Number).OrderBy(x => x.Id).ToList();
            AxisCfgCollection.Clear();
            mapper.Map(axes, AxisCfgCollection);
            snackbarMessageQueue.EnqueueEx("筛选轴成功");
        }

        private DelegateCommand _SaveSelectedAxisCommand;
        public DelegateCommand SaveSelectedAxisCommand =>
            _SaveSelectedAxisCommand ?? (_SaveSelectedAxisCommand = new DelegateCommand(ExecuteSaveSelectedAxisCommand));

        void ExecuteSaveSelectedAxisCommand()
        {
            if (CurrentSelectAxis == null)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"请选中需要保存的轴", MessageDegree.WARN);
                return;
            }
            if (Regex.IsMatch(CurrentSelectAxis.Name, "^\\d"))
            {
                snackbarMessageQueue.EnqueueEx("名称不合法,名称不能以数字开头");
                return;
            }
            if (dataBaseService.Db.Queryable<AxisInfoEntity>().Where(x => x.Name == CurrentSelectAxis.Name && x.Id != CurrentSelectAxis.Id).Any())
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"名字{CurrentSelectAxis.Name}重复!保存失败,数据已恢复.", MessageDegree.INFO);
                snackbarMessageQueue.EnqueueEx("保存失败");
                var orgin = dataBaseService.Db.Queryable<AxisInfoEntity>().Where(x => x.Id == CurrentSelectAxis.Id).First();
                mapper.Map(orgin, CurrentSelectAxis);
                return;
            }
            if (dataBaseService.Db.Queryable<AxisInfoEntity>().Where(x => x.AxisIdOnCard == CurrentSelectAxis.AxisIdOnCard && x.Id != CurrentSelectAxis.Id).Any())
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"映射轴卡ID{CurrentSelectAxis.AxisIdOnCard}重复!保存失败,数据已恢复.", MessageDegree.INFO);
                snackbarMessageQueue.EnqueueEx("保存失败");
                var orgin = dataBaseService.Db.Queryable<AxisInfoEntity>().Where(x => x.Id == CurrentSelectAxis.Id).First();
                mapper.Map(orgin, CurrentSelectAxis);
                return;
            }
            var org = dataBaseService.Db.Queryable<AxisInfoEntity>().Where(x => x.Id == CurrentSelectAxis.Id).First();
            ParCompare(org, CurrentSelectAxis);

            dataBaseService.Db.Updateable(mapper.Map<AxisInfoEntity>(CurrentSelectAxis)).ExecuteCommand();
            UpdateGlobalAxisDic();
            logService.WriteLog(LogTypes.DB.ToString(), $"用户{permissionService.CurrentUserName}保存轴信息成功.", MessageDegree.INFO);
            GetAxisGroups();
            snackbarMessageQueue.EnqueueEx("保存成功");

        }

        private DelegateCommand _SaveAllAxesCommand;
        public DelegateCommand SaveAllAxesCommand =>
            _SaveAllAxesCommand ?? (_SaveAllAxesCommand = new DelegateCommand(ExecuteSaveAllAxesCommand));

        void ExecuteSaveAllAxesCommand()
        {
            bool repeatName = AxisCfgCollection.GroupBy(x => x.Name).Where(x => x.Count() > 1).Count() > 0;
            if (repeatName)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"名字重复,无法保存", MessageDegree.ERROR);
                snackbarMessageQueue.EnqueueEx("保存失败");
                return;
            }

            foreach (var item in AxisCfgCollection)
            {
                if (Regex.IsMatch(item.Name, "^\\d"))
                {
                    snackbarMessageQueue.EnqueueEx("名称不合法,名称不能以数字开头");
                    return;
                }
            }

            bool repeatIdOnCard = AxisCfgCollection.GroupBy(x => x.AxisIdOnCard).Where(x => x.Count() > 1).Count() > 0;
            if (repeatIdOnCard)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"轴卡映射ID重复,无法保存", MessageDegree.ERROR);
                snackbarMessageQueue.EnqueueEx("保存失败");
                return;
            }

            dataBaseService.Db.Queryable<AxisInfoEntity>().ToList().ForEach(orgItem =>
            {
                var updateItem = AxisCfgCollection.Where(x => x.Id == orgItem.Id).FirstOrDefault();
                if (updateItem != null) ParCompare(orgItem, updateItem);
            });

            dataBaseService.Db.Updateable(mapper.Map<List<AxisInfoEntity>>(AxisCfgCollection)).ExecuteCommand();
            UpdateGlobalAxisDic();
            GetAxisGroups();
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
            MessageList.Clear();
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                AxisGroupCollection.Clear();
                GetAxisGroups();
                GetAxesInfo();
            }), System.Windows.Threading.DispatcherPriority.Render);

            NewAxis = new AxisInfoModel();
            logService.ShowOnUI += LogService_ShowOnUI;
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}进入轴配置页面", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("设备运转中请勿修改参数");
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            //离开页面刷新全局轴信息集合
            //UpdateGlobalAxisDic();
            logService.ShowOnUI -= LogService_ShowOnUI;
        }

        #region PrivateMethod
        private void UpdateGlobalAxisDic()
        {
            lock (MotionControlResource.AxisResourceDicUpdateLock)
            {
                motionResourceInit.InitAxisResourceDicCollection();
            }
        }

        private void GetAxesInfo()
        {
            AxisCfgCollection.Clear();
            var axisCfgs = dataBaseService.Db.Queryable<AxisInfoEntity>().OrderBy(x => x.Number).OrderBy(x => x.Id).ToList();
            mapper.Map(axisCfgs, AxisCfgCollection);
        }

        private void GetAxisGroups()
        {
            var axisGroups = dataBaseService.Db.Queryable<AxisInfoEntity>().Distinct().Select(x => x.Group).ToList();
            axisGroups.ForEach(x => { if (!string.IsNullOrWhiteSpace(x) && !AxisGroupCollection.Contains(x)) AxisGroupCollection.Add(x); });
        }

        private void LogService_ShowOnUI(MessageItem obj)
        {
            MessageList.Add(obj);
            if (MessageList.Count > 1000) MessageList.RemoveAt(0);
        }

        private void ParCompare(AxisInfoEntity orgItem, AxisInfoModel updateItem)
        {
            if (orgItem.Name != updateItem.Name) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Name: {orgItem.Name}=>{updateItem.Name}", MessageDegree.INFO);
            if (orgItem.CNName != updateItem.CNName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} CNName: {orgItem.CNName}=>{updateItem.CNName}", MessageDegree.INFO);
            if (orgItem.ENName != updateItem.ENName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} ENName: {orgItem.ENName}=>{updateItem.ENName}", MessageDegree.INFO);
            if (orgItem.VNName != updateItem.VNName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} VNName: {orgItem.VNName}=>{updateItem.VNName}", MessageDegree.INFO);
            if (orgItem.AxisIdOnCard != updateItem.AxisIdOnCard) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} AxisIdOnCard: {orgItem.AxisIdOnCard}=>{updateItem.AxisIdOnCard}", MessageDegree.INFO);
            if (orgItem.HomeMethod != updateItem.HomeMethod) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} HomeMethod: {orgItem.HomeMethod}=>{updateItem.HomeMethod}", MessageDegree.INFO);
            if (orgItem.HomeHighVel != updateItem.HomeHighVel) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} HomeHighVel :{orgItem.HomeHighVel}=>{updateItem.HomeHighVel}", MessageDegree.INFO);
            if (orgItem.HomeLowVel != updateItem.HomeLowVel) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} HomeLowVel: {orgItem.HomeLowVel}=>{updateItem.HomeLowVel}", MessageDegree.INFO);
            if (orgItem.HomeAcc != updateItem.HomeAcc) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} HomeAcc: {orgItem.HomeAcc}=>{updateItem.HomeAcc}", MessageDegree.INFO);
            if (orgItem.HomeTimeout != updateItem.HomeTimeout) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} HomeTimeout: {orgItem.HomeTimeout}=>{updateItem.HomeTimeout}", MessageDegree.INFO);
            if (orgItem.Number != updateItem.Number) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Number: {orgItem.Number}=>{updateItem.Number}", MessageDegree.INFO);
            if (orgItem.Proportion != updateItem.Proportion) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Proportion: {orgItem.Proportion}=>{updateItem.Proportion}", MessageDegree.INFO);
            if (orgItem.HomeOffset != updateItem.HomeOffset) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} HomeOffset: {orgItem.HomeOffset}=>{updateItem.HomeOffset}", MessageDegree.INFO);
            if (orgItem.Type != updateItem.Type) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Type: {orgItem.Type}=>{updateItem.Type}", MessageDegree.INFO);
            if (orgItem.SoftLimitEnable != updateItem.SoftLimitEnable) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} SoftLimitEnable: {orgItem.SoftLimitEnable}=>{updateItem.SoftLimitEnable}", MessageDegree.INFO);
            if (orgItem.SoftPositiveLimitPos != updateItem.SoftPositiveLimitPos) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} SoftPositiveLimitPos: {orgItem.SoftPositiveLimitPos}=>{updateItem.SoftPositiveLimitPos}", MessageDegree.INFO);
            if (orgItem.SoftNegativeLimitPos != updateItem.SoftNegativeLimitPos) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} SoftNegativeLimitPos: {orgItem.SoftNegativeLimitPos}=>{updateItem.SoftNegativeLimitPos}", MessageDegree.INFO);
            if (orgItem.Group != updateItem.Group) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Group: {orgItem.Group}=>{updateItem.Group}", MessageDegree.INFO);
            if (orgItem.SafeAxisEnable != updateItem.SafeAxisEnable) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} SafeAxisEnable: {orgItem.SafeAxisEnable}=>{updateItem.SafeAxisEnable}", MessageDegree.INFO);
            if (orgItem.SafeAxisPosition != updateItem.SafeAxisPosition) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} SafeAxisPosition: {orgItem.SafeAxisPosition}=>{updateItem.SafeAxisPosition}", MessageDegree.INFO);
            if (orgItem.TargetLocationGap != updateItem.TargetLocationGap) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} TargetLocationGap: {orgItem.TargetLocationGap}=>{updateItem.TargetLocationGap}", MessageDegree.INFO);
        }
        #endregion
    }
}
