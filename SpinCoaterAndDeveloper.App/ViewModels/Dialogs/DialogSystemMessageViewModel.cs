using LogServiceInterface;
using MotionControlActuation;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SpinCoaterAndDeveloper.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class DialogSystemMessageViewModel : BindableBase, IDialogAware
    {
        private readonly ILogService logService;

        private SysDialogLevel _DialogLevel;
        public SysDialogLevel DialogLevel
        {
            get { return _DialogLevel; }
            set { SetProperty(ref _DialogLevel, value); }
        }
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

        public DialogSystemMessageViewModel(IContainerProvider containerProvider)
        {
            this.logService = containerProvider.Resolve<ILogService>();
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
            RequestClose?.Invoke(new DialogResult(ButtonResult.Yes));
        }

        public event Action<IDialogResult> RequestClose;

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {

        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            Content = parameters.GetValue<string>("Content");
            Title = parameters.GetValue<string>("Title");
            ButtonCancelInfo = parameters.GetValue<string>("CancelInfo") ?? "";
            ButtonYesInfo = parameters.GetValue<string>("YesInfo") ?? "";
            DialogLevel = parameters.GetValue<SysDialogLevel>("DialogLevel");
        }
    }
}
