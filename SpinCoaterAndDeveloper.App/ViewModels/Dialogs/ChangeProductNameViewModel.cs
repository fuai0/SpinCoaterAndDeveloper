using MaterialDesignThemes.Wpf;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using PrismAutomationPlatformExt;
using SpinCoaterAndDeveloper.App.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class ChangeProductNameViewModel : BindableBase, IDialogHostAware
    {
        private readonly ISnackbarMessageQueue snackbarMessageQueue;

        private string productName;

        public string ProductName
        {
            get { return productName; }
            set { SetProperty(ref productName, value); }
        }

        public ChangeProductNameViewModel(IContainerProvider containerProvider)
        {
            snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();
        }

        private DelegateCommand cancelCommand;
        public DelegateCommand CancelCommand =>
            cancelCommand ?? (cancelCommand = new DelegateCommand(ExecuteCancelCommand));

        void ExecuteCancelCommand()
        {
            DialogHost.Close(DialogHostName, new DialogResult(ButtonResult.Cancel));
        }
        private DelegateCommand saveCommand;
        public DelegateCommand SaveCommand =>
            saveCommand ?? (saveCommand = new DelegateCommand(ExecuteSaveCommand));

        void ExecuteSaveCommand()
        {
            if (string.IsNullOrEmpty(ProductName))
            {
                snackbarMessageQueue.EnqueueEx("产品名称不能为空");
                return;
            }
            else
            {
                DialogParameters parameters = new DialogParameters();
                parameters.Add("Name", ProductName);
                DialogHost.Close(DialogHostName, new DialogResult(ButtonResult.OK, parameters));
            }
        }
        public string DialogHostName { get; set; } = "Root";
        public void OnDialogOpened(IDialogParameters parameters)
        {

        }
    }
}
