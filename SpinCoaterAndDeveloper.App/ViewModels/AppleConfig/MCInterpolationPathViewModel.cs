using AutoMapper;
using DataBaseServiceInterface;
using LogServiceInterface;
using MaterialDesignThemes.Wpf;
using MotionCardServiceInterface;
using MotionControlActuation;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using PrismAutomationPlatformExt;
using SpinCoaterAndDeveloper.App.Extensions;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using SpinCoaterAndDeveloper.Shared.Extensions;
using SpinCoaterAndDeveloper.Shared.Models.InterpolationModels;
using SpinCoaterAndDeveloper.Shared.Services.MotionResourceInitService;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class MCInterpolationPathViewModel : BindableBase, INavigationAware
    {
        private readonly IDialogHostService dialogHostService;
        private readonly ILogService logService;
        private readonly ISnackbarMessageQueue snackbarMessageQueue;
        private readonly IDataBaseService dataBaseService;
        private readonly IMapper mapper;
        private readonly IMotionResourceInit motionResourceInit;

        private bool buttonEnabel = true;

        public bool ButtonEnable
        {
            get { return buttonEnabel; }
            set { SetProperty(ref buttonEnabel, value); }
        }
        //插补路径集合
        public ObservableCollection<InterpolationPathCoordinateModel> InterpolationPathCoordinateCollection { get; set; } = new ObservableCollection<InterpolationPathCoordinateModel>();
        //轴集合
        public ObservableCollection<AxisInfoEntity> AxisCollection { get; set; } = new ObservableCollection<AxisInfoEntity>();
        //坐标系号
        public ObservableCollection<int> CoordinateNums { get; set; } = new ObservableCollection<int>() { 0, 1, 2, 3 };
        public MCInterpolationPathViewModel(IContainerProvider containerProvider)
        {
            this.dialogHostService = containerProvider.Resolve<IDialogHostService>();
            this.snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
            this.mapper = containerProvider.Resolve<IMapper>();
            this.motionResourceInit = containerProvider.Resolve<IMotionResourceInit>();
        }
        private DelegateCommand addNewInterpolationCommand;
        public DelegateCommand AddNewInterpolationCommand =>
            addNewInterpolationCommand ?? (addNewInterpolationCommand = new DelegateCommand(ExecuteAddNewInterpolationCommand).ObservesCanExecute(() => ButtonEnable));
        //添加新插补路径
        async void ExecuteAddNewInterpolationCommand()
        {
            try
            {
                ButtonEnable = false;
                var result = await dialogHostService.ShowHostDialog("AddInterpolationPathView", null);
                if (result.Result == Prism.Services.Dialogs.ButtonResult.OK)
                {
                    if (Regex.IsMatch(result.Parameters.GetValue<string>("Name"), "^\\d"))
                    {
                        snackbarMessageQueue.EnqueueEx("名称不合法,名称不能以数字开头");
                        return;
                    }

                    foreach (var item in InterpolationPathCoordinateCollection)
                    {
                        if (item.PathName == result.Parameters.GetValue<string>("Name"))
                        {
                            snackbarMessageQueue.EnqueueEx("名字重复,添加失败");
                            return;
                        }
                    }
                    var productId = dataBaseService.Db.Queryable<ProductInfoEntity>().First(it => it.Select == true);
                    var model = new InterpolationPathCoordinateModel() { PathName = result.Parameters.GetValue<string>("Name"), ProductId = productId.Id };
                    dataBaseService.Db.Insertable(mapper.Map<InterpolationPathCoordinateEntity>(model)).ExecuteCommand();
                    InitInterpolationPath();
                    snackbarMessageQueue.EnqueueEx("添加成功");
                }
                else
                {
                    snackbarMessageQueue.EnqueueEx("添加取消");
                }
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"添加插补路径异常{ex.Message}", ex);
            }
            finally
            {
                ButtonEnable = true;
            }
        }

        private DelegateCommand<InterpolationPathCoordinateModel> deleteInterpolationPathCommand;
        public DelegateCommand<InterpolationPathCoordinateModel> DeleteInterpolationPathCommand =>
            deleteInterpolationPathCommand ?? (deleteInterpolationPathCommand = new DelegateCommand<InterpolationPathCoordinateModel>(ExecuteDeleteInterpolationPathCommand));
        //删除插补路径
        async void ExecuteDeleteInterpolationPathCommand(InterpolationPathCoordinateModel parameter)
        {
            try
            {
                ButtonEnable = false;
                var result = await dialogHostService.HostQuestion("提示", $"{"确认删除".TryFindResourceEx()} {"InterpolationPath".TryFindResourceEx()} {parameter.PathName}?", "取消", "确认");
                if (result.Result == ButtonResult.OK)
                {
                    dataBaseService.Db.DeleteNav(mapper.Map<InterpolationPathCoordinateEntity>(parameter)).Include(z => z.InterpolationPaths).ExecuteCommand();
                    InterpolationPathCoordinateCollection.Remove(parameter);
                    motionResourceInit.InitInterpolationPaths();
                    snackbarMessageQueue.EnqueueEx("删除成功");
                }
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"删除插补路径异常{ex.Message}", ex);
            }
            finally
            {
                ButtonEnable = true;
            }
        }

        private DelegateCommand<InterpolationPathCoordinateModel> saveInterpolationPathCoordinateCommand;
        public DelegateCommand<InterpolationPathCoordinateModel> SaveInterpolationPathCoordinateCommand =>
            saveInterpolationPathCoordinateCommand ?? (saveInterpolationPathCoordinateCommand = new DelegateCommand<InterpolationPathCoordinateModel>(ExecuteSaveInterpolationPathCoordinateCommand).ObservesCanExecute(() => ButtonEnable));
        //保存插补坐标系设置
        void ExecuteSaveInterpolationPathCoordinateCommand(InterpolationPathCoordinateModel parameter)
        {
            try
            {
                ButtonEnable = false;
                var data = mapper.Map<InterpolationPathCoordinateEntity>(parameter);
                //有则更新,无则插入 new UpdateNavRootOptions() { IsInsertRoot = true }
                var result = dataBaseService.Db.UpdateNav(data, new UpdateNavRootOptions() { IsInsertRoot = true })
                    .Include(z1 => z1.AxisX)
                    .Include(z2 => z2.AxisY)
                    .Include(z3 => z3.AxisZ)
                    .Include(z4 => z4.AxisR)
                    .Include(z5 => z5.AxisA)
                    .Include(it => it.InterpolationPaths, new UpdateNavOptions() { OneToManyDeleteAll = true })
                    .ExecuteCommand();
                if (result)
                {
                    motionResourceInit.InitInterpolationPaths();
                    snackbarMessageQueue.EnqueueEx("保存成功!");
                }
                else
                {
                    snackbarMessageQueue.EnqueueEx("保存失败!");
                }
            }
            catch (Exception ex)
            {
                snackbarMessageQueue.EnqueueEx("保存失败!出现异常,请查看日志.");
                logService.WriteLog(LogTypes.DB.ToString(), $"保存插补坐标系异常:{ex.Message}", ex);
            }
            finally
            {
                ButtonEnable = true;
            }
        }
        private DelegateCommand<InterpolationPathCoordinateModel> addInterpolationPathCommand;
        public DelegateCommand<InterpolationPathCoordinateModel> AddInterpolationPathCommand =>
            addInterpolationPathCommand ?? (addInterpolationPathCommand = new DelegateCommand<InterpolationPathCoordinateModel>(ExecuteAddInterpolationPathCommand));
        //添加新的插补数据
        void ExecuteAddInterpolationPathCommand(InterpolationPathCoordinateModel parameter)
        {
            parameter.InterpolationPaths.Add(new InterpolationPathEditModel()
            {
                Sequence = parameter.InterpolationPaths.Count + 1,
                PathMode = InterpolationPathMode.Line,
                MX = 0,
                MY = 0,
                MZ = 0,
                TX = 0,
                TY = 0,
                TZ = 0,
                TR = 0,
                TA = 0,
                Speed = 0,
                AccSpeed = 0,
                IOEnable = false,
                StartDelayIOEnable = false,
                StartDelayTime = 0,
                EndDelayIOEnable = false,
                EndDelayTime = 0,
            });
        }
        private DelegateCommand<object[]> insertInterpolationPathCommand;
        public DelegateCommand<object[]> InsertInterpolationPathCommand =>
            insertInterpolationPathCommand ?? (insertInterpolationPathCommand = new DelegateCommand<object[]>(ExecuteInsertInterpolationPathCommand));
        //插入插补路径数据
        void ExecuteInsertInterpolationPathCommand(object[] parameter)
        {
            if (parameter == null)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"插补路径插入携带参数异常", new Exception("插补路径插入携带参数为Null"));
                snackbarMessageQueue.EnqueueEx("参数异常,添加失败!");
                return;
            }
            if (parameter[1] == null)
            {
                snackbarMessageQueue.EnqueueEx("请选择插入行");
                return;
            }
            var coordinateModel = parameter[0] as InterpolationPathCoordinateModel;
            var pathModel = parameter[1] as InterpolationPathEditModel;
            //后续路径序列号修改
            foreach (var item in coordinateModel.InterpolationPaths)
            {
                if (item.Sequence > pathModel.Sequence)
                {
                    item.Sequence += 1;
                }
            }
            coordinateModel.InterpolationPaths.Insert(pathModel.Sequence, new InterpolationPathEditModel()
            {
                Sequence = pathModel.Sequence + 1,
                PathMode = InterpolationPathMode.Line,
                MX = 0,
                MY = 0,
                MZ = 0,
                TX = 0,
                TY = 0,
                TZ = 0,
                TR = 0,
                TA = 0,
                Speed = 0,
                AccSpeed = 0,
                IOEnable = false,
                StartDelayIOEnable = false,
                StartDelayTime = 0,
                EndDelayIOEnable = false,
                EndDelayTime = 0,
            });
        }
        private DelegateCommand<object[]> deleteInterpolationPathDataCommand;
        public DelegateCommand<object[]> DeleteInterpolationPathDataCommand =>
            deleteInterpolationPathDataCommand ?? (deleteInterpolationPathDataCommand = new DelegateCommand<object[]>(ExecuteDeleteInterpolationPathDataCommand));

        void ExecuteDeleteInterpolationPathDataCommand(object[] parameter)
        {
            if (parameter == null)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"插补路径插入携带参数异常", new Exception("插补路径插入携带参数为Null"));
                snackbarMessageQueue.EnqueueEx("参数异常,添加失败!");
                return;
            }
            if (parameter[1] == null)
            {
                snackbarMessageQueue.EnqueueEx("请选择删除行");
                return;
            }
            var coordinateModel = parameter[0] as InterpolationPathCoordinateModel;
            var pathModel = parameter[1] as InterpolationPathEditModel;

            coordinateModel.InterpolationPaths.Remove(pathModel);
            //后续路径序列号修改
            foreach (var item in coordinateModel.InterpolationPaths)
            {
                if (item.Sequence > pathModel.Sequence)
                {
                    item.Sequence -= 1;
                }
            }
        }

        private DelegateCommand<InterpolationPathCoordinateModel> setBeginningPointCommand;
        public DelegateCommand<InterpolationPathCoordinateModel> SetBeginningPointCommand =>
            setBeginningPointCommand ?? (setBeginningPointCommand = new DelegateCommand<InterpolationPathCoordinateModel>(ExecuteSetBeginningPointCommand));
        /// <summary>
        /// 示教起点坐标
        /// </summary>
        /// <param name="parameter"></param>
        void ExecuteSetBeginningPointCommand(InterpolationPathCoordinateModel parameter)
        {
            try
            {
                parameter.BeginningX = MotionControlResource.AxisResource[parameter.AxisX.Name].EncPos / parameter.AxisX.Proportion;
                parameter.BeginningY = MotionControlResource.AxisResource[parameter.AxisY.Name].EncPos / parameter.AxisY.Proportion;
                if (parameter.EnableAxisZ)
                {
                    parameter.BeginningZ = MotionControlResource.AxisResource[parameter.AxisZ.Name].EncPos / parameter.AxisZ.Proportion;
                }
                if (parameter.EnableAxisR)
                {
                    //需转换成角度
                    parameter.BeginningR = MotionControlResource.AxisResource[parameter.AxisR.Name].EncPos / parameter.AxisR.Proportion;
                }
                if (parameter.EnableAxisA)
                {
                    parameter.BeginningA = MotionControlResource.AxisResource[parameter.AxisA.Name].EncPos / parameter.AxisA.Proportion;
                }
                snackbarMessageQueue.EnqueueEx("示教成功,请保存");
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"示教插补起点异常:{ex.Message}", ex);
                snackbarMessageQueue.EnqueueEx("示教异常");
                return;
            }
        }

        private DelegateCommand<object[]> interpolationArcMidPointTeachCommand;
        public DelegateCommand<object[]> InterpolationArcMidPointTeachCommand =>
            interpolationArcMidPointTeachCommand ?? (interpolationArcMidPointTeachCommand = new DelegateCommand<object[]>(ExecuteInterpolationArcMidPointTeachCommand));
        //圆弧中间点示教
        void ExecuteInterpolationArcMidPointTeachCommand(object[] parameter)
        {
            try
            {
                if (parameter == null)
                {
                    logService.WriteLog(LogTypes.DB.ToString(), $"示教携带参数异常", new Exception("示教携带参数异常"));
                    snackbarMessageQueue.EnqueueEx("示教携带参数异常");
                    return;
                }
                var coordinateModel = parameter[0] as InterpolationPathCoordinateModel;
                var pathModel = parameter[1] as InterpolationPathEditModel;
                pathModel.MX = MotionControlResource.AxisResource[coordinateModel.AxisX.Name].EncPos / coordinateModel.AxisX.Proportion - GlobalValues.InterpolationPaths[coordinateModel.PathName].BeginningX;
                pathModel.MY = MotionControlResource.AxisResource[coordinateModel.AxisY.Name].EncPos / coordinateModel.AxisY.Proportion - GlobalValues.InterpolationPaths[coordinateModel.PathName].BeginningY;
                if (coordinateModel.EnableAxisZ)
                {
                    pathModel.MZ = MotionControlResource.AxisResource[coordinateModel.AxisZ.Name].EncPos / coordinateModel.AxisZ.Proportion - GlobalValues.InterpolationPaths[coordinateModel.PathName].BeginningZ;
                }
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"示教异常", ex);
                snackbarMessageQueue.EnqueueEx("示教失败");
            }
        }

        private DelegateCommand<object[]> interpolationEndPointTeachCommand;
        public DelegateCommand<object[]> InterpolationEndPointTeachCommand =>
            interpolationEndPointTeachCommand ?? (interpolationEndPointTeachCommand = new DelegateCommand<object[]>(ExecuteInterpolationEndPointTeachCommand));
        //终点示教
        void ExecuteInterpolationEndPointTeachCommand(object[] parameter)
        {
            try
            {
                if (parameter == null)
                {
                    logService.WriteLog(LogTypes.DB.ToString(), $"终点示教携带参数异常", new Exception("终点示教携带参数异常"));
                    snackbarMessageQueue.EnqueueEx("终点示教携带参数异常");
                    return;
                }
                var coordinateModel = parameter[0] as InterpolationPathCoordinateModel;
                var pathModel = parameter[1] as InterpolationPathEditModel;
                pathModel.TX = MotionControlResource.AxisResource[coordinateModel.AxisX.Name].EncPos / coordinateModel.AxisX.Proportion - GlobalValues.InterpolationPaths[coordinateModel.PathName].BeginningX;
                pathModel.TY = MotionControlResource.AxisResource[coordinateModel.AxisY.Name].EncPos / coordinateModel.AxisY.Proportion - GlobalValues.InterpolationPaths[coordinateModel.PathName].BeginningY;
                if (coordinateModel.EnableAxisZ)
                {
                    pathModel.TZ = MotionControlResource.AxisResource[coordinateModel.AxisZ.Name].EncPos / coordinateModel.AxisZ.Proportion - GlobalValues.InterpolationPaths[coordinateModel.PathName].BeginningZ;
                }
                if (coordinateModel.EnableAxisR)
                {
                    pathModel.TR = MotionControlResource.AxisResource[coordinateModel.AxisR.Name].EncPos / coordinateModel.AxisR.Proportion - GlobalValues.InterpolationPaths[coordinateModel.PathName].BeginningR;
                }
                if (coordinateModel.EnableAxisA)
                {
                    pathModel.TA = MotionControlResource.AxisResource[coordinateModel.AxisA.Name].EncPos / coordinateModel.AxisA.Proportion - GlobalValues.InterpolationPaths[coordinateModel.PathName].BeginningA;
                }
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $"终点示教异常", ex);
                snackbarMessageQueue.EnqueueEx("示教失败");
            }
        }
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                //获取所有轴
                mapper.Map(dataBaseService.Db.Queryable<AxisInfoEntity>().ToList(), AxisCollection);
                InitInterpolationPath();
            }), System.Windows.Threading.DispatcherPriority.Render);
            snackbarMessageQueue.EnqueueEx("设备运转中,请勿修改参数");
        }
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            //离开页面复位插补路径数据集合
            //motionResourceInit.InitInterpolationPaths();
        }
        private void InitInterpolationPath()
        {
            var interpolationCoordinate = dataBaseService.Db.Queryable<InterpolationPathCoordinateEntity>()
                .Includes(x1 => x1.AxisX)
                .Includes(x2 => x2.AxisY)
                .Includes(x3 => x3.AxisZ)
                .Includes(x4 => x4.AxisR)
                .Includes(x5 => x5.AxisA)
                .Includes(x => x.ProductInfo)
                .Where(x => x.ProductInfo.Select == true)
                .OrderBy(it => it.Id)
                .Includes(p => p.InterpolationPaths.OrderBy(s => s.Sequence).ToList())
                .ToList();
            //清除界面插补路径并从数据库重新查询赋值
            InterpolationPathCoordinateCollection.Clear();
            mapper.Map(interpolationCoordinate, InterpolationPathCoordinateCollection);
        }
    }
}
