using LogServiceInterface;
using PermissionServiceInterface;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class GlobalLogViewModel : BindableBase, IDialogAware
    {
        private readonly ILogService logService;
        private readonly IPermissionService permissionService;

        public ObservableCollection<MessageItem> MessageList { get; set; } = new ObservableCollection<MessageItem>();
        public GlobalLogViewModel(IContainerProvider containerProvider)
        {
            this.logService = containerProvider.Resolve<ILogService>();
            this.permissionService = containerProvider.Resolve<IPermissionService>();
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

        public string Title { get; set; } = "GlobalLog".TryFindResourceEx();

#pragma warning disable 0067
        public event Action<IDialogResult> RequestClose;
#pragma warning restore 0067

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            logService.ShowOnUI -= LogService_ShowOnUI;
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}关闭全局日志", MessageDegree.INFO);
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            logService.ShowOnUI += LogService_ShowOnUI;
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}打开全局日志", MessageDegree.INFO);
        }

        private void LogService_ShowOnUI(MessageItem obj)
        {
            MessageList.Add(obj);
            if (MessageList.Count > 1000) MessageList.RemoveAt(0);
        }
    }
}
