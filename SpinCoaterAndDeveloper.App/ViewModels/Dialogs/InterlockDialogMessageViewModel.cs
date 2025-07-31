using LogServiceInterface;
using MotionCardServiceInterface;
using MotionControlActuation;
using PermissionServiceInterface;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.Extensions;
using SpinCoaterAndDeveloper.Shared.Services.MachineInterlockService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class InterlockDialogMessageViewModel : BindableBase, IDialogAware
    {
        public event Action<IDialogResult> RequestClose;

        private Dictionary<string, bool> differentOutput;
        private Dictionary<string, double> differentAxis;
        private Guid guid;
        private CancellationTokenSource cancellationTokenSource;

        private readonly IMachineInterlock machineInterlock;
        private readonly ILogService logService;
        private readonly IPermissionService permissionService;
        private readonly IMotionCardService motionCardService;
        private readonly IContainerProvider containerProvider;

        private string title;
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }
        private string content;
        public string Content
        {
            get { return content; }
            set { SetProperty(ref content, value); }
        }
        private string _ButtonIgnoreInfo = "强制启动".TryFindResourceEx();
        public string ButtonIgnoreInfo
        {
            get { return _ButtonIgnoreInfo; }
            set { SetProperty(ref _ButtonIgnoreInfo, value); }
        }
        private string _ButtonYesInfo = "确定".TryFindResourceEx();
        public string ButtonYesInfo
        {
            get { return _ButtonYesInfo; }
            set { SetProperty(ref _ButtonYesInfo, value); }
        }
        private bool _ButtonEnable = true;
        public bool ButtonEnable
        {
            get { return _ButtonEnable; }
            set { SetProperty(ref _ButtonEnable, value); }
        }

        public ObservableCollection<InterlockOutput> DifferentOutputsCollection { get; set; } = new ObservableCollection<InterlockOutput>();
        public ObservableCollection<InterlockAxis> DifferentAxesCollection { get; set; } = new ObservableCollection<InterlockAxis>();
        public InterlockDialogMessageViewModel(IContainerProvider containerProvider)
        {
            this.logService = containerProvider.Resolve<ILogService>();
            this.machineInterlock = containerProvider.Resolve<IMachineInterlock>();
            this.permissionService = containerProvider.Resolve<IPermissionService>();
            this.motionCardService = containerProvider.Resolve<IMotionCardService>();
            this.containerProvider = containerProvider;
        }

        private DelegateCommand<string> _YesCommand;
        public DelegateCommand<string> YesCommand =>
            _YesCommand ?? (_YesCommand = new DelegateCommand<string>(ExecuteYesCommand));

        void ExecuteYesCommand(string parameter)
        {
            if (parameter != null && parameter == "Auto")
                logService.WriteLog(LogTypes.DB.ToString(), $@"程序自动关闭窗口", MessageDegree.INFO);
            else
                logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}关闭窗口", MessageDegree.INFO);

            RequestClose?.Invoke(new DialogResult(ButtonResult.Yes));
        }

        private DelegateCommand _IgnoreCommand;
        public DelegateCommand IgnoreCommand =>
            _IgnoreCommand ?? (_IgnoreCommand = new DelegateCommand(ExecuteIgnoreCommand));

        void ExecuteIgnoreCommand()
        {
            machineInterlock.InterlockOneCycleForceColse(guid);
            RequestClose?.Invoke(new DialogResult(ButtonResult.Ignore));
        }

        private DelegateCommand<InterlockOutput> _InterlockOutputCommand;
        public DelegateCommand<InterlockOutput> InterlockOutputCommand =>
            _InterlockOutputCommand ?? (_InterlockOutputCommand = new DelegateCommand<InterlockOutput>(ExecuteInterlockOutputCommand));

        void ExecuteInterlockOutputCommand(InterlockOutput parameter)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"联锁弹窗,用户{permissionService.CurrentUserName}设定输出点位:{parameter.Name}状态为:{parameter.Status}", MessageDegree.INFO);
            motionCardService.SetOuputStsEx(parameter.Name, parameter.Status);
        }

        private DelegateCommand<InterlockAxis> _InterlockAxisCommand;
        public DelegateCommand<InterlockAxis> InterlockAxisCommand =>
            _InterlockAxisCommand ?? (_InterlockAxisCommand = new DelegateCommand<InterlockAxis>(ExecuteInterlockAxisCommand).ObservesCanExecute(() => ButtonEnable));

        async void ExecuteInterlockAxisCommand(InterlockAxis parameter)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"联锁弹窗,用户{permissionService.CurrentUserName}设定轴:{parameter.Name}位置:{parameter.Position}", MessageDegree.INFO);
            try
            {
                ButtonEnable = false;
                cancellationTokenSource = new CancellationTokenSource();
                SingleAxisAbsMoveAndWaitArrived singleAxisAbsMoveAndWaitArrived = new SingleAxisAbsMoveAndWaitArrived(containerProvider);
                await singleAxisAbsMoveAndWaitArrived.StartAbsAsync(parameter.Name,
                                                                    MotionControlResource.AxisResource[parameter.Name].GetHomeHighVel(),
                                                                    MotionControlResource.AxisResource[parameter.Name].GetHomeAcc(),
                                                                    MotionControlResource.AxisResource[parameter.Name].GetHomeAcc(),
                                                                    parameter.Position,
                                                                    MotionControlResource.AxisResource[parameter.Name].GetHomeTimeout(),
                                                                    cancellationTokenSource);
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"联锁弹窗,轴回联锁记录位置异常:{ex.Message}", ex);
            }
            finally
            {
                ButtonEnable = true;
            }
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            cancellationTokenSource?.Cancel();
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            Content = parameters.GetValue<string>("Content");
            Title = parameters.GetValue<string>("Title");
            //获取不一样的输出点
            differentOutput = parameters.GetValue<Dictionary<string, bool>>("DifferentOutput");
            foreach (var item in differentOutput)
            {
                InterlockOutput output = new InterlockOutput() { Name = item.Key, Status = item.Value };
                switch (System.Threading.Thread.CurrentThread.CurrentUICulture.Name)
                {
                    case "zh-CN":
                        output.ShowOnUIName = MotionControlResource.IOOutputResource[item.Key].GetCNName();
                        break;
                    case "en-US":
                        output.ShowOnUIName = MotionControlResource.IOOutputResource[item.Key].GetENName();
                        break;
                    case "vi-VN":
                        output.ShowOnUIName = MotionControlResource.IOOutputResource[item.Key].GetVNName();
                        break;
                    default:
                        output.ShowOnUIName = item.Key;
                        break;
                }
                output.ShowOnUIName = string.IsNullOrWhiteSpace(output.ShowOnUIName) ? item.Key : output.ShowOnUIName;
                DifferentOutputsCollection.Add(output);
            }
            //获取不一样的轴位置
            differentAxis = parameters.GetValue<Dictionary<string, double>>("DifferentAxis");
            foreach (var item in differentAxis)
            {
                InterlockAxis axis = new InterlockAxis() { Name = item.Key, Position = item.Value };
                switch (System.Threading.Thread.CurrentThread.CurrentUICulture.Name)
                {
                    case "zh-CN":
                        axis.ShowOnUIName = MotionControlResource.AxisResource[item.Key].GetCNName();
                        break;
                    case "en-US":
                        axis.ShowOnUIName = MotionControlResource.AxisResource[item.Key].GetENName();
                        break;
                    case "vi-VN":
                        axis.ShowOnUIName = MotionControlResource.AxisResource[item.Key].GetVNName();
                        break;
                    default:
                        axis.ShowOnUIName = item.Key;
                        break;
                }
                axis.ShowOnUIName = string.IsNullOrWhiteSpace(axis.ShowOnUIName) ? item.Key : axis.ShowOnUIName;
                DifferentAxesCollection.Add(axis);
            }
            guid = parameters.GetValue<Guid>("Guid");
        }
    }

    public class InterlockOutput : BindableBase
    {
        private string _Name;
        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value); }
        }
        private string _ShowOnUIName;
        public string ShowOnUIName
        {
            get { return _ShowOnUIName; }
            set { SetProperty(ref _ShowOnUIName, value); }
        }
        private bool _Status;
        public bool Status
        {
            get { return _Status; }
            set { SetProperty(ref _Status, value); }
        }
    }
    public class InterlockAxis : BindableBase
    {
        private string _Name;
        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value); }
        }
        private string _ShowOnUIName;
        public string ShowOnUIName
        {
            get { return _ShowOnUIName; }
            set { SetProperty(ref _ShowOnUIName, value); }
        }
        private double _Position;
        public double Position
        {
            get { return _Position; }
            set { SetProperty(ref _Position, value); }
        }
    }
}
