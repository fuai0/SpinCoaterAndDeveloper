using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SerialPortService.ViewModels
{
    public class SerialPortMessageViewModel : BindableBase,IDialogAware
    {
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
        private DelegateCommand yesCommand;
        public DelegateCommand YesCommand =>
            yesCommand ?? (yesCommand = new DelegateCommand(ExecuteYesCommand));

        void ExecuteYesCommand()
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
        }
    }
}
