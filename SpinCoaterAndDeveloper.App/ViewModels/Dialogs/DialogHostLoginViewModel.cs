using DataBaseServiceInterface;
using MaterialDesignThemes.Wpf;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using PrismAutomationPlatformExt;
using SpinCoaterAndDeveloper.App.Common;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.App.ViewModels.Dialogs
{
    public class DialogHostLoginViewModel : BindableBase, IDialogHostAware
    {
        private readonly IDataBaseService dataBaseService;

        private string account;
        public string Account
        {
            get { return account; }
            set { SetProperty(ref account, value); }
        }
        private string password;
        public string Password
        {
            get { return password; }
            set { SetProperty(ref password, value); }
        }
        private string errorInfo;
        public string ErrorInfo
        {
            get { return errorInfo; }
            set { SetProperty(ref errorInfo, value); }
        }
        private bool loginBtnEnable = true;
        public bool LoginBtnEnable
        {
            get { return loginBtnEnable; }
            set { SetProperty(ref loginBtnEnable, value); }
        }

        public ObservableCollection<string> AccountsCollection { get; set; } = new ObservableCollection<string>();

        private DelegateCommand loginCommand;
        public DelegateCommand LoginCommand =>
            loginCommand ?? (loginCommand = new DelegateCommand(ExecuteLoginCommand).ObservesCanExecute(() => LoginBtnEnable));

        void ExecuteLoginCommand()
        {
            LoginBtnEnable = false;
            try
            {
                if (string.IsNullOrEmpty(Account) || string.IsNullOrEmpty(Password))
                {
                    ErrorInfo = "请输入用户名和密码";
                    return;
                }
                var user = dataBaseService.Db.Queryable<UserInfoEntity>().Where(it => it.UserName == Account && it.Password == Password).First();
                if (user != null)
                {
                    IDialogParameters dialogParameters = new DialogParameters();
                    dialogParameters.Add("user", user);
                    DialogHost.Close(DialogHostName, new DialogResult(ButtonResult.OK, dialogParameters));
                }
                else
                {
                    ErrorInfo = "登录失败";
                }
            }
            catch (Exception ex)
            {
                ErrorInfo = $"{ex.Message}";
            }
            finally
            {
                LoginBtnEnable = true;
            }
        }

        private DelegateCommand cancelCommand;

        public DelegateCommand CancelCommand =>
            cancelCommand ?? (cancelCommand = new DelegateCommand(ExecuteCancelCommand));

        void ExecuteCancelCommand()
        {
            DialogHost.Close(DialogHostName, new DialogResult(ButtonResult.Cancel));
        }

        public string DialogHostName { get; set; } = "Root";

        public DialogHostLoginViewModel(IContainerProvider containerProvider)
        {
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            AccountsCollection.Clear();
            dataBaseService.Db.Queryable<UserInfoEntity>().ToList().ForEach(x => AccountsCollection.Add(x.UserName));
        }
    }
}
