using AutoMapper;
using DataBaseServiceInterface;
using LogServiceInterface;
using MaterialDesignThemes.Wpf;
using PermissionServiceInterface;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using PrismAutomationPlatformExt;
using SpinCoaterAndDeveloper.App.Extensions;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using SpinCoaterAndDeveloper.Shared.Extensions;
using SpinCoaterAndDeveloper.Shared.Services.MotionResourceInitService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Windows;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class SystemSettingViewModel : BindableBase, INavigationAware
    {
        private readonly IDataBaseService dataBaseService;
        private readonly IMapper mapper;
        private readonly IDialogHostService dialogHostService;
        private readonly ISnackbarMessageQueue snackbarMessageQueue;
        private readonly IMotionResourceInit motionResourceInit;
        private readonly ILogService logService;
        private readonly IPermissionService permissionService;

        private bool currentSelect;
        public bool CurrentSelect
        {
            get { return currentSelect; }
            set { SetProperty(ref currentSelect, value); }
        }
        private bool _SilenceAlarm;
        public bool SilenceAlarm
        {
            get { return _SilenceAlarm; }
            set { SetProperty(ref _SilenceAlarm, value); }
        }
        private MessageDegree _LogLevel;
        public MessageDegree LogLevel
        {
            get { return _LogLevel; }
            set { SetProperty(ref _LogLevel, value); }
        }

        public ObservableCollection<ProductInfoEntity> ProductCollections { get; set; } = new ObservableCollection<ProductInfoEntity>();
        public SystemSettingViewModel(IContainerProvider containerProvider)
        {
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
            this.mapper = containerProvider.Resolve<IMapper>();
            this.dialogHostService = containerProvider.Resolve<IDialogHostService>();
            this.snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();
            this.motionResourceInit = containerProvider.Resolve<IMotionResourceInit>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.permissionService = containerProvider.Resolve<IPermissionService>();
        }
        public SystemSettingViewModel()
        {
            switch (ConfigurationManager.AppSettings["Language"])
            {
                case "zh-CN":
                    CurrentSelect = false;
                    break;
                case "en-US":
                    CurrentSelect = true;
                    break;
                default:
                    break;
            }
        }
        private DelegateCommand<object> languageSelectCommand;
        public DelegateCommand<object> LanguageSelectCommand =>
            languageSelectCommand ?? (languageSelectCommand = new DelegateCommand<object>(ExecuteLanguageSelectCommand));

        void ExecuteLanguageSelectCommand(object parameter)
        {

            List<ResourceDictionary> dictionaryList = new List<ResourceDictionary>();
            foreach (ResourceDictionary dictionary in Application.Current.Resources.MergedDictionaries)
            {
                if (dictionary.Source != null)
                {
                    dictionaryList.Add(dictionary);
                }
            }
            string requestedCulture = "";
            if ((bool)parameter)
            {
                requestedCulture = @"Resources\Language\en-US.xaml";
                SaveSetting("Language", "en-US");
                System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
            }
            else
            {
                requestedCulture = @"Resources\Language\zh-CN.xaml";
                SaveSetting("Language", "zh-CN");
                System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("zh-CN");
            }
            ResourceDictionary resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString.Equals(requestedCulture));
            Application.Current.Resources.MergedDictionaries.Remove(resourceDictionary);
            Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
        }
        private void SaveSetting(string name, string data)
        {
            Configuration cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            cfg.AppSettings.Settings[name].Value = data;
            cfg.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
        //添加新产品
        private DelegateCommand addNewProductCommand;
        public DelegateCommand AddNewProductCommand =>
            addNewProductCommand ?? (addNewProductCommand = new DelegateCommand(ExecuteAddNewProductCommand));

        async void ExecuteAddNewProductCommand()
        {
            var result = await dialogHostService.ShowHostDialog("AddNewProductView", null);
            if (result.Result == Prism.Services.Dialogs.ButtonResult.OK)
            {
                foreach (var product in ProductCollections)
                {
                    if (product.Name == result.Parameters.GetValue<string>("Name"))
                    {
                        snackbarMessageQueue.EnqueueEx("已存在相同名称产品,创建失败");
                        return;
                    }
                }
                var newProductId = await dataBaseService.Db.Insertable(new ProductInfoEntity() { Name = result.Parameters.GetValue<string>("Name"), Select = false }).ExecuteReturnIdentityAsync();
                //复制运动参数
                var parmeters = await dataBaseService.Db.Queryable<ParmeterInfoEntity>().Where(x => x.ProductInfo.Select == true).ToListAsync();
                parmeters.ForEach(x => { x.Id = 0; x.ProductId = newProductId; });
                await dataBaseService.Db.Insertable(parmeters).ExecuteCommandAsync();
                //复制屏蔽功能
                var functionShields = await dataBaseService.Db.Queryable<FunctionShieldEntity>().Where(x => x.ProductInfo.Select == true).ToListAsync();
                functionShields.ForEach(x => { x.Id = 0; x.ProductId = newProductId; });
                await dataBaseService.Db.Insertable(functionShields).ExecuteCommandAsync();
                //复制运动点位
                var mPoints = await dataBaseService.Db.Queryable<MovementPointInfoEntity>()
                    .Includes(x => x.MovementPointPositions)
                    .Includes(x => x.MovementPointSecurities)
                    .Where(x => x.ProductInfo.Select == true)
                    .ToListAsync();
                mPoints.ForEach(x =>
                {
                    x.Id = 0;
                    x.ProductId = newProductId;
                    x.MovementPointPositions.ForEach(m => { m.Id = 0; m.MovementPointNameId = 0; });
                    x.MovementPointSecurities.ForEach(n => { n.Id = 0; n.MovementPointNameId = 0; });
                });
                await dataBaseService.Db.InsertNav(mPoints).Include(x => x.MovementPointPositions).Include(x => x.MovementPointSecurities).ExecuteCommandAsync();
                //复制插补路径
                var interpolations = await dataBaseService.Db.Queryable<InterpolationPathCoordinateEntity>()
                    .Includes(x1 => x1.AxisX)
                    .Includes(x2 => x2.AxisY)
                    .Includes(x3 => x3.AxisZ)
                    .Includes(x4 => x4.AxisR)
                    .Includes(x5 => x5.AxisA)
                    .Includes(x => x.ProductInfo)
                    .Where(x => x.ProductInfo.Select == true)
                    .OrderBy(it => it.Id)
                    .Includes(p => p.InterpolationPaths.OrderBy(s => s.Sequence).ToList())
                    .ToListAsync();
                interpolations.ForEach(x =>
                {
                    x.Id = 0;
                    x.ProductId = newProductId;
                    x.InterpolationPaths.ForEach(m => { m.Id = 0; m.CoordinateId = 0; });
                });
                await dataBaseService.Db.InsertNav(interpolations).Include(x => x.InterpolationPaths).ExecuteCommandAsync();

                GetProducts();
                snackbarMessageQueue.EnqueueEx("添加成功");
            }
        }
        //切换产品
        private DelegateCommand productChangeCommand;
        public DelegateCommand ProductChangeCommand =>
            productChangeCommand ?? (productChangeCommand = new DelegateCommand(ExecuteProductChangeCommand));

        void ExecuteProductChangeCommand()
        {
            dataBaseService.Db.Updateable(ProductCollections.ToList()).ExecuteCommand();
            UpdateGlobalData();
        }
        //修改产品名称
        private DelegateCommand changeProductNameCommand;
        public DelegateCommand ChangeProductNameCommand =>
            changeProductNameCommand ?? (changeProductNameCommand = new DelegateCommand(ExecuteChangeProductNameCommand));

        async void ExecuteChangeProductNameCommand()
        {
            var result = await dialogHostService.ShowHostDialog("ChangeProductNameView", null);
            if (result.Result == Prism.Services.Dialogs.ButtonResult.OK)
            {
                foreach (var product in ProductCollections)
                {
                    if (product.Name == result.Parameters.GetValue<string>("Name"))
                    {
                        snackbarMessageQueue.EnqueueEx("已存在相同名称产品,创建失败");
                        return;
                    }
                }
                var currentProduct = dataBaseService.Db.Queryable<ProductInfoEntity>().Where(it => it.Select == true).First();
                currentProduct.Name = result.Parameters.GetValue<string>("Name");
                dataBaseService.Db.Updateable(currentProduct).ExecuteCommand();
                GlobalValues.CurrentProduct = currentProduct.Name;
            }
            GetProducts();
        }

        private DelegateCommand _DeleteProductCommand;
        public DelegateCommand DeleteProductCommand =>
            _DeleteProductCommand ?? (_DeleteProductCommand = new DelegateCommand(ExecuteDeleteProductCommand));

        async void ExecuteDeleteProductCommand()
        {
            if (dataBaseService.Db.Queryable<ProductInfoEntity>().Count() <= 1)
            {
                snackbarMessageQueue.EnqueueEx("不能删除最后一个产品");
                return;
            }
            var currentProduct = await dataBaseService.Db.Queryable<ProductInfoEntity>().Where(x => x.Select == true).FirstAsync();
            var result = await dialogHostService.ShowHostDialog("DialogHostMessageView", new DialogParameters() { { "Title", "警告" }, { "Content", $"确认删除产品{currentProduct.Name}?\r\n请注意与产品关联的参数及运动点位将被删除." }, { "CancelInfo", "取消" }, { "SaveInfo", "确定" } });
            if (result.Result != ButtonResult.OK)
            {
                snackbarMessageQueue.EnqueueEx($"{"取消删除产品".TryFindResourceEx()?.ToString()}{currentProduct.Name}");
                return;
            }
            //删除产品关联运动参数
            await dataBaseService.Db.Deleteable<ParmeterInfoEntity>().Where(x => x.ProductId == currentProduct.Id).ExecuteCommandAsync();
            //删除产品关联屏蔽功能
            await dataBaseService.Db.Deleteable<FunctionShieldEntity>().Where(x => x.ProductId == currentProduct.Id).ExecuteCommandAsync();
            //删除产品关联运动点位
            await dataBaseService.Db.DeleteNav<MovementPointInfoEntity>(x => x.ProductId == currentProduct.Id).Include(m => m.MovementPointPositions).Include(m => m.MovementPointSecurities).ExecuteCommandAsync();
            //删除产品关联的插补路径
            await dataBaseService.Db.DeleteNav<InterpolationPathCoordinateEntity>(x => x.ProductId == currentProduct.Id).Include(m => m.InterpolationPaths).ExecuteCommandAsync();
            //删除产品
            await dataBaseService.Db.Deleteable(currentProduct).ExecuteCommandAsync();
            //设定默认选中第一个产品
            var newSelectProduct = await dataBaseService.Db.Queryable<ProductInfoEntity>().FirstAsync();
            newSelectProduct.Select = true;
            await dataBaseService.Db.Updateable(newSelectProduct).ExecuteCommandAsync();
            GetProducts();
            UpdateGlobalData();
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}删除产品{currentProduct.Name}", MessageDegree.INFO);
            snackbarMessageQueue.EnqueueEx("删除成功");
        }

        private DelegateCommand _ChangeSilenceAlarmCommand;
        public DelegateCommand ChangeSilenceAlarmCommand =>
            _ChangeSilenceAlarmCommand ?? (_ChangeSilenceAlarmCommand = new DelegateCommand(ExecuteChangeSilenceAlarmCommand));

        void ExecuteChangeSilenceAlarmCommand()
        {
            GlobalValues.SilenceAlarm = SilenceAlarm;
        }

        private DelegateCommand _LogLevelChangeCommand;
        public DelegateCommand LogLevelChangeCommand =>
            _LogLevelChangeCommand ?? (_LogLevelChangeCommand = new DelegateCommand(ExecuteLogLevelChangeCommand));

        void ExecuteLogLevelChangeCommand()
        {
            switch (LogLevel)
            {
                case MessageDegree.DEBUG:
                    logService.SetMessageDegree(MessageDegree.DEBUG);
                    break;
                case MessageDegree.INFO:
                    logService.SetMessageDegree(MessageDegree.INFO);
                    break;
                case MessageDegree.WARN:
                    logService.SetMessageDegree(MessageDegree.WARN);
                    break;
                case MessageDegree.ERROR:
                    logService.SetMessageDegree(MessageDegree.ERROR);
                    break;
                case MessageDegree.FATAL:
                    logService.SetMessageDegree(MessageDegree.FATAL);
                    break;
                default:
                    logService.SetMessageDegree(MessageDegree.DEBUG);
                    break;
            }
            SaveSetting("MessageDegree", LogLevel.ToString());
        }
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            switch (ConfigurationManager.AppSettings["Language"])
            {
                case "zh-CN":
                    CurrentSelect = false;
                    break;
                case "en-US":
                    CurrentSelect = true;
                    break;
                default:
                    break;
            }
            switch (ConfigurationManager.AppSettings["MessageDegree"])
            {
                case "DEBUG":
                    LogLevel = MessageDegree.DEBUG;
                    break;
                case "INFO":
                    LogLevel = MessageDegree.INFO;
                    break;
                case "WARN":
                    LogLevel = MessageDegree.WARN;
                    break;
                case "ERROR":
                    LogLevel = MessageDegree.ERROR;
                    break;
                case "FATAL":
                    LogLevel = MessageDegree.FATAL;
                    break;
                default:
                    LogLevel = MessageDegree.DEBUG;
                    break;
            }
            GetProducts();
            SilenceAlarm = GlobalValues.SilenceAlarm;
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}进入系统调试页面", MessageDegree.INFO);
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {

        }

        #region PrivateMethod
        private void UpdateGlobalData()
        {
            var currentProduct = dataBaseService.Db.Queryable<ProductInfoEntity>().Where(it => it.Select == true).First();
            GlobalValues.CurrentProduct = currentProduct != null ? currentProduct.Name : null;
            motionResourceInit.InitMCPointDicCollection();
            motionResourceInit.InitMCParmeterDicCollection();
            motionResourceInit.InitInterpolationPaths();
            motionResourceInit.InitFunctionShieldDicCollection();
        }

        private void GetProducts()
        {
            ProductCollections.Clear();
            var products = dataBaseService.Db.Queryable<ProductInfoEntity>().ToList();
            mapper.Map(products, ProductCollections);
        }
        #endregion
    }
}
