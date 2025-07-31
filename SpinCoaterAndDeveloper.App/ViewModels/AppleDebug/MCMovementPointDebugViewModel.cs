using AutoMapper;
using DataBaseServiceInterface;
using LogServiceInterface;
using MaterialDesignThemes.Wpf;
using MotionCardServiceInterface;
using MotionControlActuation;
using PermissionServiceInterface;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using SpinCoaterAndDeveloper.App.Common.Models;
using SpinCoaterAndDeveloper.App.Extensions;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using SpinCoaterAndDeveloper.Shared.Extensions;
using SpinCoaterAndDeveloper.Shared.Models.MotionControlModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class MCMovementPointDebugViewModel : BindableBase, INavigationAware
    {
        private readonly IContainerProvider containerProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly IDataBaseService dataBaseService;
        private readonly IDialogService dialogService;
        private readonly IMapper mapper;
        private readonly ILogService logService;
        private readonly ISnackbarMessageQueue snackbarMessageQueue;
        private readonly IPermissionService permissionService;
        private readonly IMotionCardService motionCardService;

        private CancellationTokenSource relMoveArrivedCancellationToken = null;
        private CancellationTokenSource moveMentPositionArrivedCancellationToken = null;

        #region Binding
        private bool _ButtonEnable = true;
        public bool ButtonEnable
        {
            get { return _ButtonEnable; }
            set { SetProperty(ref _ButtonEnable, value); }
        }
        private MovementPointMonitorModel _CurrentSelectPointName;
        public MovementPointMonitorModel CurrentSelectPointName
        {
            get { return _CurrentSelectPointName; }
            set { SetProperty(ref _CurrentSelectPointName, value); }
        }
        private double _GlobalVel;
        public double GlobalVel
        {
            get { return _GlobalVel; }
            set { SetProperty(ref _GlobalVel, value); }
        }
        private string _MovementPointNameFilter;
        public string MovementPointNameFilter
        {
            get { return _MovementPointNameFilter; }
            set { SetProperty(ref _MovementPointNameFilter, value); }
        }
        #endregion
        public ObservableCollection<MovementPointMonitorModel> MovementPointMonitorCollection { get; set; } = new ObservableCollection<MovementPointMonitorModel>();
        public ObservableCollection<string> MovementPointGroupCollection { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<MessageItem> MessageList { get; set; } = new ObservableCollection<MessageItem>();

        public MCMovementPointDebugViewModel(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
            this.eventAggregator = containerProvider.Resolve<IEventAggregator>();
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
            this.dialogService = containerProvider.Resolve<IDialogService>();
            this.mapper = containerProvider.Resolve<IMapper>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();
            this.permissionService = containerProvider.Resolve<IPermissionService>();
            this.motionCardService = containerProvider.Resolve<IMotionCardService>();

            CollectionView collectionView = CollectionViewSource.GetDefaultView(MovementPointMonitorCollection) as CollectionView;
            collectionView.Filter = x =>
            {
                if (string.IsNullOrWhiteSpace(MovementPointNameFilter)) return true;
                return (x as MovementPointMonitorModel).ShowOnUIName.Contains(MovementPointNameFilter);
            };
        }

        private DelegateCommand _MovementPointNameFilterCommand;
        public DelegateCommand MovementPointNameFilterCommand =>
            _MovementPointNameFilterCommand ?? (_MovementPointNameFilterCommand = new DelegateCommand(ExecuteMovementPointNameFilterCommand));

        void ExecuteMovementPointNameFilterCommand()
        {
            CollectionViewSource.GetDefaultView(MovementPointMonitorCollection).Refresh();
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

        private DelegateCommand _SaveGlobalVelCommand;
        public DelegateCommand SaveGlobalVelCommand =>
            _SaveGlobalVelCommand ?? (_SaveGlobalVelCommand = new DelegateCommand(ExecuteSaveGlobalVelCommand));

        void ExecuteSaveGlobalVelCommand()
        {
            Configuration cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            cfg.AppSettings.Settings["GlobalVelPercentage"].Value = GlobalVel.ToString();
            cfg.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
            GlobalValues.GlobalVelPercentage = GlobalVel;
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}修改全局速度到{GlobalVel}%", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("修改成功");
        }

        //示教
        private DelegateCommand _TeachPointCommand;
        public DelegateCommand TeachPointCommand =>
            _TeachPointCommand ?? (_TeachPointCommand = new DelegateCommand(ExecuteTeachPointCommand).ObservesCanExecute(() => ButtonEnable));

        void ExecuteTeachPointCommand()
        {
            if (CurrentSelectPointName == null)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"请选择点位", MessageDegree.WARN);
                return;
            }
            foreach (var item in CurrentSelectPointName.MovementPointPositionsMonitorCollection)
            {
                if (item.InvolveAxis == true && item.MovementPointType == MovementType.Abs)
                {
                    item.AbsValue = MotionControlResource.AxisResource[item.AxisInfo.Name].EncPos / MotionControlResource.AxisResource[item.AxisInfo.Name].Proportion;
                }
                if (item.InvolveAxis == true && item.MovementPointType != MovementType.Abs)
                {
                    logService.WriteLog(LogTypes.DB.ToString(), $@"点位{CurrentSelectPointName.Name}相关轴{item.AxisInfo.Name}不是绝对类型,无法示教", MessageDegree.ERROR);
                }
            }
        }

        private DelegateCommand _SaveDataCommand;
        public DelegateCommand SaveDataCommand =>
            _SaveDataCommand ?? (_SaveDataCommand = new DelegateCommand(ExecuteSaveDataCommand));

        void ExecuteSaveDataCommand()
        {
            if (GlobalValues.MachineStatus == FSMStateCode.PowerUpping || GlobalValues.MachineStatus == FSMStateCode.EmergencyStopping)
            {
                var result = dialogService.QuestionShowDialog("警告", "设备可能未复位,位置数据不准确,请确认是否继续保存.", "取消", "确定");
                if (result != ButtonResult.Yes) return;
            }
            ButtonEnable = false;
            try
            {
                dataBaseService.Db.Queryable<MovementPointInfoEntity>()
                    .Includes(x => x.MovementPointPositions.OrderBy(m => m.AxisInfo.Number).OrderBy(m => m.AxisInfo.Id).ToList(), y => y.AxisInfo)
                    .Includes(x => x.MovementPointSecurities.OrderBy(m => m.Sequence).ToList())
                    .Includes(x => x.MovementPointPositions.OrderBy(m => m.AxisInfo.Number).OrderBy(m => m.AxisInfo.Id).ToList(), y => y.JogIOInputInfo)
                    .Where(x => x.ProductInfo.Select == true)
                    .ToList()
                    .ForEach(orgItem =>
                    {
                        var updateItem = MovementPointMonitorCollection.Where(x => x.Id == orgItem.Id).FirstOrDefault();
                        if (updateItem != null) MovementPointCompare(orgItem, updateItem);
                    });

                dataBaseService.Db.UpdateNav(mapper.Map<List<MovementPointInfoEntity>>(MovementPointMonitorCollection)).Include(x => x.MovementPointPositions).Include(x => x.MovementPointSecurities).ExecuteCommand();
                //更新全局字典中的数据
                foreach (var point in MovementPointMonitorCollection)
                {
                    foreach (var newAxis in point.MovementPointPositionsMonitorCollection)
                    {
                        var oldData = GlobalValues.MCPointDicCollection[point.Name].MovementPointPositions.Where(x => x.AxisInfo.Name == newAxis.AxisInfo.Name).FirstOrDefault();
                        if (oldData != null)
                        {
                            if (oldData.AbsValue != newAxis.AbsValue) oldData.AbsValue = newAxis.AbsValue;
                            if (oldData.RelValue != newAxis.RelValue) oldData.RelValue = newAxis.RelValue;
                            if (oldData.Vel != newAxis.Vel) oldData.Vel = newAxis.Vel;
                        }
                    }
                }
                logService.WriteLog(LogTypes.DB.ToString(), $@"保存成功", MessageDegree.INFO);
                snackbarMessageQueue.EnqueueEx("保存成功");
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"保存点位数据异常:{ex.Message}", ex);
            }
            finally
            {
                ButtonEnable = true;
            }
        }

        //移动到点位
        private DelegateCommand _MoveToSelectedPointCommand;
        public DelegateCommand MoveToSelectedPointCommand =>
            _MoveToSelectedPointCommand ?? (_MoveToSelectedPointCommand = new DelegateCommand(ExecuteMoveToSelectedPointCommand).ObservesCanExecute(() => ButtonEnable));

        async void ExecuteMoveToSelectedPointCommand()
        {
            try
            {
                ButtonEnable = false;
                if (CurrentSelectPointName == null)
                {
                    logService.WriteLog(LogTypes.DB.ToString(), $@"未选中点位", MessageDegree.INFO);
                    return;
                }
                var confirmResult = dialogService.QuestionShowDialog($"警告", $"确定移动到点位{CurrentSelectPointName.Name}?");
                if (confirmResult != ButtonResult.Yes) return;
                logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}选择移动到点位{CurrentSelectPointName.Name}", MessageDegree.INFO);

                moveMentPositionArrivedCancellationToken = new CancellationTokenSource();
                MovementPointMoveAndWaitArrived mcMoveAndWaitArrived = new MovementPointMoveAndWaitArrived(containerProvider);
                var result = await mcMoveAndWaitArrived.StartAsync(CurrentSelectPointName.Name, moveMentPositionArrivedCancellationToken);
                if (result)
                {
                    logService.WriteLog(LogTypes.DB.ToString(), $@"移动到点位完成", MessageDegree.INFO);
                    snackbarMessageQueue.EnqueueEx("移动到点位完成");
                }
                else
                {
                    logService.WriteLog(LogTypes.DB.ToString(), $@"移动到点位失败", MessageDegree.INFO);
                    snackbarMessageQueue.EnqueueEx("移动到点位失败");
                }
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"移动到点位异常:{ex.Message}", ex);
            }
            finally
            {
                ButtonEnable = true;
            }
        }

        //停止点位移动
        private DelegateCommand _StopMoveToSelectedCommand;
        public DelegateCommand StopMoveToSelectedCommand =>
            _StopMoveToSelectedCommand ?? (_StopMoveToSelectedCommand = new DelegateCommand(ExecuteStopMoveToSelectedCommand));

        void ExecuteStopMoveToSelectedCommand()
        {
            if (CurrentSelectPointName == null)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"未选中点位", MessageDegree.INFO);
                return;
            }
            moveMentPositionArrivedCancellationToken?.Cancel();
        }

        //下拉选择点位分组
        private DelegateCommand<string> _MovementPointGourpChangedCommand;
        public DelegateCommand<string> MovementPointGourpChangedCommand =>
            _MovementPointGourpChangedCommand ?? (_MovementPointGourpChangedCommand = new DelegateCommand<string>(ExecuteMovementPointGourpChangedCommand));

        void ExecuteMovementPointGourpChangedCommand(string parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter))
            {
                return;
            }
            var mPoints = dataBaseService.Db.Queryable<MovementPointInfoEntity>()
                .Includes(x => x.MovementPointPositions.OrderBy(m => m.AxisInfo.Number).OrderBy(m => m.AxisInfo.Id).ToList(), y => y.AxisInfo)
                .Includes(x => x.MovementPointPositions.OrderBy(m => m.AxisInfo.Number).OrderBy(m => m.Id).ToList(), y => y.JogIOInputInfo)
                .Includes(x => x.MovementPointSecurities.OrderBy(m => m.Sequence).ToList())
                .Where(x => x.ProductInfo.Select == true && x.Group == parameter)
                .ToList();
            MovementPointMonitorCollection.Clear();
            mapper.Map(mPoints, MovementPointMonitorCollection);
            snackbarMessageQueue.EnqueueEx("点位筛选成功");
        }

        //显示所有点位
        private DelegateCommand _ShowAllMovementPointCommand;
        public DelegateCommand ShowAllMovementPointCommand =>
            _ShowAllMovementPointCommand ?? (_ShowAllMovementPointCommand = new DelegateCommand(ExecuteShowAllMovementPointCommand).ObservesCanExecute(() => ButtonEnable));

        void ExecuteShowAllMovementPointCommand()
        {
            MovementPointNameFilter = "";
            GetMovementPoints();
            MovementPointGroupCollection.Clear();
            GetMovementPointGroup();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}显示所有点位成功", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("显示所有点位成功");
        }

        //更细记录Jog速度
        private DelegateCommand<AxisMonitorModel> _UpdateJogVelCommand;
        public DelegateCommand<AxisMonitorModel> UpdateJogVelCommand =>
            _UpdateJogVelCommand ?? (_UpdateJogVelCommand = new DelegateCommand<AxisMonitorModel>(ExecuteUpdateJogVelCommand));

        void ExecuteUpdateJogVelCommand(AxisMonitorModel parameter)
        {
            if (dataBaseService.Db.Queryable<AxisInfoEntity>().Where(x => x.Id == parameter.Id).First().JogVel == parameter.JogVel)
                return;
            foreach (var point in MovementPointMonitorCollection)
            {
                foreach (var item in point.MovementPointPositionsMonitorCollection)
                {
                    if (item.AxisInfo.Name == parameter.Name)
                    {
                        item.AxisInfo.JogVel = parameter.JogVel;
                    }
                }
            }
            var result = dataBaseService.Db.Updateable(mapper.Map<AxisInfoEntity>(parameter)).ExecuteCommand();
            if (result != 0)
                logService.WriteLog(LogTypes.DB.ToString(), $@"Jog速度已保存", MessageDegree.INFO);
            else
                logService.WriteLog(LogTypes.DB.ToString(), $@"Jog速度保存失败", MessageDegree.ERROR);
        }

        //Jog运动
        private DelegateCommand<AxisMonitorModel> _JogNegativeCommand;
        public DelegateCommand<AxisMonitorModel> JogNegativeCommand =>
            _JogNegativeCommand ?? (_JogNegativeCommand = new DelegateCommand<AxisMonitorModel>(ExecuteJogNegativeCommand));

        void ExecuteJogNegativeCommand(AxisMonitorModel parameter)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}对轴{parameter.Name}负向点动", MessageDegree.INFO);
            //Jog加减速使用回原加速度
            motionCardService.JogMoveStart((short)parameter.AxisIdOnCard, parameter.JogVel * parameter.Proportion, Direction.Negative, parameter.HomeAcc * parameter.Proportion, parameter.HomeAcc * parameter.Proportion);
        }

        private DelegateCommand<AxisMonitorModel> _JogPositiveCommand;
        public DelegateCommand<AxisMonitorModel> JogPositiveCommand =>
            _JogPositiveCommand ?? (_JogPositiveCommand = new DelegateCommand<AxisMonitorModel>(ExecuteJogPositiveCommand));

        void ExecuteJogPositiveCommand(AxisMonitorModel parameter)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}对轴{parameter.Name}正向点动", MessageDegree.INFO);
            //Jog加减速使用回原加速度
            motionCardService.JogMoveStart((short)parameter.AxisIdOnCard, parameter.JogVel * parameter.Proportion, Direction.Positive, parameter.HomeAcc * parameter.Proportion, parameter.HomeAcc * parameter.Proportion);
        }

        //Jog停止
        private DelegateCommand<AxisMonitorModel> _JogMoveStopCommand;
        public DelegateCommand<AxisMonitorModel> JogMoveStopCommand =>
            _JogMoveStopCommand ?? (_JogMoveStopCommand = new DelegateCommand<AxisMonitorModel>(ExecuteJogMoveStopCommand));

        void ExecuteJogMoveStopCommand(AxisMonitorModel parameter)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}停止点动", MessageDegree.INFO);
            motionCardService.JogMoveStop((short)parameter.AxisIdOnCard);
        }

        //上使能
        private DelegateCommand<AxisMonitorModel> _AxisServoOnCommand;
        public DelegateCommand<AxisMonitorModel> AxisServoOnCommand =>
            _AxisServoOnCommand ?? (_AxisServoOnCommand = new DelegateCommand<AxisMonitorModel>(ExecuteAxisServoOnCommand).ObservesCanExecute(() => ButtonEnable));

        void ExecuteAxisServoOnCommand(AxisMonitorModel parameter)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}对轴{parameter.Name}上使能", MessageDegree.INFO);
            motionCardService.AxisServo((short)parameter.AxisIdOnCard, true);
        }

        //清除轴错误
        private DelegateCommand<AxisMonitorModel> _AxisClearErrorCommand;
        public DelegateCommand<AxisMonitorModel> AxisClearErrorCommand =>
            _AxisClearErrorCommand ?? (_AxisClearErrorCommand = new DelegateCommand<AxisMonitorModel>(ExecuteAxisClearErrorCommand).ObservesCanExecute(() => ButtonEnable));

        void ExecuteAxisClearErrorCommand(AxisMonitorModel parameter)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}对轴{parameter.Name}清除错误", MessageDegree.INFO);
            motionCardService.ClearAxSts((short)parameter.AxisIdOnCard);
        }
        //下使能
        private DelegateCommand<AxisMonitorModel> _AxisServoOffCommand;
        public DelegateCommand<AxisMonitorModel> AxisServoOffCommand =>
            _AxisServoOffCommand ?? (_AxisServoOffCommand = new DelegateCommand<AxisMonitorModel>(ExecuteAxisServoOffCommand).ObservesCanExecute(() => ButtonEnable));

        void ExecuteAxisServoOffCommand(AxisMonitorModel parameter)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}对轴{parameter.Name}下使能", MessageDegree.INFO);
            motionCardService.AxisServo((short)parameter.AxisIdOnCard, false);
        }

        private DelegateCommand<AxisMonitorModel> _RelMoveCommand;
        public DelegateCommand<AxisMonitorModel> RelMoveCommand =>
            _RelMoveCommand ?? (_RelMoveCommand = new DelegateCommand<AxisMonitorModel>(ExecuteRelMoveCommand).ObservesCanExecute(() => ButtonEnable));

        async void ExecuteRelMoveCommand(AxisMonitorModel parameter)
        {
            try
            {
                ButtonEnable = false;
                logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}启动寸动,寸动距离{parameter.RelMoveDistance}", MessageDegree.INFO);
                motionCardService.AxisServo((short)parameter.AxisIdOnCard, true);
                motionCardService.StartMoveRel((short)parameter.AxisIdOnCard, 0, 0, parameter.JogVel * parameter.Proportion, parameter.HomeAcc * parameter.Proportion, parameter.HomeAcc * parameter.Proportion, parameter.RelMoveDistance * parameter.Proportion);
                relMoveArrivedCancellationToken = new CancellationTokenSource();
                var result = await Task.Run(() =>
                {
                    while (true)
                    {
                        if (relMoveArrivedCancellationToken.IsCancellationRequested)
                        {
                            foreach (var axis in MotionControlResource.AxisResource)
                                motionCardService.StopMove((short)axis.Value.AxisIdOnCard, 1);
                            return false;
                        }
                        if (motionCardService.RealTimeCheckAxisArrivedEx(parameter.Name)) break;
                        Thread.Sleep(100);
                    }
                    return true;
                }, relMoveArrivedCancellationToken.Token);

                var msg = result ? "寸动到位" : "寸动失败";
                logService.WriteLog(LogTypes.DB.ToString(), $@"{msg}", MessageDegree.INFO);
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"寸动异常{ex.Message}", ex);
            }
            finally
            {
                ButtonEnable = true;
            }
        }

        private DelegateCommand<AxisMonitorModel> _RelMoveStopCommand;
        public DelegateCommand<AxisMonitorModel> RelMoveStopCommand =>
            _RelMoveStopCommand ?? (_RelMoveStopCommand = new DelegateCommand<AxisMonitorModel>(ExecuteRelMoveStopCommand));

        void ExecuteRelMoveStopCommand(AxisMonitorModel parameter)
        {
            relMoveArrivedCancellationToken?.Cancel();
            moveMentPositionArrivedCancellationToken?.Cancel();
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            //每次进入页面,刷新界面内容
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                MovementPointGroupCollection.Clear();
                GetMovementPointGroup();
                GetMovementPoints();
            }), System.Windows.Threading.DispatcherPriority.Render);
            GlobalVel = GlobalValues.GlobalVelPercentage;
            logService.ShowOnUI += LogService_ShowOnUI;
            MessageList.Clear();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}进入点位调试页面", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("点位调试页面,保存后生效.");
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            relMoveArrivedCancellationToken?.Cancel();
            moveMentPositionArrivedCancellationToken?.Cancel();
            logService.ShowOnUI -= LogService_ShowOnUI;
        }

        private void GetMovementPoints()
        {
            MovementPointMonitorCollection.Clear();
            var mPoints = dataBaseService.Db.Queryable<MovementPointInfoEntity>()
                .Includes(x => x.MovementPointPositions.OrderBy(m => m.AxisInfo.Number).OrderBy(m => m.Id).ToList(), y => y.AxisInfo)
                .Includes(x => x.MovementPointPositions.OrderBy(m => m.AxisInfo.Number).OrderBy(m => m.Id).ToList(), y => y.JogIOInputInfo)
                .Includes(x => x.MovementPointSecurities.OrderBy(m => m.Sequence).ToList())
                .Where(x => x.ProductInfo.Select == true)
                .ToList();
            mapper.Map(mPoints, MovementPointMonitorCollection);
        }

        private void GetMovementPointGroup()
        {
            var mPointGroups = dataBaseService.Db.Queryable<MovementPointInfoEntity>().Where(x => x.ProductInfo.Select == true).Distinct().Select(x => x.Group).ToList();
            mPointGroups.ForEach(x => { if (!string.IsNullOrWhiteSpace(x) && !MovementPointGroupCollection.Contains(x)) MovementPointGroupCollection.Add(x); });
        }

        private void LogService_ShowOnUI(MessageItem obj)
        {
            MessageList.Add(obj);
            if (MessageList.Count > 1000) MessageList.RemoveAt(0);
        }

        private void MovementPointCompare(MovementPointInfoEntity orgItem, MovementPointMonitorModel updateItem)
        {
            if (orgItem.Name != updateItem.Name) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Name: {orgItem.Name}=>{updateItem.Name}", MessageDegree.INFO);
            if (orgItem.CNName != updateItem.CNName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} CNName: {orgItem.CNName}=>{updateItem.CNName}", MessageDegree.INFO);
            if (orgItem.ENName != updateItem.ENName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} ENName: {orgItem.ENName}=>{updateItem.ENName}", MessageDegree.INFO);
            if (orgItem.VNName != updateItem.VNName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} VNName: {orgItem.VNName}=>{updateItem.VNName}", MessageDegree.INFO);
            if (orgItem.XXName != updateItem.XXName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} XXName: {orgItem.XXName}=>{updateItem.XXName}", MessageDegree.INFO);
            if (orgItem.Group != updateItem.Group) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Group: {orgItem.Group}=>{updateItem.Group}", MessageDegree.INFO);
            if (orgItem.Backup != updateItem.Backup) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Backup: {orgItem.Backup}=>{updateItem.Backup}", MessageDegree.INFO);
            if (orgItem.Tag != updateItem.Tag) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Tag: {orgItem.Tag}=>{updateItem.Tag}", MessageDegree.INFO);
            if (orgItem.ManualMoveSecurityTimeOut != updateItem.ManualMoveSecurityTimeOut) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} ManualMoveSecurityTimeOut: {orgItem.ManualMoveSecurityTimeOut}=>{updateItem.ManualMoveSecurityTimeOut}", MessageDegree.INFO);
            if (orgItem.ManualMoveSecurityEnable != updateItem.ManualMoveSecurityEnable) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} ManualMoveSecurityEnable: {orgItem.ManualMoveSecurityEnable}=>{updateItem.ManualMoveSecurityEnable}", MessageDegree.INFO);
            if (orgItem.MovementPointSecurities.Count != updateItem.MovementPointSecuritiesMonitorCollection.Count) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} MovementPointSecurities: {orgItem.MovementPointSecurities.Count}=>{updateItem.MovementPointSecuritiesMonitorCollection.Count}", MessageDegree.INFO);

            foreach (var orgMP in orgItem.MovementPointPositions)
            {
                foreach (var updateMP in updateItem.MovementPointPositionsMonitorCollection)
                {
                    if (orgMP.AxisInfoId == updateMP.AxisInfoId)
                    {
                        if (orgMP.MovementPointType != updateMP.MovementPointType) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgMP.Id} MovementPointType: {orgMP.MovementPointType}=>{updateMP.MovementPointType}", MessageDegree.INFO);
                        if (orgMP.AbsValue != updateMP.AbsValue) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgMP.Id} AbsValue: {orgMP.AbsValue}=>{updateMP.AbsValue}", MessageDegree.INFO);
                        if (orgMP.RelValue != updateMP.RelValue) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgMP.Id} RelValue: {orgMP.RelValue}=>{updateMP.RelValue}", MessageDegree.INFO);
                        if (orgMP.JogIOInputId != updateMP.JogIOInputId) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgMP.Id} JogIOInputId: {orgMP.JogIOInputId}=>{updateMP.JogIOInputId}", MessageDegree.INFO);
                        if (orgMP.JogArrivedCondition != updateMP.JogArrivedCondition) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgMP.Id} JogArrivedCondition: {orgMP.JogArrivedCondition}=>{updateMP.JogArrivedCondition}", MessageDegree.INFO);
                        if (orgMP.JogDirection != updateMP.JogDirection) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgMP.Id} JogDirection: {orgMP.JogDirection}=>{updateMP.JogDirection}", MessageDegree.INFO);
                        if (orgMP.Vel != updateMP.Vel) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgMP.Id} Vel: {orgMP.Vel}=>{updateMP.Vel}", MessageDegree.INFO);
                        if (orgMP.Acc != updateMP.Acc) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgMP.Id} Acc: {orgMP.Acc}=>{updateMP.Acc}", MessageDegree.INFO);
                        if (orgMP.Dec != updateMP.Dec) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgMP.Id} Dec: {orgMP.Dec}=>{updateMP.Dec}", MessageDegree.INFO);
                        if (orgMP.Offset != updateMP.Offset) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgMP.Id} Offset: {orgMP.Offset}=>{updateMP.Offset}", MessageDegree.INFO);
                        if (orgMP.InvolveAxis != updateMP.InvolveAxis) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgMP.Id} InvolveAxis: {orgMP.InvolveAxis}=>{updateMP.InvolveAxis}", MessageDegree.INFO);
                    }
                }
            }
        }
    }
}
