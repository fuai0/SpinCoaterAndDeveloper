using MaterialDesignThemes.Wpf;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using PrismAutomationPlatformExt;
using SpinCoaterAndDeveloper.App.Common;
using SpinCoaterAndDeveloper.App.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.App.ViewModels.Dialogs
{
    public class AddParmeterViewModel : BindableBase, IDialogHostAware
    {
        private string parmeterName;

        public string ParmeterName
        {
            get { return parmeterName; }
            set { SetProperty(ref parmeterName, value); }
        }

        private string parmeterData;

        public string ParmeterData
        {
            get { return parmeterData; }
            set { SetProperty(ref parmeterData, value); }
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
            if (string.IsNullOrEmpty(ParmeterName) || string.IsNullOrEmpty(ParmeterData))
            {
                return;
            }
            else
            {
                DialogParameters parameters = new DialogParameters();
                parameters.Add("ParModel", new ParmeterInfoModel() { Name = parmeterName, Data = ParmeterData });
                DialogHost.Close(DialogHostName, new DialogResult(ButtonResult.OK, parameters));
            }
        }
        public string DialogHostName { get; set; } = "Root";

        public void OnDialogOpened(IDialogParameters parameters)
        {

        }
    }
}
