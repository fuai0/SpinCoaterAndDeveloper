using AutoMapper;
using DataBaseServiceInterface;
using LiveChartsCore.SkiaSharpView;
using LogServiceInterface;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;
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
using SpinCoaterAndDeveloper.Shared.Services.MotionResourceInitService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class MCCylinderConfigViewModel : BindableBase, INavigationAware
    {
        private readonly IMapper mapper;
        private readonly ILogService logService;
        private readonly IDataBaseService dataBaseService;
        private readonly IMotionResourceInit motionResourceInit;
        private readonly IPermissionService permissionService;
        private readonly IDialogHostService dialogHostService;
        private readonly ISnackbarMessageQueue snackbarMessageQueue;

        #region Binding
        private bool _IsRightDrawerOpen;
        public bool IsRightDrawerOpen
        {
            get { return _IsRightDrawerOpen; }
            set { SetProperty(ref _IsRightDrawerOpen, value); }
        }
        private CylinderInfoModel _NewCylinder;
        public CylinderInfoModel NewCylinder
        {
            get { return _NewCylinder; }
            set { SetProperty(ref _NewCylinder, value); }
        }
        private CylinderInfoModel _CurrentSelectCylinder;
        public CylinderInfoModel CurrentSelectCylinder
        {
            get { return _CurrentSelectCylinder; }
            set { SetProperty(ref _CurrentSelectCylinder, value); }
        }

        #endregion

        public ObservableCollection<CylinderInfoModel> CylinderCollection { get; set; } = new ObservableCollection<CylinderInfoModel>();
        public ObservableCollection<string> CylinderGroupCollection { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<MessageItem> MessageList { get; set; } = new ObservableCollection<MessageItem>();
        public ObservableCollection<IOOutputInfoModel> IOOutputCollection { get; set; } = new ObservableCollection<IOOutputInfoModel>();
        public ObservableCollection<IOInputInfoModel> IOInputCollection { get; set; } = new ObservableCollection<IOInputInfoModel>();
        public MCCylinderConfigViewModel(IContainerProvider containerProvider)
        {
            this.mapper = containerProvider.Resolve<IMapper>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.permissionService = containerProvider.Resolve<IPermissionService>();
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
            this.dialogHostService = containerProvider.Resolve<IDialogHostService>();
            this.snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();
            this.motionResourceInit = containerProvider.Resolve<IMotionResourceInit>();
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

        private DelegateCommand _DeleteCylinderCommand;
        public DelegateCommand DeleteCylinderCommand =>
            _DeleteCylinderCommand ?? (_DeleteCylinderCommand = new DelegateCommand(ExecuteDeleteCylinderCommand));

        async void ExecuteDeleteCylinderCommand()
        {
            if (CurrentSelectCylinder == null)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"请选中要删除的轴", MessageDegree.WARN);
                return;
            }
            var result = await dialogHostService.ShowHostDialog("DialogHostMessageView", new DialogParameters() { { "Title", "警告" }, { "Content", $"确认删除气缸{CurrentSelectCylinder.Name}?" }, { "CancelInfo", "取消" }, { "SaveInfo", "确定" } });
            if (result.Result != ButtonResult.OK)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}取消删除气缸", MessageDegree.INFO);
                return;
            }
            dataBaseService.Db.Deleteable(mapper.Map<CylinderInfoEntity>(CurrentSelectCylinder)).ExecuteCommand();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}删除气缸{CurrentSelectCylinder.Name}成功", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("删除气缸成功");
            GetCylinderInfo();
            NewCylinder = new CylinderInfoModel();
            GetCylinderGroups();
            UpdateCylinderDic();
            CurrentSelectCylinder = null;
        }

        private DelegateCommand _AddCylinderCommand;
        public DelegateCommand AddCylinderCommand =>
            _AddCylinderCommand ?? (_AddCylinderCommand = new DelegateCommand(ExecuteAddCylinderCommand));

        void ExecuteAddCylinderCommand()
        {
            if (string.IsNullOrWhiteSpace(NewCylinder.Name))
            {
                snackbarMessageQueue.EnqueueEx("请输入气缸名称");
                return;
            }
            if (Regex.IsMatch(NewCylinder.Name, "^\\d"))
            {
                snackbarMessageQueue.EnqueueEx("名称不合法,名称不能以数字开头");
                return;
            }
            if (dataBaseService.Db.Queryable<CylinderInfoEntity>().Where(x => x.Name == NewCylinder.Name).Any())
            {
                snackbarMessageQueue.EnqueueEx("气缸名称重复");
                return;
            }
            if (dataBaseService.Db.Queryable<CylinderInfoEntity>().Where(x => x.Number == NewCylinder.Number).Any())
            {
                snackbarMessageQueue.EnqueueEx("气缸编号重复");
                return;
            }
            NewCylinder.CNName = NewCylinder.Name;
            dataBaseService.Db.Insertable(mapper.Map<CylinderInfoEntity>(NewCylinder)).ExecuteCommand();
            GetCylinderInfo();
            GetCylinderGroups();
            UpdateCylinderDic();
            NewCylinder = new CylinderInfoModel();
            logService.WriteLog(LogTypes.DB.ToString(), $"用户{permissionService.CurrentUserName}创建气缸{NewCylinder.Name}成功.\r\n{JsonConvert.SerializeObject(NewCylinder)}", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("添加气缸成功");
        }

        private DelegateCommand _ShowAllCylinderCommand;
        public DelegateCommand ShowAllCylinderCommand =>
            _ShowAllCylinderCommand ?? (_ShowAllCylinderCommand = new DelegateCommand(ExecuteShowAllCylinderCommand));
        //显示所有气缸
        void ExecuteShowAllCylinderCommand()
        {
            GetCylinderInfo();
            NewCylinder = new CylinderInfoModel();
            CylinderGroupCollection.Clear();
            GetCylinderGroups();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}显示所有气缸成功", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("显示所有气缸成功");
        }

        private DelegateCommand<string> _GroupChangeCommand;
        public DelegateCommand<string> GroupChangeCommand =>
            _GroupChangeCommand ?? (_GroupChangeCommand = new DelegateCommand<string>(ExecuteGroupChangeCommand));

        void ExecuteGroupChangeCommand(string parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter))
            {
                return;
            }
            var cylinders = dataBaseService.Db.Queryable<CylinderInfoEntity>()
                                              .Includes(x => x.SingleValveOutputInfo)
                                              .Includes(x => x.DualValveOriginOutputInfo)
                                              .Includes(x => x.DualValveMovingOutputInfo)
                                              .Includes(x => x.SensorOriginInputInfo)
                                              .Includes(x => x.SensorMovingInputInfo)
                                              .Where(x => x.Group == parameter).OrderBy(x => x.Number).OrderBy(x => x.Id).ToList();
            CylinderCollection.Clear();
            mapper.Map(cylinders, CylinderCollection);
            CylinderCollection.ToList().ForEach(cylinder =>
            {
                cylinder.SingleValveOutputInfo = cylinder.SingleValveOutputInfo != null ? IOOutputCollection.Where(x => x.Id == cylinder.SingleValveOutputId).FirstOrDefault() : null;
                cylinder.DualValveOriginOutputInfo = cylinder.DualValveOriginOutputInfo != null ? IOOutputCollection.Where(x => x.Id == cylinder.DualValveOriginOutputId).FirstOrDefault() : null;
                cylinder.DualValveMovingOutputInfo = cylinder.DualValveMovingOutputInfo != null ? IOOutputCollection.Where(x => x.Id == cylinder.DualValveMovingOutputId).FirstOrDefault() : null;
                cylinder.SensorOriginInputInfo = cylinder.SensorOriginInputInfo != null ? IOInputCollection.Where(x => x.Id == cylinder.SensorOriginInputId).FirstOrDefault() : null;
                cylinder.SensorMovingInputInfo = cylinder.SensorMovingInputInfo != null ? IOInputCollection.Where(x => x.Id == cylinder.SensorMovingInputId).FirstOrDefault() : null;
            });
            snackbarMessageQueue.EnqueueEx("筛选气缸成功");
        }

        private DelegateCommand _AddCylinderCfgCommand;
        public DelegateCommand AddCylinderCfgCommand =>
            _AddCylinderCfgCommand ?? (_AddCylinderCfgCommand = new DelegateCommand(ExecuteAddCylinderCfgCommand));

        void ExecuteAddCylinderCfgCommand()
        {
            IsRightDrawerOpen = true;
        }

        private DelegateCommand _SaveSelectedCylinderCommand;
        public DelegateCommand SaveSelectedCylinderCommand =>
            _SaveSelectedCylinderCommand ?? (_SaveSelectedCylinderCommand = new DelegateCommand(ExecuteSaveSelectedCylinderCommand));

        void ExecuteSaveSelectedCylinderCommand()
        {
            if (CurrentSelectCylinder == null)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"请选中需要保存的气缸", MessageDegree.WARN);
                return;
            }
            if (Regex.IsMatch(CurrentSelectCylinder.Name, "^\\d"))
            {
                snackbarMessageQueue.EnqueueEx("名称不合法,名称不能以数字开头");
                return;
            }
            if (dataBaseService.Db.Queryable<CylinderInfoEntity>().Where(x => x.Name == CurrentSelectCylinder.Name && x.Id != CurrentSelectCylinder.Id).Any())
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"名字{CurrentSelectCylinder.Name}重复!保存失败,数据已恢复.", MessageDegree.INFO);
                snackbarMessageQueue.EnqueueEx("保存失败");
                var orgin = dataBaseService.Db.Queryable<CylinderInfoEntity>().Where(x => x.Id == CurrentSelectCylinder.Id).First();
                mapper.Map(orgin, CurrentSelectCylinder);
                return;
            }
            CurrentSelectCylinder.SingleValveOutputId = CurrentSelectCylinder.SingleValveOutputInfo == null ? 0 : CurrentSelectCylinder.SingleValveOutputInfo.Id;
            CurrentSelectCylinder.DualValveOriginOutputId = CurrentSelectCylinder.DualValveOriginOutputInfo == null ? 0 : CurrentSelectCylinder.DualValveOriginOutputInfo.Id;
            CurrentSelectCylinder.DualValveMovingOutputId = CurrentSelectCylinder.DualValveMovingOutputInfo == null ? 0 : CurrentSelectCylinder.DualValveMovingOutputInfo.Id;
            CurrentSelectCylinder.SensorOriginInputId = CurrentSelectCylinder.SensorOriginInputInfo == null ? 0 : CurrentSelectCylinder.SensorOriginInputInfo.Id;
            CurrentSelectCylinder.SensorMovingInputId = CurrentSelectCylinder.SensorMovingInputInfo == null ? 0 : CurrentSelectCylinder.SensorMovingInputInfo.Id;

            var org = dataBaseService.Db.Queryable<CylinderInfoEntity>().Where(x => x.Id == CurrentSelectCylinder.Id).First();
            ParCompare(org, CurrentSelectCylinder);

            dataBaseService.Db.Updateable(mapper.Map<CylinderInfoEntity>(CurrentSelectCylinder)).ExecuteCommand();
            GetCylinderGroups();
            UpdateCylinderDic();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}保存气缸信息成功", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("保存成功");
        }

        private DelegateCommand _SaveAllCylindersCommand;
        public DelegateCommand SaveAllCylindersCommand =>
            _SaveAllCylindersCommand ?? (_SaveAllCylindersCommand = new DelegateCommand(ExecuteSaveAllCylindersCommand));

        void ExecuteSaveAllCylindersCommand()
        {
            bool repeateName = CylinderCollection.GroupBy(x => x.Name).Where(x => x.Count() > 1).Count() > 0;
            if (repeateName)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"名字重复,无法保存", MessageDegree.ERROR);
                snackbarMessageQueue.EnqueueEx("保存失败");
                return;
            }
            foreach (var item in CylinderCollection)
            {
                if (Regex.IsMatch(item.Name, "^\\d"))
                {
                    snackbarMessageQueue.EnqueueEx("名称不合法,名称不能以数字开头");
                    return;
                }
            }

            CylinderCollection.ToList().ForEach(cylinder =>
            {
                cylinder.SingleValveOutputId = cylinder.SingleValveOutputInfo == null ? 0 : cylinder.SingleValveOutputInfo.Id;
                cylinder.DualValveOriginOutputId = cylinder.DualValveOriginOutputInfo == null ? 0 : cylinder.DualValveOriginOutputInfo.Id;
                cylinder.DualValveMovingOutputId = cylinder.DualValveMovingOutputInfo == null ? 0 : cylinder.DualValveMovingOutputInfo.Id;
                cylinder.SensorOriginInputId = cylinder.SensorOriginInputInfo == null ? 0 : cylinder.SensorOriginInputInfo.Id;
                cylinder.SensorMovingInputId = cylinder.SensorMovingInputInfo == null ? 0 : cylinder.SensorMovingInputInfo.Id;
            });
            dataBaseService.Db.Queryable<CylinderInfoEntity>().ToList().ForEach(orgItem =>
            {
                var updateItem = CylinderCollection.Where(x => x.Id == orgItem.Id).FirstOrDefault();
                if (updateItem != null) { ParCompare(orgItem, updateItem); }
            });

            dataBaseService.Db.Updateable(mapper.Map<List<CylinderInfoEntity>>(CylinderCollection)).ExecuteCommand();
            GetCylinderGroups();
            UpdateCylinderDic();
            logService.WriteLog(LogTypes.DB.ToString(), $@"保存成功", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("保存成功");
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            GetIOInputInfo();
            GetIOOutputInfo();
            GetCylinderInfo();
            CylinderGroupCollection.Clear();
            GetCylinderGroups();
            NewCylinder = new CylinderInfoModel();
            logService.ShowOnUI += LogService_ShowOnUI;
            MessageList.Clear();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}进入气缸配置页面", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("设备运转中,请勿修改参数");
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            //motionResourceInit.InitCylinderDicCollection();
            logService.ShowOnUI -= LogService_ShowOnUI;
        }
        #region PrivateMethod
        private void UpdateCylinderDic()
        {
            motionResourceInit.InitCylinderDicCollection();
        }

        private void LogService_ShowOnUI(MessageItem obj)
        {
            MessageList.Add(obj);
            if (MessageList.Count > 1000) MessageList.RemoveAt(0);
        }

        private void GetCylinderInfo()
        {
            CylinderCollection.Clear();
            var cylinders = dataBaseService.Db.Queryable<CylinderInfoEntity>()
                .Includes(x => x.SingleValveOutputInfo)
                .Includes(x => x.DualValveOriginOutputInfo)
                .Includes(x => x.DualValveMovingOutputInfo)
                .Includes(x => x.SensorOriginInputInfo)
                .Includes(x => x.SensorMovingInputInfo)
                .OrderBy(x => x.Number)
                .OrderBy(x => x.Id)
                .ToList();
            mapper.Map(cylinders, CylinderCollection);
            CylinderCollection.ToList().ForEach(cylinder =>
            {
                cylinder.SingleValveOutputInfo = cylinder.SingleValveOutputInfo != null ? IOOutputCollection.Where(x => x.Id == cylinder.SingleValveOutputId).FirstOrDefault() : null;
                cylinder.DualValveOriginOutputInfo = cylinder.DualValveOriginOutputInfo != null ? IOOutputCollection.Where(x => x.Id == cylinder.DualValveOriginOutputId).FirstOrDefault() : null;
                cylinder.DualValveMovingOutputInfo = cylinder.DualValveMovingOutputInfo != null ? IOOutputCollection.Where(x => x.Id == cylinder.DualValveMovingOutputId).FirstOrDefault() : null;
                cylinder.SensorOriginInputInfo = cylinder.SensorOriginInputInfo != null ? IOInputCollection.Where(x => x.Id == cylinder.SensorOriginInputId).FirstOrDefault() : null;
                cylinder.SensorMovingInputInfo = cylinder.SensorMovingInputInfo != null ? IOInputCollection.Where(x => x.Id == cylinder.SensorMovingInputId).FirstOrDefault() : null;
            });
        }

        private void GetCylinderGroups()
        {
            var cylinderGroup = dataBaseService.Db.Queryable<CylinderInfoEntity>().Distinct().Select(x => x.Group).ToList();
            cylinderGroup.ForEach(x => { if (!string.IsNullOrWhiteSpace(x) && !CylinderGroupCollection.Contains(x)) CylinderGroupCollection.Add(x); });
        }

        private void GetIOInputInfo()
        {
            var inputs = dataBaseService.Db.Queryable<IOInputInfoEntity>().OrderBy(x => x.Number).OrderBy(x => x.Id).ToList();
            mapper.Map(inputs, IOInputCollection);
        }

        private void GetIOOutputInfo()
        {
            var outpus = dataBaseService.Db.Queryable<IOOutputInfoEntity>().OrderBy(x => x.Number).OrderBy(x => x.Id).ToList();
            mapper.Map(outpus, IOOutputCollection);
        }

        private void ParCompare(CylinderInfoEntity orgItem, CylinderInfoModel updateItem)
        {
            if (orgItem.Name != updateItem.Name) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Name: {orgItem.Name}=>{updateItem.Name}", MessageDegree.INFO);
            if (orgItem.Number != updateItem.Number) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Number:{orgItem.Number}=>{updateItem.Number}", MessageDegree.INFO);
            if (orgItem.CNName != updateItem.CNName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} CNName:{orgItem.CNName}=>{updateItem.CNName}", MessageDegree.INFO);
            if (orgItem.ENName != updateItem.ENName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} ENName:{orgItem.ENName}=>{updateItem.ENName}", MessageDegree.INFO);
            if (orgItem.VNName != updateItem.VNName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} VNName:{orgItem.VNName}=>{updateItem.VNName}", MessageDegree.INFO);
            if (orgItem.XXName != updateItem.XXName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} XXName:{orgItem.XXName}=>{updateItem.XXName}", MessageDegree.INFO);
            if (orgItem.Backup != updateItem.Backup) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Backup:{orgItem.Backup}=>{updateItem.Backup}", MessageDegree.INFO);
            if (orgItem.Group != updateItem.Group) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Group:{orgItem.Group}=>{updateItem.Group}", MessageDegree.INFO);
            if (orgItem.Tag != updateItem.Tag) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Tag:{orgItem.Tag}=>{updateItem.Tag}", MessageDegree.INFO);
            if (orgItem.OriginPointTimeout != updateItem.OriginPointTimeout) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} OriginPointTimeout:{orgItem.OriginPointTimeout}=>{updateItem.OriginPointTimeout}", MessageDegree.INFO);
            if (orgItem.MovingPointTimeout != updateItem.MovingPointTimeout) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} MovingPointTimeout:{orgItem.MovingPointTimeout}=>{updateItem.MovingPointTimeout}", MessageDegree.INFO);
            if (orgItem.ValveType != updateItem.ValveType) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} ValveType:{orgItem.ValveType}=>{updateItem.ValveType}", MessageDegree.INFO);
            if (orgItem.SingleValveOutputId != updateItem.SingleValveOutputId) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} SingleValveOutputId:{orgItem.SingleValveOutputId}=>{updateItem.SingleValveOutputId}", MessageDegree.INFO);
            if (orgItem.DualValveOriginOutputId != updateItem.DualValveOriginOutputId) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} DualValveOriginOutputId:{orgItem.DualValveOriginOutputId}=>{updateItem.DualValveOriginOutputId}", MessageDegree.INFO);
            if (orgItem.DualValveMovingOutputId != updateItem.DualValveMovingOutputId) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} DualValveMovingOutputId:{orgItem.DualValveMovingOutputId}=>{updateItem.DualValveMovingOutputId}", MessageDegree.INFO);
            if (orgItem.SensorType != updateItem.SensorType) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} SensorType:{orgItem.SensorType}=>{updateItem.SensorType}", MessageDegree.INFO);
            if (orgItem.DelayTime != updateItem.DelayTime) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} DelayTime:{orgItem.DelayTime}=>{updateItem.DelayTime}", MessageDegree.INFO);
            if (orgItem.SensorOriginInputId != updateItem.SensorOriginInputId) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} DualSensorOriginInputId:{orgItem.SensorOriginInputId}=>{updateItem.SensorOriginInputId}", MessageDegree.INFO);
            if (orgItem.SensorMovingInputId != updateItem.SensorMovingInputId) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} DualSensorMovingInputId:{orgItem.SensorMovingInputId}=>{updateItem.SensorMovingInputId}", MessageDegree.INFO);
            if (orgItem.ShiedSensorOriginInput != updateItem.ShiedSensorOriginInput) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} ShiedSensorOriginInput:{orgItem.ShiedSensorOriginInput}=>{updateItem.ShiedSensorOriginInput}", MessageDegree.INFO);
            if (orgItem.ShiedSensorOriginInputDelayTime != updateItem.ShiedSensorOriginInputDelayTime) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} ShiedSensorOriginInputDelayTime:{orgItem.ShiedSensorOriginInputDelayTime}=>{updateItem.ShiedSensorOriginInputDelayTime}", MessageDegree.INFO);
            if (orgItem.ShiedSensorMovingInput != updateItem.ShiedSensorMovingInput) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} ShiedSensorMovingInput:{orgItem.ShiedSensorMovingInput}=>{updateItem.ShiedSensorMovingInput}", MessageDegree.INFO);
            if (orgItem.ShiedSensorMovingInputDelayTime != updateItem.ShiedSensorMovingInputDelayTime) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} ShiedSensorMovingInputDelayTime:{orgItem.ShiedSensorMovingInputDelayTime}=>{updateItem.ShiedSensorMovingInputDelayTime}", MessageDegree.INFO);
        }
        #endregion
    }
}
