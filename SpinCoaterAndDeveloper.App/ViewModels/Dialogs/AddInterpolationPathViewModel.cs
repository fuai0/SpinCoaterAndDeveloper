using MaterialDesignThemes.Wpf;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using PrismAutomationPlatformExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.App.ViewModels.Dialogs
{
    public class AddInterpolationPathViewModel : BindableBase, IDialogHostAware
    {
        private string interpolationPathName;

        public string InterpolationPathName
        {
            get { return interpolationPathName; }
            set { SetProperty(ref interpolationPathName, value); }
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
            if (string.IsNullOrWhiteSpace(InterpolationPathName))
            {
                return;
            }
            else
            {
                DialogParameters parameters = new DialogParameters();
                parameters.Add("Name", InterpolationPathName);
                DialogHost.Close(DialogHostName, new DialogResult(ButtonResult.OK, parameters));
            }
        }
        public string DialogHostName { get; set; } = "Root";

        public void OnDialogOpened(IDialogParameters parameters)
        {

        }
    }
}
