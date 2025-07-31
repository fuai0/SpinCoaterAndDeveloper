using MaterialDesignThemes.Wpf;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using PrismAutomationPlatformExt;
using SpinCoaterAndDeveloper.App.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.App.ViewModels.Dialogs
{
    public class DialogHostMessageViewModel : BindableBase, IDialogHostAware
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
        private string cancelInfo;
        public string CancelInfo
        {
            get { return cancelInfo; }
            set { SetProperty(ref cancelInfo, value); }
        }
        private string saveInfo;
        public string SaveInfo
        {
            get { return saveInfo; }
            set { SetProperty(ref saveInfo, value); }
        }
        public string DialogHostName { get; set; } = "Root";

        private DelegateCommand saveCommand;
        public DelegateCommand SaveCommand =>
            saveCommand ?? (saveCommand = new DelegateCommand(ExecuteSaveCommand));

        void ExecuteSaveCommand()
        {
            DialogHost.Close(DialogHostName, new DialogResult(ButtonResult.OK));
        }
        private DelegateCommand cancelCommand;
        public DelegateCommand CancelCommand =>
            cancelCommand ?? (cancelCommand = new DelegateCommand(ExecuteCancelCommand));

        void ExecuteCancelCommand()
        {
            DialogHost.Close(DialogHostName, new DialogResult(ButtonResult.No));
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.ContainsKey("Title"))
                Title = parameters.GetValue<string>("Title");

            if (parameters.ContainsKey("Content"))
                Content = parameters.GetValue<string>("Content");

            if (parameters.ContainsKey("CancelInfo"))
                CancelInfo = parameters.GetValue<string>("CancelInfo");

            if (parameters.ContainsKey("SaveInfo"))
                SaveInfo = parameters.GetValue<string>("SaveInfo");

        }
    }
}
