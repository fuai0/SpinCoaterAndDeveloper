using AutoMapper;
using DataBaseServiceInterface;
using DryIoc;
using LogServiceInterface;
using MaterialDesignThemes.Wpf;
using MotionCardServiceInterface;
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
using SpinCoaterAndDeveloper.App.Views;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using SpinCoaterAndDeveloper.Shared.Extensions;
using SpinCoaterAndDeveloper.Shared.Models.MotionControlModels;
using SpinCoaterAndDeveloper.Shared.Services.MotionResourceInitService;
using SqlSugar;
using SqlSugar.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class MCMovementPointConfigViewModel : BindableBase, INavigationAware
    {
        private readonly IContainerProvider containerProvider;
        private readonly IDialogHostService dialogHostService;
        private readonly IDialogService dialogService;
        private readonly IEventAggregator eventAggregator;
        private readonly IDataBaseService dataBaseService;
        private readonly IMapper mapper;
        private readonly ILogService logService;
        private readonly ISnackbarMessageQueue snackbarMessageQueue;
        private readonly IMotionResourceInit motionResourceInit;
        private readonly IPermissionService permissionService;
        private readonly IMotionCardService motionCardService;

        private CancellationTokenSource relMoveArrivedCancellationToken = null;
        private CancellationTokenSource moveMentPositionArrivedCancellationToken = null;

        #region Binding
        private bool buttonEnable = true;
        public bool ButtonEnable
        {
            get { return buttonEnable; }
            set { SetProperty(ref buttonEnable, value); }
        }

        private bool _IsRightDrawerOpen;
        public bool IsRightDrawerOpen
        {
            get { return _IsRightDrawerOpen; }
            set { SetProperty(ref _IsRightDrawerOpen, value); }
        }

        private MovementPointInfoModel _NewMovementPointName;
        public MovementPointInfoModel NewMovementPointName
        {
            get { return _NewMovementPointName; }
            set { SetProperty(ref _NewMovementPointName, value); }
        }

        private MovementPointInfoModel _CurrentSelectPointName;
        public MovementPointInfoModel CurrentSelectPointName
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

        private bool _MovementPointSecurityCfgButtonEnable = true;
        public bool MovementPointSecurityCfgButtonEnable
        {
            get { return _MovementPointSecurityCfgButtonEnable; }
            set { SetProperty(ref _MovementPointSecurityCfgButtonEnable, value); }
        }
        #endregion

        public ObservableCollection<MovementPointInfoModel> MovementPointCollection { get; set; } = new ObservableCollection<MovementPointInfoModel>();
        public ObservableCollection<string> MovementPointGroupCollection { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<MessageItem> MessageList { get; set; } = new ObservableCollection<MessageItem>();
        public ObservableCollection<IOInputInfoModel> IOInputCollection { get; set; } = new ObservableCollection<IOInputInfoModel>();

        public MCMovementPointConfigViewModel(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
            this.dialogHostService = containerProvider.Resolve<IDialogHostService>();
            this.dialogService = containerProvider.Resolve<IDialogService>();
            this.eventAggregator = containerProvider.Resolve<IEventAggregator>();
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
            this.mapper = containerProvider.Resolve<IMapper>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.motionResourceInit = containerProvider.Resolve<IMotionResourceInit>();
            this.snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();
            this.permissionService = containerProvider.Resolve<IPermissionService>();
            this.motionCardService = containerProvider.Resolve<IMotionCardService>();
        }

        private DelegateCommand _AddNewMovementPointCommand;
        public DelegateCommand AddNewMovementPointCommand =>
            _AddNewMovementPointCommand ?? (_AddNewMovementPointCommand = new DelegateCommand(ExecuteAddNewMovementPointCommand));

        void ExecuteAddNewMovementPointCommand()
        {
            IsRightDrawerOpen = true;
        }

        //添加新点位
        private DelegateCommand _AddNewMovementPointCfgCommand;
        public DelegateCommand AddNewMovementPointCfgCommand =>
            _AddNewMovementPointCfgCommand ?? (_AddNewMovementPointCfgCommand = new DelegateCommand(ExecuteAddNewMovementPointCfgCommand).ObservesCanExecute(() => ButtonEnable));

        void ExecuteAddNewMovementPointCfgCommand()
        {
            if (string.IsNullOrWhiteSpace(NewMovementPointName.Name))
            {
                snackbarMessageQueue.EnqueueEx("请输入名称");
                return;
            }
            if (Regex.IsMatch(NewMovementPointName.Name, "^\\d"))
            {
                snackbarMessageQueue.EnqueueEx("名称不合法,名称不能以数字开头");
                return;
            }
            if (dataBaseService.Db.Queryable<MovementPointInfoEntity>().Where(x => x.Name == NewMovementPointName.Name && x.ProductInfo.Select == true).Any())
            {
                snackbarMessageQueue.EnqueueEx("名称重复");
                return;
            }
            var product = dataBaseService.Db.Queryable<ProductInfoEntity>().Where(x => x.Select == true).First();
            NewMovementPointName.ProductId = product.Id;
            var axesList = dataBaseService.Db.Queryable<AxisInfoEntity>().ToList();
            axesList.ForEach(axis => { NewMovementPointName.MovementPointPositionsCollection.Add(new MovementPointPositionModel() { AxisInfoId = axis.Id, AxisInfo = mapper.Map<AxisInfoModel>(axis) }); ; });
            dataBaseService.Db.InsertNav(mapper.Map<MovementPointInfoEntity>(NewMovementPointName)).Include(x => x.MovementPointPositions).Include(x => x.MovementPointSecurities).ExecuteCommand();
            GetMovementPoints();
            GetMovementPointGroup();
            logService.WriteLog(LogTypes.DB.ToString(), $"用户{permissionService.CurrentUserName}创建点位{NewMovementPointName.Name}成功.\r\n{JsonConvert.SerializeObject(NewMovementPointName)}", MessageDegree.INFO);
            NewMovementPointName = new MovementPointInfoModel();
            snackbarMessageQueue.EnqueueEx("添加成功");
        }

        //保存当前选中项
        private DelegateCommand _SaveSelectedMovementPointCommand;
        public DelegateCommand SaveSelectedMovementPointCommand =>
            _SaveSelectedMovementPointCommand ?? (_SaveSelectedMovementPointCommand = new DelegateCommand(ExecuteSaveSelectedMovementPointCommand).ObservesCanExecute(() => ButtonEnable));

        void ExecuteSaveSelectedMovementPointCommand()
        {
            if (GlobalValues.MachineStatus == FSMStateCode.PowerUpping || GlobalValues.MachineStatus == FSMStateCode.EmergencyStopping)
            {
                var result = dialogService.QuestionShowDialog("警告", "设备可能未复位,位置数据不准确,请确认是否继续保存.", "取消", "确定");
                if (result != ButtonResult.Yes) return;
            }
            try
            {
                ButtonEnable = false;
                if (CurrentSelectPointName == null)
                {
                    logService.WriteLog(LogTypes.DB.ToString(), $@"请选中需要保存的点位", MessageDegree.WARN);
                    return;
                }
                if (dataBaseService.Db.Queryable<MovementPointInfoEntity>().Where(x => x.Name == CurrentSelectPointName.Name && x.Id != CurrentSelectPointName.Id && x.ProductInfo.Select == true).Any())
                {
                    snackbarMessageQueue.EnqueueEx("名称重复");
                    return;
                }
                if (Regex.IsMatch(CurrentSelectPointName.Name, "^\\d"))
                {
                    snackbarMessageQueue.EnqueueEx("名称不合法,名称不能以数字开头");
                    return;
                }
                CurrentSelectPointName.MovementPointPositionsCollection.ToList().ForEach(x => { if (x.JogIOInputInfo != null) x.JogIOInputId = x.JogIOInputInfo.Id; });
                var org = dataBaseService.Db.Queryable<MovementPointInfoEntity>()
                    .Includes(x => x.MovementPointPositions.OrderBy(m => m.AxisInfo.Number).OrderBy(m => m.AxisInfo.Id).ToList(), y => y.AxisInfo)
                    .Includes(x => x.MovementPointSecurities.OrderBy(m => m.Sequence).ToList())
                    .Includes(x => x.MovementPointPositions.OrderBy(m => m.AxisInfo.Number).OrderBy(m => m.AxisInfo.Id).ToList(), y => y.JogIOInputInfo)
                    .Where(x => x.ProductInfo.Select == true && x.Id == CurrentSelectPointName.Id)
                    .First();
                MovementPointCompare(org, CurrentSelectPointName);
                dataBaseService.Db.UpdateNav(mapper.Map<MovementPointInfoEntity>(CurrentSelectPointName)).Include(x => x.MovementPointPositions).Include(x => x.MovementPointSecurities).ExecuteCommand();
                UpdateGlobalMCDic();
                logService.WriteLog(LogTypes.DB.ToString(), $"用户{permissionService.CurrentUserName}保存点位信息成功.", MessageDegree.INFO);
                GetMovementPointGroup();
                snackbarMessageQueue.EnqueueEx("保存成功");
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"保存当前选中点位异常{ex.Message}", ex);
            }
            finally
            {
                ButtonEnable = true;
            }
        }

        //删除
        private DelegateCommand _DeleteSelectedMovementPointCommand;
        public DelegateCommand DeleteSelectedMovementPointCommand =>
            _DeleteSelectedMovementPointCommand ?? (_DeleteSelectedMovementPointCommand = new DelegateCommand(ExecuteDeleteSelectedMovementPointCommand).ObservesCanExecute(() => ButtonEnable));

        async void ExecuteDeleteSelectedMovementPointCommand()
        {
            if (CurrentSelectPointName == null)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"请选中要删除的点位", MessageDegree.WARN);
                return;
            }
            var result = await dialogHostService.ShowHostDialog("DialogHostMessageView", new DialogParameters() { { "Title", "警告" }, { "Content", $"确认删除点位{CurrentSelectPointName.Name}?" }, { "CancelInfo", "取消" }, { "SaveInfo", "确定" } });
            if (result.Result != ButtonResult.OK)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}取消删除点位", MessageDegree.INFO);
                return;
            }
            dataBaseService.Db.DeleteNav(mapper.Map<MovementPointInfoEntity>(CurrentSelectPointName)).Include(x => x.MovementPointPositions).Include(x => x.MovementPointSecurities).ExecuteCommand();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}删除点位{CurrentSelectPointName.Name}成功", MessageDegree.INFO);
            GetMovementPoints();
            GetMovementPointGroup();
            UpdateGlobalMCDic();
            NewMovementPointName = new MovementPointInfoModel();
            CurrentSelectPointName = null;
            snackbarMessageQueue.EnqueueEx("删除点位成功");
        }
        //保存所有点位
        private DelegateCommand _SaveAllMovementPointCommand;
        public DelegateCommand SaveAllMovementPointCommand =>
            _SaveAllMovementPointCommand ?? (_SaveAllMovementPointCommand = new DelegateCommand(ExecuteSaveAllMovementPointCommand).ObservesCanExecute(() => ButtonEnable));

        void ExecuteSaveAllMovementPointCommand()
        {
            if (GlobalValues.MachineStatus == FSMStateCode.PowerUpping || GlobalValues.MachineStatus == FSMStateCode.EmergencyStopping)
            {
                var result = dialogService.QuestionShowDialog("警告", "设备可能未复位,位置数据不准确,请确认是否继续保存.", "取消", "确定");
                if (result != ButtonResult.Yes) return;
            }
            try
            {
                ButtonEnable = false;
                bool repeatName = MovementPointCollection.GroupBy(x => x.Name).Where(x => x.Count() > 1).Count() > 0;
                if (repeatName)
                {
                    logService.WriteLog(LogTypes.DB.ToString(), $@"名字重复,无法保存", MessageDegree.ERROR);
                    snackbarMessageQueue.EnqueueEx("保存失败");
                    return;
                }
                foreach (var item in MovementPointCollection)
                {
                    if (Regex.IsMatch(item.Name, "^\\d"))
                    {
                        snackbarMessageQueue.EnqueueEx("名称不合法,名称不能以数字开头");
                        return;
                    }
                }
                MovementPointCollection.ToList().ForEach(x =>
                {
                    x.MovementPointPositionsCollection.ToList().ForEach(y =>
                    {
                        if (y.JogIOInputInfo != null)
                            y.JogIOInputId = y.JogIOInputInfo.Id;
                    });
                });
                dataBaseService.Db.Queryable<MovementPointInfoEntity>()
                    .Includes(x => x.MovementPointPositions.OrderBy(m => m.AxisInfo.Number).OrderBy(m => m.AxisInfo.Id).ToList(), y => y.AxisInfo)
                    .Includes(x => x.MovementPointSecurities.OrderBy(m => m.Sequence).ToList())
                    .Includes(x => x.MovementPointPositions.OrderBy(m => m.AxisInfo.Number).OrderBy(m => m.AxisInfo.Id).ToList(), y => y.JogIOInputInfo)
                    .Where(x => x.ProductInfo.Select == true)
                    .ToList()
                    .ForEach(orgItem =>
                        {
                            var updateItem = MovementPointCollection.Where(x => x.Id == orgItem.Id).FirstOrDefault();
                            if (updateItem != null) MovementPointCompare(orgItem, updateItem);
                        });

                dataBaseService.Db.UpdateNav(mapper.Map<List<MovementPointInfoEntity>>(MovementPointCollection)).Include(x => x.MovementPointPositions).Include(x => x.MovementPointSecurities).ExecuteCommand();
                UpdateGlobalMCDic();
                logService.WriteLog(LogTypes.DB.ToString(), $@"保存成功", MessageDegree.INFO);
                GetMovementPointGroup();
                snackbarMessageQueue.EnqueueEx("保存成功");
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"保存所有点位异常:{ex.Message}", ex);
            }
            finally
            {
                ButtonEnable = true;
            }
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
                .Includes(x => x.MovementPointSecurities.OrderBy(m => m.Sequence).ToList())
                .Where(x => x.ProductInfo.Select == true && x.Group == parameter)
                .ToList();
            MovementPointCollection.Clear();
            mapper.Map(mPoints, MovementPointCollection);
            MovementPointCollection.ToList().ForEach(x =>
            {
                x.MovementPointPositionsCollection.ToList().ForEach(y =>
                {
                    if (y.JogIOInputId != 0)
                        y.JogIOInputInfo = IOInputCollection.Where(z => z.Id == y.JogIOInputId).FirstOrDefault();
                });
            });
            snackbarMessageQueue.EnqueueEx("点位筛选成功");
        }

        //显示所有点位
        private DelegateCommand _ShowAllMovementPointCommand;
        public DelegateCommand ShowAllMovementPointCommand =>
            _ShowAllMovementPointCommand ?? (_ShowAllMovementPointCommand = new DelegateCommand(ExecuteShowAllMovementPointCommand).ObservesCanExecute(() => ButtonEnable));

        void ExecuteShowAllMovementPointCommand()
        {
            GetMovementPoints();
            NewMovementPointName = new MovementPointInfoModel();
            MovementPointGroupCollection.Clear();
            GetMovementPointGroup();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}显示所有点位成功", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("显示所有点位成功");
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
        //更细记录Jog速度
        private DelegateCommand<AxisInfoModel> _UpdateJogVelCommand;
        public DelegateCommand<AxisInfoModel> UpdateJogVelCommand =>
            _UpdateJogVelCommand ?? (_UpdateJogVelCommand = new DelegateCommand<AxisInfoModel>(ExecuteUpdateJogVelCommand));

        void ExecuteUpdateJogVelCommand(AxisInfoModel parameter)
        {
            if (dataBaseService.Db.Queryable<AxisInfoEntity>().Where(x => x.Id == parameter.Id).First().JogVel == parameter.JogVel)
                return;
            foreach (var point in MovementPointCollection)
            {
                foreach (var item in point.MovementPointPositionsCollection)
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
        private DelegateCommand<AxisInfoModel> _JogNegativeCommand;
        public DelegateCommand<AxisInfoModel> JogNegativeCommand =>
            _JogNegativeCommand ?? (_JogNegativeCommand = new DelegateCommand<AxisInfoModel>(ExecuteJogNegativeCommand));

        void ExecuteJogNegativeCommand(AxisInfoModel parameter)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}对轴{parameter.Name}负向点动", MessageDegree.INFO);
            //Jog加减速使用回原加速度
            motionCardService.JogMoveStart((short)parameter.AxisIdOnCard, parameter.JogVel * parameter.Proportion, Direction.Negative, parameter.HomeAcc * parameter.Proportion, parameter.HomeAcc * parameter.Proportion);
        }

        private DelegateCommand<AxisInfoModel> _JogPositiveCommand;
        public DelegateCommand<AxisInfoModel> JogPositiveCommand =>
            _JogPositiveCommand ?? (_JogPositiveCommand = new DelegateCommand<AxisInfoModel>(ExecuteJogPositiveCommand));

        void ExecuteJogPositiveCommand(AxisInfoModel parameter)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}对轴{parameter.Name}正向点动", MessageDegree.INFO);
            //Jog加减速使用回原加速度
            motionCardService.JogMoveStart((short)parameter.AxisIdOnCard, parameter.JogVel * parameter.Proportion, Direction.Positive, parameter.HomeAcc * parameter.Proportion, parameter.HomeAcc * parameter.Proportion);
        }
        //Jog停止
        private DelegateCommand<AxisInfoModel> _JogMoveStopCommand;
        public DelegateCommand<AxisInfoModel> JogMoveStopCommand =>
            _JogMoveStopCommand ?? (_JogMoveStopCommand = new DelegateCommand<AxisInfoModel>(ExecuteJogMoveStopCommand));

        void ExecuteJogMoveStopCommand(AxisInfoModel parameter)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}停止点动", MessageDegree.INFO);
            motionCardService.JogMoveStop((short)parameter.AxisIdOnCard);
        }
        //上使能
        private DelegateCommand<AxisInfoModel> _AxisServoOnCommand;
        public DelegateCommand<AxisInfoModel> AxisServoOnCommand =>
            _AxisServoOnCommand ?? (_AxisServoOnCommand = new DelegateCommand<AxisInfoModel>(ExecuteAxisServoOnCommand).ObservesCanExecute(() => ButtonEnable));

        void ExecuteAxisServoOnCommand(AxisInfoModel parameter)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}对轴{parameter.Name}上使能", MessageDegree.INFO);
            motionCardService.AxisServo((short)parameter.AxisIdOnCard, true);
        }
        //清除轴错误
        private DelegateCommand<AxisInfoModel> _AxisClearErrorCommand;
        public DelegateCommand<AxisInfoModel> AxisClearErrorCommand =>
            _AxisClearErrorCommand ?? (_AxisClearErrorCommand = new DelegateCommand<AxisInfoModel>(ExecuteAxisClearErrorCommand).ObservesCanExecute(() => ButtonEnable));

        void ExecuteAxisClearErrorCommand(AxisInfoModel parameter)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}对轴{parameter.Name}清除错误", MessageDegree.INFO);
            motionCardService.ClearAxSts((short)parameter.AxisIdOnCard);
        }
        //下使能
        private DelegateCommand<AxisInfoModel> _AxisServoOffCommand;
        public DelegateCommand<AxisInfoModel> AxisServoOffCommand =>
            _AxisServoOffCommand ?? (_AxisServoOffCommand = new DelegateCommand<AxisInfoModel>(ExecuteAxisServoOffCommand).ObservesCanExecute(() => ButtonEnable));

        void ExecuteAxisServoOffCommand(AxisInfoModel parameter)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}对轴{parameter.Name}下使能", MessageDegree.INFO);
            motionCardService.AxisServo((short)parameter.AxisIdOnCard, false);
        }
        private DelegateCommand<AxisInfoModel> _RelMoveCommand;
        public DelegateCommand<AxisInfoModel> RelMoveCommand =>
            _RelMoveCommand ?? (_RelMoveCommand = new DelegateCommand<AxisInfoModel>(ExecuteRelMoveCommand).ObservesCanExecute(() => ButtonEnable));

        async void ExecuteRelMoveCommand(AxisInfoModel parameter)
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

        private DelegateCommand<AxisInfoModel> _RelMoveStopCommand;
        public DelegateCommand<AxisInfoModel> RelMoveStopCommand =>
            _RelMoveStopCommand ?? (_RelMoveStopCommand = new DelegateCommand<AxisInfoModel>(ExecuteRelMoveStopCommand));

        void ExecuteRelMoveStopCommand(AxisInfoModel parameter)
        {
            relMoveArrivedCancellationToken?.Cancel();
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
            foreach (var item in CurrentSelectPointName.MovementPointPositionsCollection)
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
                //重新初始化全局点位集合
                motionResourceInit.InitMCPointDicCollection();

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

        private DelegateCommand _MovementPointSecurityCfgCommand;
        public DelegateCommand MovementPointSecurityCfgCommand =>
            _MovementPointSecurityCfgCommand ?? (_MovementPointSecurityCfgCommand = new DelegateCommand(ExecuteMovementPointSecurityCfgCommand).ObservesCanExecute(() => MovementPointSecurityCfgButtonEnable));

        void ExecuteMovementPointSecurityCfgCommand()
        {
            if (CurrentSelectPointName == null)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"请选择需要设定的点位", MessageDegree.WARN);
                snackbarMessageQueue.EnqueueEx("请选择需要设定的点位");
                return;
            }
            MovementPointSecurityCfgButtonEnable = false;
            dialogService.ShowDialog(nameof(MCMovementPointSecurityView), new DialogParameters() { { "MovementPointModel", CurrentSelectPointName } }, r =>
            {
                MovementPointSecurityCfgButtonEnable = true;
                logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}离开点位安全配置页面,请保存点位数据", MessageDegree.INFO);
            });
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            //每次进入页面,刷新界面内容
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                GetIOInputCollection();
                MovementPointGroupCollection.Clear();
                GetMovementPointGroup();
                GetMovementPoints();
            }), System.Windows.Threading.DispatcherPriority.Render);

            GlobalVel = GlobalValues.GlobalVelPercentage;
            NewMovementPointName = new MovementPointInfoModel();
            logService.ShowOnUI += LogService_ShowOnUI;
            MessageList.Clear();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}进入点位配置页面", MessageDegree.INFO);
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            relMoveArrivedCancellationToken?.Cancel();
            moveMentPositionArrivedCancellationToken?.Cancel();
            //离开页面,修改全局运动点位集合
            UpdateGlobalMCDic();
            logService.ShowOnUI -= LogService_ShowOnUI;
        }

        #region PrivateMethod
        private void UpdateGlobalMCDic()
        {
            var newPoints = dataBaseService.Db.Queryable<MovementPointInfoEntity>()
                .Includes(x => x.MovementPointPositions.Where(m => m.InvolveAxis == true).OrderBy(n => n.AxisInfo.Number).OrderBy(n => n.AxisInfo.Id).ToList(), y => y.AxisInfo)
                .Includes(x => x.MovementPointPositions.Where(m => m.InvolveAxis == true).OrderBy(n => n.AxisInfo.Number).OrderBy(n => n.AxisInfo.Id).ToList(), y => y.JogIOInputInfo)
                .Includes(x => x.MovementPointSecurities.OrderBy(n => n.Sequence).ToList())
                .Where(x => x.ProductInfo.Select == true)
                .ToList();
            var willRemoveItems = new List<MovementPointInfo>();
            var willAddItems = new List<MovementPointInfo>();
            foreach (var item in GlobalValues.MCPointDicCollection)
            {
                var result = newPoints.Where(x => x.Id == item.Value.GetId()).FirstOrDefault();
                if (result == null)
                {
                    willRemoveItems.Add(item.Value);
                }
                else
                {
                    if (item.Key == result.Name)
                    {
                        if (item.Value.GetCNName() != result.CNName) item.Value.CNName = result.CNName;
                        if (item.Value.GetENName() != result.ENName) item.Value.ENName = result.ENName;
                        if (item.Value.GetVNName() != result.VNName) item.Value.VNName = result.VNName;
                        if (item.Value.GetXXName() != result.XXName) item.Value.XXName = result.XXName;
                        if (item.Value.GetGroup() != result.Group) item.Value.Group = result.Group;
                        if (item.Value.GetBackup() != result.Backup) item.Value.Backup = result.Backup;
                        if (item.Value.GetTag() != result.Tag) item.Value.Tag = result.Tag;
                        //PointPosition List更新
                        var willRemovePositionItems = new List<MovementPointPosition>();
                        var willAddPositionItems = new List<MovementPointPosition>();
                        foreach (var positionItem in item.Value.MovementPointPositions)
                        {
                            var positionResult = result.MovementPointPositions.Where(x => x.AxisInfoId == positionItem.GetAxisInfoId()).FirstOrDefault();
                            if (positionResult == null)
                            {
                                willRemovePositionItems.Add(positionItem);
                            }
                            else
                            {
                                mapper.Map(positionResult, positionItem);
                            }
                        }
                        willRemovePositionItems.ForEach(removePositionItem => item.Value.MovementPointPositions.Remove(removePositionItem));
                        foreach (var positionItem in result.MovementPointPositions)
                        {
                            var positionResult = item.Value.MovementPointPositions.Where(x => x.GetId() == positionItem.Id).FirstOrDefault();
                            if (positionResult == null)
                                willAddPositionItems.Add(mapper.Map<MovementPointPosition>(positionItem));
                        }
                        willAddPositionItems.ForEach(addPositionItem => item.Value.MovementPointPositions.Add(addPositionItem));
                        //排序
                        item.Value.MovementPointPositions = item.Value.MovementPointPositions.OrderBy(x => x.AxisInfo.GetNumber()).OrderBy(x => x.GetId()).ToList();

                        //点位中配置的安全动作更新
                        item.Value.MovementPointSecurities.Clear();
                        mapper.Map(result.MovementPointSecurities, item.Value.MovementPointSecurities);
                        //排序
                        item.Value.MovementPointSecurities = item.Value.MovementPointSecurities.OrderBy(x => x.GetSequence()).ToList();
                    }
                    else
                        willRemoveItems.Add(item.Value);
                }
            }
            willRemoveItems.ForEach(removeItem => GlobalValues.MCPointDicCollection.Remove(removeItem.Name));
            foreach (var item in newPoints)
            {
                var result = GlobalValues.MCPointDicCollection.Values.ToList().Where(x => x.GetId() == item.Id).FirstOrDefault();
                if (result == null)
                    willAddItems.Add(mapper.Map<MovementPointInfo>(item));
            }
            willAddItems.ForEach(addItem => GlobalValues.MCPointDicCollection.Add(addItem.Name, addItem));
        }

        private void GetMovementPoints()
        {
            MovementPointCollection.Clear();
            var mPoints = dataBaseService.Db.Queryable<MovementPointInfoEntity>()
                .Includes(x => x.MovementPointPositions.OrderBy(m => m.AxisInfo.Number).OrderBy(m => m.Id).ToList(), y => y.AxisInfo)
                .Includes(x => x.MovementPointSecurities.OrderBy(m => m.Sequence).ToList())
                .Where(x => x.ProductInfo.Select == true)
                .ToList();
            mapper.Map(mPoints, MovementPointCollection);
            MovementPointCollection.ToList().ForEach(x =>
            {
                x.MovementPointPositionsCollection.ToList().ForEach(y =>
                {
                    if (y.JogIOInputId != 0)
                        y.JogIOInputInfo = IOInputCollection.Where(z => z.Id == y.JogIOInputId).FirstOrDefault();
                });
            });
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

        private void GetIOInputCollection()
        {
            IOInputCollection.Clear();
            var ioInputs = dataBaseService.Db.Queryable<IOInputInfoEntity>().OrderBy(x => x.Number).OrderBy(x => x.Id).ToList();
            mapper.Map(ioInputs, IOInputCollection);
        }

        private void MovementPointCompare(MovementPointInfoEntity orgItem, MovementPointInfoModel updateItem)
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
            if (orgItem.MovementPointSecurities.Count != updateItem.MovementPointSecuritiesCollection.Count) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} MovementPointSecurities: {orgItem.MovementPointSecurities.Count}=>{updateItem.MovementPointSecuritiesCollection.Count}", MessageDegree.INFO);

            foreach (var orgMP in orgItem.MovementPointPositions)
            {
                foreach (var updateMP in updateItem.MovementPointPositionsCollection)
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
        #endregion
    }
}
