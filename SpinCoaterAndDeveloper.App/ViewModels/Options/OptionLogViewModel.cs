using LogServiceInterface;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class OptionLogViewModel : BindableBase
    {
        private readonly ILogService logService;

        public ObservableCollection<MessageItem> MessageList { get; set; } = new ObservableCollection<MessageItem>();
        public OptionLogViewModel(IContainerProvider containerProvider)
        {
            this.logService = containerProvider.Resolve<ILogService>();
            logService.ShowOnUI += obj =>
            {
                MessageList.Add(obj);
                if (MessageList.Count > 1000) MessageList.RemoveAt(0);
            };
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
    }
}
