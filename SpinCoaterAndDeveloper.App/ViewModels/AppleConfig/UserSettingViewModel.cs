using AutoMapper;
using DataBaseServiceInterface;
using LogServiceInterface;
using MaterialDesignThemes.Wpf;
using PermissionServiceInterface;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using PrismAutomationPlatformExt;
using SpinCoaterAndDeveloper.App.Common;
using SpinCoaterAndDeveloper.App.Common.Models;
using SpinCoaterAndDeveloper.App.Extensions;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using SpinCoaterAndDeveloper.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class UserSettingViewModel : BindableBase, INavigationAware
    {
        private readonly IDialogHostService dialogHostService;
        private readonly IDataBaseService dataBaseService;
        private readonly IMapper mapper;
        private readonly IEventAggregator eventAggregator;
        private readonly ILogService logService;
        private readonly IPermissionService permissionService;
        private readonly ISnackbarMessageQueue snackbarMessageQueue;

        private UserInfoModel newUser;

        public UserInfoModel NewUser
        {
            get { return newUser; }
            set { SetProperty(ref newUser, value); }
        }

        private UserInfoModel changePWDUser;

        public UserInfoModel ChangePWDUser
        {
            get { return changePWDUser; }
            set { SetProperty(ref changePWDUser, value); }
        }
        public ObservableCollection<UserInfoModel> Users { get; set; } = new ObservableCollection<UserInfoModel>();

        private bool newUserEnable = true;

        public bool NewUserEnable
        {
            get { return newUserEnable; }
            set { SetProperty(ref newUserEnable, value); }
        }
        private bool searchUserEnable = true;

        public bool SearchUserEnable
        {
            get { return searchUserEnable; }
            set { SetProperty(ref searchUserEnable, value); }
        }

        public UserSettingViewModel(IContainerProvider containerProvider)
        {
            this.dialogHostService = containerProvider.Resolve<IDialogHostService>();
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
            this.mapper = containerProvider.Resolve<IMapper>();
            this.eventAggregator = containerProvider.Resolve<IEventAggregator>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.permissionService = containerProvider.Resolve<IPermissionService>();
            this.snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();
        }

        private DelegateCommand searchUserCommand;
        public DelegateCommand SearchUserCommand =>
            searchUserCommand ?? (searchUserCommand = new DelegateCommand(ExecuteSearchUserCommand).ObservesCanExecute(() => SearchUserEnable));

        async void ExecuteSearchUserCommand()
        {
            SearchUserEnable = false;
            try
            {
                Users.Clear();
                var searchResult = await dataBaseService.Db.Queryable<UserInfoEntity>().ToListAsync();
                foreach (var userEntity in searchResult)
                {
                    Users.Add(mapper.Map<UserInfoModel>(userEntity));
                }
            }
            catch (Exception ex)
            {
                logService.WriteLog("UserManager", "查找用户", ex);
                snackbarMessageQueue.EnqueueEx(ex.Message);
            }
            finally
            {
                SearchUserEnable = true;
            }
        }

        private DelegateCommand createUserCommand;
        public DelegateCommand CreateUserCommand =>
            createUserCommand ?? (createUserCommand = new DelegateCommand(ExecuteCreateUserCommand).ObservesCanExecute(() => NewUserEnable));

        async void ExecuteCreateUserCommand()
        {
            NewUserEnable = false;
            try
            {
                if (!permissionService.CheckPermission(PermissionLevel.Admin))
                {
                    snackbarMessageQueue.EnqueueEx("权限不足，请登录管理员权限");
                    return;
                }
                if (string.IsNullOrEmpty(NewUser.UserName))
                {
                    snackbarMessageQueue.EnqueueEx("请输入用户名");
                    return;
                }
                if (string.IsNullOrEmpty(NewUser.Password) || string.IsNullOrEmpty(NewUser.PasswordSec))
                {
                    snackbarMessageQueue.EnqueueEx("请输入密码");
                    return;
                }
                if (NewUser.Password != NewUser.PasswordSec)
                {
                    snackbarMessageQueue.EnqueueEx("两次输入的密码不一致");
                    return;
                }
                if (NewUser.Authority == null)
                {
                    snackbarMessageQueue.EnqueueEx("请选择用户权限");
                    return;
                }
                var user = await dataBaseService.Db.Queryable<UserInfoEntity>().Where(it => it.UserName == NewUser.UserName).FirstAsync();
                if (user != null)
                {
                    snackbarMessageQueue.EnqueueEx("用户已存在");
                    return;
                }
                await dataBaseService.Db.Insertable<UserInfoEntity>(new UserInfoEntity() { UserName = NewUser.UserName, Password = NewUser.Password, Authority = NewUser.Authority }).ExecuteCommandAsync();
                NewUser = new UserInfoModel();
            }
            catch (Exception ex)
            {
                logService.WriteLog("UserManager", "创建用户", ex);
                snackbarMessageQueue.EnqueueEx(ex.Message);
            }
            finally
            {
                NewUserEnable = true;
            }
        }

        private DelegateCommand<UserInfoModel> deleteUserCommand;
        public DelegateCommand<UserInfoModel> DeleteUserCommand =>
            deleteUserCommand ?? (deleteUserCommand = new DelegateCommand<UserInfoModel>(ExecuteDeleteUser));

        async void ExecuteDeleteUser(UserInfoModel user)
        {
            try
            {
                if (!permissionService.CheckPermission(PermissionLevel.Admin))
                {
                    snackbarMessageQueue.EnqueueEx("权限不足，请登录管理员权限");
                    return;
                }
                if (user == null)
                {
                    snackbarMessageQueue.EnqueueEx("请选择要删除的用户");
                    return;
                }
                var confirmResult = await dialogHostService.HostQuestion("警告", "确认删除?", "取消", "确认");
                if (confirmResult.Result == Prism.Services.Dialogs.ButtonResult.No)
                    return;
                var result = await dataBaseService.Db.Deleteable<UserInfoEntity>().Where(it => it.UserName == user.UserName).ExecuteCommandAsync();
                if (result == 0)
                {
                    snackbarMessageQueue.EnqueueEx("删除失败");
                    return;
                }
                ExecuteSearchUserCommand();
            }
            catch (Exception ex)
            {
                logService.WriteLog("UserManager", "删除用户", ex);
                snackbarMessageQueue.EnqueueEx(ex.Message);
            }
        }

        private DelegateCommand<UserInfoModel> changePWDCommand;


        public DelegateCommand<UserInfoModel> ChangePWDCommand =>
            changePWDCommand ?? (changePWDCommand = new DelegateCommand<UserInfoModel>(ExecuteChangePWDCommand));

        async void ExecuteChangePWDCommand(UserInfoModel user)
        {
            if (string.IsNullOrEmpty(user.UserName) || string.IsNullOrEmpty(user.PasswordOld) || string.IsNullOrEmpty(user.Password) || string.IsNullOrEmpty(user.PasswordSec) || string.IsNullOrEmpty(user.Authority))
            {
                snackbarMessageQueue.EnqueueEx("请输入正确信息");
                return;
            }
            if (user.Authority == "管理员")
            {
                if (!permissionService.CheckPermission(PermissionLevel.Admin))
                {
                    snackbarMessageQueue.EnqueueEx("需要管理员权限");
                    return;
                }
            }
            var _user = await dataBaseService.Db.Queryable<UserInfoEntity>().Where(it => it.UserName == user.UserName && it.Password == user.PasswordOld).FirstAsync();
            if (_user == null)
            {
                snackbarMessageQueue.EnqueueEx("用户不存在或密码不正确");
                return;
            }
            if (user.Password != user.PasswordSec)
            {
                snackbarMessageQueue.EnqueueEx("两次输入的密码不一致");
                return;
            }
            var result = await dataBaseService.Db.Updateable<UserInfoEntity>(new UserInfoEntity() { Password = user.Password, Authority = user.Authority }).Where(it => it.UserName == user.UserName).IgnoreColumns(it => new { it.UserName }).ExecuteCommandAsync();
            if (result == 0)
            {
                snackbarMessageQueue.EnqueueEx("密码修改失败");
                return;
            }
            ChangePWDUser = new UserInfoModel();
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            NewUser = new UserInfoModel();
            ChangePWDUser = new UserInfoModel();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {

        }
    }
}
