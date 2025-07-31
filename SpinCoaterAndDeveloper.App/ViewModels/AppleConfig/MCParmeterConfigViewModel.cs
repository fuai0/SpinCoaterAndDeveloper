using AutoMapper;
using DataBaseServiceInterface;
using LogServiceInterface;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;
using PermissionServiceInterface;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using PrismAutomationPlatformExt;
using SpinCoaterAndDeveloper.App.Common.Models;
using SpinCoaterAndDeveloper.App.Extensions;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using SpinCoaterAndDeveloper.Shared.Extensions;
using SpinCoaterAndDeveloper.Shared.Models.MotionControlModels;
using SpinCoaterAndDeveloper.Shared.Services.MotionResourceInitService;
using SqlSugar.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class MCParmeterConfigViewModel : BindableBase, INavigationAware
    {
        private readonly IDialogHostService dialogHostService;
        private readonly IMapper mapper;
        private readonly IDataBaseService dataBaseService;
        private readonly ISnackbarMessageQueue snackbarMessageQueue;
        private readonly ILogService logService;
        private readonly IPermissionService permissionService;
        private readonly IMotionResourceInit motionResourceInit;

        #region Binding
        private bool _IsRightDrawerOpen;
        public bool IsRightDrawerOpen
        {
            get { return _IsRightDrawerOpen; }
            set { SetProperty(ref _IsRightDrawerOpen, value); }
        }

        private ParmeterInfoModel _NewPar;
        public ParmeterInfoModel NewPar
        {
            get { return _NewPar; }
            set { SetProperty(ref _NewPar, value); }
        }

        private ParmeterInfoModel _CurrentSelectPar;
        public ParmeterInfoModel CurrentSelectPar
        {
            get { return _CurrentSelectPar; }
            set { SetProperty(ref _CurrentSelectPar, value); }
        }
        #endregion
        public ObservableCollection<ParmeterInfoModel> ParmeterCollection { get; set; } = new ObservableCollection<ParmeterInfoModel>();
        public ObservableCollection<string> ParmeterGroupCollection { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<MessageItem> MessageList { get; set; } = new ObservableCollection<MessageItem>();
        public MCParmeterConfigViewModel(IContainerProvider containerProvider)
        {
            this.mapper = containerProvider.Resolve<IMapper>();
            this.dialogHostService = containerProvider.Resolve<IDialogHostService>();
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
            this.snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.permissionService = containerProvider.Resolve<IPermissionService>();
            this.motionResourceInit = containerProvider.Resolve<IMotionResourceInit>();
        }

        private DelegateCommand _AddParCfgCommand;
        public DelegateCommand AddParCfgCommand =>
            _AddParCfgCommand ?? (_AddParCfgCommand = new DelegateCommand(ExecuteAddParCfgCommand));

        void ExecuteAddParCfgCommand()
        {
            IsRightDrawerOpen = true;
        }

        private DelegateCommand addParmeterCommand;
        public DelegateCommand AddParmeterCommand =>
            addParmeterCommand ?? (addParmeterCommand = new DelegateCommand(ExecuteAddParmeterCommand));

        async void ExecuteAddParmeterCommand()
        {
            var result = await dialogHostService.ShowHostDialog("AddParmeterView", null);
            if (result.Result == Prism.Services.Dialogs.ButtonResult.OK)
            {
                var productId = dataBaseService.Db.Queryable<ProductInfoEntity>().First(it => it.Select == true);
                if (productId == null)
                {
                    snackbarMessageQueue.EnqueueEx($"无当前生成产品,无法添加!");
                    GetParmeters();
                    return;
                }
                if (Regex.IsMatch(result.Parameters.GetValue<ParmeterInfoModel>("ParModel").Name, "^\\d"))
                {
                    snackbarMessageQueue.EnqueueEx("名称不合法,名称不能以数字开头");
                    return;
                }
                if (dataBaseService.Db.Queryable<ParmeterInfoEntity>().Where(x => x.Name == result.Parameters.GetValue<ParmeterInfoModel>("ParModel").Name && x.ProductInfo.Select == true).Any())
                {
                    snackbarMessageQueue.EnqueueEx("已存在相同名称参数,创建失败.");
                    return;
                }

                dataBaseService.Db.Insertable(new ParmeterInfoEntity() { Name = result.Parameters.GetValue<ParmeterInfoModel>("ParModel").Name, Data = result.Parameters.GetValue<ParmeterInfoModel>("ParModel").Data, ProductId = productId.Id }).ExecuteCommand();
                GetParmeters();
                GetParmeterGroup();
                logService.WriteLog(LogTypes.DB.ToString(), $"用户{permissionService.CurrentUserName}创建参数{result.Parameters.GetValue<ParmeterInfoModel>("ParModel").Name} : {result.Parameters.GetValue<ParmeterInfoModel>("ParModel").Data}成功.", MessageDegree.INFO);
                snackbarMessageQueue.EnqueueEx($"创建成功!");
            }
        }

        private DelegateCommand _AddParCommand;
        public DelegateCommand AddParCommand =>
            _AddParCommand ?? (_AddParCommand = new DelegateCommand(ExecuteAddParCommand));

        void ExecuteAddParCommand()
        {
            if (string.IsNullOrWhiteSpace(NewPar.Name))
            {
                snackbarMessageQueue.EnqueueEx("请输入名称");
                return;
            }
            if (Regex.IsMatch(NewPar.Name, "^\\d"))
            {
                snackbarMessageQueue.EnqueueEx("名称不合法,名称不能以数字开头");
                return;
            }
            if (dataBaseService.Db.Queryable<ParmeterInfoEntity>().Where(x => x.Name == NewPar.Name && x.ProductInfo.Select == true).Any())
            {
                snackbarMessageQueue.EnqueueEx("名称重复");
                return;
            }
            if (string.IsNullOrWhiteSpace(NewPar.Data))
            {
                snackbarMessageQueue.EnqueueEx("请输入数值");
                return;
            }
            var product = dataBaseService.Db.Queryable<ProductInfoEntity>().Where(x => x.Select == true).First();
            NewPar.ProductId = product.Id;
            dataBaseService.Db.Insertable(mapper.Map<ParmeterInfoEntity>(NewPar)).ExecuteCommand();
            GetParmeters();
            GetParmeterGroup();
            logService.WriteLog(LogTypes.DB.ToString(), $"用户{permissionService.CurrentUserName}创建参数{NewPar.Name}成功.\r\n{JsonConvert.SerializeObject(NewPar)}", MessageDegree.INFO);
            NewPar = new ParmeterInfoModel();
            snackbarMessageQueue.EnqueueEx("添加成功");
        }

        private DelegateCommand<ParmeterInfoModel> deleteParmeterCommand;
        public DelegateCommand<ParmeterInfoModel> DeleteParmeterCommand =>
            deleteParmeterCommand ?? (deleteParmeterCommand = new DelegateCommand<ParmeterInfoModel>(ExecuteDeleteParmeterCommand));

        async void ExecuteDeleteParmeterCommand(ParmeterInfoModel parameter)
        {
            if (parameter == null)
            {
                snackbarMessageQueue.EnqueueEx("请选中删除条目.");
            }
            else
            {
                var result = await dialogHostService.HostQuestion("提示", $"{"确认删除".TryFindResourceEx()} {parameter.Name}?", "取消", "删除");
                if (result.Result == Prism.Services.Dialogs.ButtonResult.OK)
                {
                    dataBaseService.Db.Deleteable<ParmeterInfoEntity>(mapper.Map<ParmeterInfoModel, ParmeterInfoEntity>(parameter)).ExecuteCommand();
                    GetParmeters();
                    NewPar = new ParmeterInfoModel();
                    GetParmeterGroup();
                    CurrentSelectPar = null;
                    logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}删除参数{parameter.Name}成功", MessageDegree.INFO);
                }
            }
        }

        private DelegateCommand _DeleteParCommand;
        public DelegateCommand DeleteParCommand =>
            _DeleteParCommand ?? (_DeleteParCommand = new DelegateCommand(ExecuteDeleteParCommand));

        async void ExecuteDeleteParCommand()
        {
            if (CurrentSelectPar == null)
            {
                snackbarMessageQueue.EnqueueEx("请选中需要删除的参数");
                return;
            }
            var result = await dialogHostService.ShowHostDialog("DialogHostMessageView", new DialogParameters() { { "Title", "警告" }, { "Content", $"确认删除参数{CurrentSelectPar.Name}?" }, { "CancelInfo", "取消" }, { "SaveInfo", "确定" } });
            if (result.Result != ButtonResult.OK)
            {
                return;
            }
            dataBaseService.Db.Deleteable(mapper.Map<ParmeterInfoEntity>(CurrentSelectPar)).ExecuteCommand();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}删除参数{CurrentSelectPar.Name}成功", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("删除成功");
            GetParmeters();
            GetParmeterGroup();
            NewPar = new ParmeterInfoModel();
            CurrentSelectPar = null;
            UpdateGlobalMCParmeterDic();
        }

        private DelegateCommand _ShowAllParsCommand;
        public DelegateCommand ShowAllParsCommand =>
            _ShowAllParsCommand ?? (_ShowAllParsCommand = new DelegateCommand(ExecuteShowAllParsCommand));

        void ExecuteShowAllParsCommand()
        {
            GetParmeters();
            NewPar = new ParmeterInfoModel();
            ParmeterGroupCollection.Clear();
            GetParmeterGroup();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}显示所有参数成功", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("显示所有参数成功");
        }

        private DelegateCommand<string> _GroupChangedCommand;
        public DelegateCommand<string> GroupChangedCommand =>
            _GroupChangedCommand ?? (_GroupChangedCommand = new DelegateCommand<string>(ExecuteGroupChangedCommand));

        void ExecuteGroupChangedCommand(string parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter))
            {
                return;
            }
            var pars = dataBaseService.Db.Queryable<ParmeterInfoEntity>().Includes(x => x.ProductInfo).Where(x => x.Group == parameter && x.ProductInfo.Select == true).OrderBy(x => x.Number).OrderBy(x => x.Id).ToList();
            ParmeterCollection.Clear();
            mapper.Map(pars, ParmeterCollection);
            snackbarMessageQueue.EnqueueEx("筛选成功");
        }

        private DelegateCommand _SaveSelectedParCommand;
        public DelegateCommand SaveSelectedParCommand =>
            _SaveSelectedParCommand ?? (_SaveSelectedParCommand = new DelegateCommand(ExecuteSaveSelectedParCommand));

        void ExecuteSaveSelectedParCommand()
        {
            if (CurrentSelectPar == null)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"请选中需要保存的参数", MessageDegree.WARN);
                return;
            }
            if (Regex.IsMatch(CurrentSelectPar.Name, "^\\d"))
            {
                snackbarMessageQueue.EnqueueEx("名称不合法,名称不能以数字开头");
                return;
            }
            bool checkResult = true;
            switch (CurrentSelectPar.DataType)
            {
                case ParmeterType.String:
                    checkResult &= true;
                    break;
                case ParmeterType.Bool:
                    checkResult &= bool.TryParse(CurrentSelectPar.Data, out _);
                    break;
                case ParmeterType.Byte:
                    checkResult &= byte.TryParse(CurrentSelectPar.Data, out _);
                    break;
                case ParmeterType.Short:
                    checkResult &= short.TryParse(CurrentSelectPar.Data, out _);
                    break;
                case ParmeterType.Int:
                    checkResult &= int.TryParse(CurrentSelectPar.Data, out _);
                    break;
                case ParmeterType.Double:
                    checkResult &= double.TryParse(CurrentSelectPar.Data, out _);
                    break;
                case ParmeterType.Float:
                    checkResult &= float.TryParse(CurrentSelectPar.Data, out _);
                    break;
                case ParmeterType.DateTime:
                    checkResult &= DateTime.TryParse(CurrentSelectPar.Data, out _);
                    break;
                default:
                    break;
            }

            if (checkResult == false)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"数据非法,无法保存!", MessageDegree.ERROR);
                snackbarMessageQueue.EnqueueEx("保存失败");
                return;
            }

            if (ParmeterCollection.GroupBy(x => x.Name).Where(x => x.Count() > 1).Count() > 0)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"名字重复,无法保存", MessageDegree.ERROR);
                snackbarMessageQueue.EnqueueEx("保存失败");
                return;
            }
            var org = dataBaseService.Db.Queryable<ParmeterInfoEntity>().Where(x => x.Id == CurrentSelectPar.Id).First();
            ParCompare(org, CurrentSelectPar);

            dataBaseService.Db.Updateable(mapper.Map<ParmeterInfoEntity>(CurrentSelectPar)).ExecuteCommand();
            GetParmeterGroup();
            UpdateGlobalMCParmeterDic();
            logService.WriteLog(LogTypes.DB.ToString(), $"用户{permissionService.CurrentUserName}保存参数信息成功.", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("保存成功");
        }

        private DelegateCommand _SaveAllParsCommand;
        public DelegateCommand SaveAllParsCommand =>
            _SaveAllParsCommand ?? (_SaveAllParsCommand = new DelegateCommand(ExecuteSaveAllParsCommand));

        void ExecuteSaveAllParsCommand()
        {
            bool repeatName = ParmeterCollection.GroupBy(x => x.Name).Where(x => x.Count() > 1).Count() > 0;
            if (repeatName)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"名字重复,无法保存", MessageDegree.ERROR);
                snackbarMessageQueue.EnqueueEx("保存失败");
                return;
            }
            foreach (var item in ParmeterCollection)
            {
                if (Regex.IsMatch(item.Name, "^\\d"))
                {
                    snackbarMessageQueue.EnqueueEx("名称不合法,名称不能以数字开头");
                    return;
                }
            }
            bool checkResult = true;
            foreach (var par in ParmeterCollection)
            {
                switch (par.DataType)
                {
                    case ParmeterType.String:
                        checkResult &= true;
                        break;
                    case ParmeterType.Bool:
                        checkResult &= bool.TryParse(par.Data, out _);
                        break;
                    case ParmeterType.Byte:
                        checkResult &= byte.TryParse(par.Data, out _);
                        break;
                    case ParmeterType.Short:
                        checkResult &= short.TryParse(par.Data, out _);
                        break;
                    case ParmeterType.Int:
                        checkResult &= int.TryParse(par.Data, out _);
                        break;
                    case ParmeterType.Double:
                        checkResult &= double.TryParse(par.Data, out _);
                        break;
                    case ParmeterType.Float:
                        checkResult &= float.TryParse(par.Data, out _);
                        break;
                    case ParmeterType.DateTime:
                        checkResult &= DateTime.TryParse(par.Data, out _);
                        break;
                    default:
                        break;
                }
            }
            if (checkResult == false)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"数据非法,无法保存!", MessageDegree.ERROR);
                snackbarMessageQueue.EnqueueEx("保存失败");
                return;
            }

            dataBaseService.Db.Queryable<ParmeterInfoEntity>().Where(x => x.ProductInfo.Select == true).OrderBy(x => x.Number).OrderBy(x => x.Id).ToList().ForEach(orgItem =>
            {
                var updateItem = ParmeterCollection.Where(x => x.Id == orgItem.Id).FirstOrDefault();
                if (updateItem != null) ParCompare(orgItem, updateItem);
            });

            dataBaseService.Db.Updateable(mapper.Map<List<ParmeterInfoEntity>>(ParmeterCollection)).ExecuteCommand();
            GetParmeterGroup();
            UpdateGlobalMCParmeterDic();
            logService.WriteLog(LogTypes.DB.ToString(), $@"保存成功", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("保存成功");
        }

        private DelegateCommand _DataCheckCommand;
        public DelegateCommand DataCheckCommand =>
            _DataCheckCommand ?? (_DataCheckCommand = new DelegateCommand(ExecuteDataCheckCommand));

        void ExecuteDataCheckCommand()
        {
            if (CurrentSelectPar == null) return;

            bool checkResult = true;
            switch (CurrentSelectPar.DataType)
            {
                case ParmeterType.String:
                    checkResult = true;
                    break;
                case ParmeterType.Bool:
                    checkResult = bool.TryParse(CurrentSelectPar.Data, out _);
                    break;
                case ParmeterType.Byte:
                    checkResult = byte.TryParse(CurrentSelectPar.Data, out _);
                    break;
                case ParmeterType.Short:
                    checkResult = short.TryParse(CurrentSelectPar.Data, out _);
                    break;
                case ParmeterType.Int:
                    checkResult = int.TryParse(CurrentSelectPar.Data, out _);
                    break;
                case ParmeterType.Double:
                    checkResult = double.TryParse(CurrentSelectPar.Data, out _);
                    break;
                case ParmeterType.Float:
                    checkResult = float.TryParse(CurrentSelectPar.Data, out _);
                    break;
                case ParmeterType.DateTime:
                    checkResult = DateTime.TryParse(CurrentSelectPar.Data, out _);
                    break;
                default:
                    break;
            }
            if (!checkResult)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"{CurrentSelectPar.Name}设定的数据非法!请修改.", MessageDegree.ERROR);
            }
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

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                ParmeterGroupCollection.Clear();
                GetParmeterGroup();
                GetParmeters();
            }), System.Windows.Threading.DispatcherPriority.Render);

            NewPar = new ParmeterInfoModel();
            logService.ShowOnUI += LogService_ShowOnUI;
            MessageList.Clear();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}进入参数配置页面", MessageDegree.INFO);
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            //更新全局运动参数集合,不使用重新初始化,防止设备运行中异常
            UpdateGlobalMCParmeterDic();
            logService.ShowOnUI -= LogService_ShowOnUI;
        }
        #region PrivateMethod
        private void UpdateGlobalMCParmeterDic()
        {
            var newPars = dataBaseService.Db.Queryable<ParmeterInfoEntity>().Includes(x => x.ProductInfo).Where(x => x.ProductInfo.Select == true).OrderBy(x => x.Number).OrderBy(x => x.Id).ToList();
            var willRemoveItems = new List<ParmeterInfo>();
            var willAddItems = new List<ParmeterInfo>();
            foreach (var item in GlobalValues.MCParmeterDicCollection)
            {
                var result = newPars.Where(x => x.Id == item.Value.GetId()).FirstOrDefault();
                if (result == null)
                {
                    willRemoveItems.Add(item.Value);
                }
                else
                {
                    if (item.Key == result.Name)
                        mapper.Map(result, item.Value);
                    else
                        willRemoveItems.Add(item.Value);
                }
            }
            willRemoveItems.ForEach(removeItem => GlobalValues.MCParmeterDicCollection.Remove(removeItem.Name));
            foreach (var item in newPars)
            {
                var result = GlobalValues.MCParmeterDicCollection.Values.ToList().Where(x => x.GetId() == item.Id).FirstOrDefault();
                if (result == null)
                    willAddItems.Add(mapper.Map<ParmeterInfo>(item));
            }
            willAddItems.ForEach(addItem => GlobalValues.MCParmeterDicCollection.Add(addItem.Name, addItem));
        }

        private void GetParmeters()
        {
            ParmeterCollection.Clear();
            var pars = dataBaseService.Db.Queryable<ParmeterInfoEntity>().Includes(x => x.ProductInfo).Where(x => x.ProductInfo.Select == true).OrderBy(x => x.Number).OrderBy(x => x.Id).ToList();
            mapper.Map(pars, ParmeterCollection);
        }

        private void GetParmeterGroup()
        {
            var parGroups = dataBaseService.Db.Queryable<ParmeterInfoEntity>().Where(x => x.ProductInfo.Select == true).Distinct().Select(x => x.Group).ToList();
            parGroups.ForEach(x => { if (!string.IsNullOrWhiteSpace(x) && !ParmeterGroupCollection.Contains(x)) ParmeterGroupCollection.Add(x); });
        }

        private void LogService_ShowOnUI(MessageItem obj)
        {
            MessageList.Add(obj);
            if (MessageList.Count > 1000) MessageList.RemoveAt(0);
        }

        private void ParCompare(ParmeterInfoEntity orgItem, ParmeterInfoModel updateItem)
        {
            if (orgItem.Number != updateItem.Number) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Number: {orgItem.Number}=>{updateItem.Number}", MessageDegree.INFO);
            if (orgItem.Name != updateItem.Name) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Name: {orgItem.Name}=>{updateItem.Name}", MessageDegree.INFO);
            if (orgItem.CNName != updateItem.CNName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} CNName: {orgItem.CNName}=>{updateItem.CNName}", MessageDegree.INFO);
            if (orgItem.ENName != updateItem.ENName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} ENName: {orgItem.ENName}=>{updateItem.ENName}", MessageDegree.INFO);
            if (orgItem.VNName != updateItem.VNName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} VNName: {orgItem.VNName}=>{updateItem.VNName}", MessageDegree.INFO);
            if (orgItem.XXName != updateItem.XXName) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} XXName: {orgItem.XXName}=>{updateItem.XXName}", MessageDegree.INFO);
            if (orgItem.Data != updateItem.Data) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Data: {orgItem.Data}=>{updateItem.Data}", MessageDegree.INFO);
            if (orgItem.DataType != updateItem.DataType) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} DataType: {orgItem.DataType}=>{updateItem.DataType}", MessageDegree.INFO);
            if (orgItem.Unit != updateItem.Unit) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Unit: {orgItem.Unit}=>{updateItem.Unit}", MessageDegree.INFO);
            if (orgItem.Group != updateItem.Group) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Group: {orgItem.Group}=>{updateItem.Group}", MessageDegree.INFO);
            if (orgItem.Backup != updateItem.Backup) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Backup: {orgItem.Backup}=>{updateItem.Backup}", MessageDegree.INFO);
            if (orgItem.Tag != updateItem.Tag) logService.WriteLog(LogTypes.DB.ToString(), $@"Id:{orgItem.Id} Tag: {orgItem.Tag}=>{updateItem.Tag}", MessageDegree.INFO);
        }
        #endregion
    }
}
