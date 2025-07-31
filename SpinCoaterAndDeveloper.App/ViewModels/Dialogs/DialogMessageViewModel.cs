using FSM;
using LogServiceInterface;
using MotionControlActuation;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SpinCoaterAndDeveloper.Actuation.Actuation;
using SpinCoaterAndDeveloper.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SpinCoaterAndDeveloper.App.ViewModels.Dialogs
{
    public class DialogMessageViewModel : BindableBase, IDialogAware
    {
        //是否是流程总弹窗,用于是否监视设备状态切换(如急停等)
        private ActuationManagerAbs actuationManager;
        private CancellationTokenSource cancellationTokenSource;
        private readonly IContainerProvider containerProvider;
        private readonly ILogService logService;
        private Task task = null;
        public DialogMessageViewModel(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
            this.logService = containerProvider.Resolve<ILogService>();
        }

        #region Bingding

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
        private string buttonIgnoreInfo;

        public string ButtonIgnoreInfo
        {
            get { return buttonIgnoreInfo; }
            set { SetProperty(ref buttonIgnoreInfo, value); }
        }
        private string buttonCancelInfo;
        public string ButtonCancelInfo
        {
            get { return buttonCancelInfo; }
            set { SetProperty(ref buttonCancelInfo, value); }
        }
        private string buttonYesInfo;
        public string ButtonYesInfo
        {
            get { return buttonYesInfo; }
            set { SetProperty(ref buttonYesInfo, value); }
        }
        private string buttonRedirectInfo;

        public string ButtonRedirectInfo
        {
            get { return buttonRedirectInfo; }
            set { SetProperty(ref buttonRedirectInfo, value); }
        }

        #endregion

        private DelegateCommand _LoadedCommand;
        public DelegateCommand LoadedCommand =>
            _LoadedCommand ?? (_LoadedCommand = new DelegateCommand(ExecuteLoadedCommand));

        void ExecuteLoadedCommand()
        {
            //如果不是流程弹窗则退出
            if (actuationManager == null) return;
            //进入时判断是否已经取消
            if (actuationManager.GetManagerThreadCancelStatus())
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"{title}_{Content}_{ButtonRedirectInfo} 报警弹框自动退出", MessageDegree.WARN);
                RequestClose?.Invoke(new DialogResult(ButtonResult.None));
            }
            //监视管理线程是否退出,如果退出则窗口自动关闭
            task = Task.Run(() =>
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    if (actuationManager.GetManagerThreadCancelStatus())
                    {
                        Application.Current.Dispatcher.InvokeAsync(() => { RequestClose?.Invoke(new DialogResult(ButtonResult.None)); });
                        logService.WriteLog(LogTypes.DB.ToString(), $@"{title}_{Content}_{ButtonRedirectInfo}_报警弹框自动退出", MessageDegree.WARN);
                        break;
                    }
                    //如果用户没有处理窗口按了启动,则发报警事件
                    if (GlobalValues.MachineStatus == FSMStateCode.Running || GlobalValues.MachineStatus == FSMStateCode.BurnInTesting)
                    {
                        GlobalValues.OperationCommandQueue.Enqueue(new FSMEvent(FSMEventCode.Alarm));
                        Thread.Sleep(200);
                    }
                    Thread.Sleep(20);
                }
            }, cancellationTokenSource.Token);
        }

        private DelegateCommand ignoreCommand;
        public DelegateCommand IgnoreCommand =>
            ignoreCommand ?? (ignoreCommand = new DelegateCommand(ExecuteIgnoreCommand));

        void ExecuteIgnoreCommand()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Ignore));
        }

        private DelegateCommand cancelCommand;
        public DelegateCommand CancelCommand =>
            cancelCommand ?? (cancelCommand = new DelegateCommand(ExecuteCancelCommand));

        void ExecuteCancelCommand()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        private DelegateCommand<string> yesCommand;
        public DelegateCommand<string> YesCommand =>
            yesCommand ?? (yesCommand = new DelegateCommand<string>(ExecuteYesCommand));

        void ExecuteYesCommand(string parameter)
        {
            //软件界面按钮不带参数parameter为Null.查找控件模拟点击带参数parameter为Auto(button.Command.Execute("Auto"))
            //2024/9/24移除模拟点击方案,采用窗口自管理方案.管理线程停止则窗口自动关闭
            RequestClose?.Invoke(new DialogResult(ButtonResult.Yes, new DialogParameters()));
        }
        private DelegateCommand _RedirectCommand;
        public DelegateCommand RedirectCommand =>
            _RedirectCommand ?? (_RedirectCommand = new DelegateCommand(ExecuteRedirectCommand));

        void ExecuteRedirectCommand()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Abort));
        }

        public event Action<IDialogResult> RequestClose;
        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            cancellationTokenSource?.Cancel();

            if (task != null && !task.Wait(1000))
                logService.WriteLog(LogTypes.DB.ToString(), $@"{title}_{Content}_{ButtonRedirectInfo} 报警弹框监视线程退出超时", MessageDegree.FATAL);
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            cancellationTokenSource = new CancellationTokenSource();

            Content = parameters.GetValue<string>("Content");
            Title = parameters.GetValue<string>("Title");
            ButtonIgnoreInfo = parameters.GetValue<string>("IgnoreInfo") ?? "";
            ButtonCancelInfo = parameters.GetValue<string>("CancelInfo") ?? "";
            ButtonYesInfo = parameters.GetValue<string>("YesInfo") ?? "";
            ButtonRedirectInfo = parameters.GetValue<string>("RedirectInfo") ?? "";
            actuationManager = parameters.GetValue<ActuationManagerAbs>("ActuationManager");
        }
    }
}
