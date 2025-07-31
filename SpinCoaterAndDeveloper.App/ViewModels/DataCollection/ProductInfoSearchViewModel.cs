using AutoMapper;
using DataBaseServiceInterface;
using LogServiceInterface;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SpinCoaterAndDeveloper.App.Common.Models;
using SpinCoaterAndDeveloper.App.Extensions;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using SpinCoaterAndDeveloper.Shared.Extensions;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class ProductInfoSearchViewModel : BindableBase
    {

        private readonly IDataBaseService dataBaseService;
        private readonly IMapper mapper;
        private readonly ISnackbarMessageQueue snackbarMessageQueue;
        private readonly ILogService logService;
        private readonly IEventAggregator eventAggregator;

        private bool searchBtnEnable = true;

        public bool SearchBtnEnable
        {
            get { return searchBtnEnable; }
            set { SetProperty(ref searchBtnEnable, value); }
        }
        private string productStartDate;

        public string ProductStartDate
        {
            get { return productStartDate; }
            set { SetProperty(ref productStartDate, value); }
        }
        private string productStartTime;

        public string ProductStartTime
        {
            get { return productStartTime; }
            set { SetProperty(ref productStartTime, value); }
        }
        private string productEndDate;

        public string ProductEndDate
        {
            get { return productEndDate; }
            set { SetProperty(ref productEndDate, value); }
        }
        private string productEndTime;

        public string ProductEndTime
        {
            get { return productEndTime; }
            set { SetProperty(ref productEndTime, value); }
        }
        private string productLogNums;

        public string ProductLogNums
        {
            get { return productLogNums; }
            set { SetProperty(ref productLogNums, value); }
        }
        private string productCode;

        public string ProductCode
        {
            get { return productCode; }
            set { SetProperty(ref productCode, value); }
        }
        public ObservableCollection<ProductShowInfoModel> ProductInfoCollections { get; set; } = new ObservableCollection<ProductShowInfoModel>();
        public ProductInfoSearchViewModel(IContainerProvider containerProvider)
        {
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
            this.mapper = containerProvider.Resolve<IMapper>();
            this.snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.eventAggregator = containerProvider.Resolve<IEventAggregator>();
        }
        private DelegateCommand searchProductCommand;
        public DelegateCommand SearchProductCommand =>
            searchProductCommand ?? (searchProductCommand = new DelegateCommand(ExecuteSearchProductCommand).ObservesCanExecute(() => SearchBtnEnable));

        void ExecuteSearchProductCommand()
        {
            try
            {
                SearchBtnEnable = false;
                ProductInfoCollections.Clear();

                eventAggregator.UpdateLoadingEvent(true);
                var expable = Expressionable.Create<ProduceInfoEntity>();
                if (!string.IsNullOrEmpty(ProductStartDate))
                {
                    DateTime startTime = Convert.ToDateTime(ProductStartDate + (string.IsNullOrEmpty(ProductStartTime) ? " 00:00:00" : $" {ProductStartTime}:00"));
                    expable.And(it => it.ProductStartTime > startTime);
                }
                if (!string.IsNullOrEmpty(ProductEndDate))
                {
                    DateTime endTime = Convert.ToDateTime(ProductEndDate + (string.IsNullOrEmpty(ProductEndTime) ? " 00:00:00" : $" {ProductEndTime}:00"));
                    expable.And(it => it.ProductStartTime < endTime);
                }
                expable.AndIF(ProductCode != null, it => it.ProductCode == ProductCode);

                var exp = expable.ToExpression();
                Task.Run(async () =>
                {
                    List<ProduceInfoEntity> result = new List<ProduceInfoEntity>();
                    switch (ProductLogNums)
                    {
                        case "100":
                            result = await dataBaseService.Db.Queryable<ProduceInfoEntity>().Where(exp).SplitTable(tabs => tabs.Take(6)).OrderBy(it => it.CreateTime, SqlSugar.OrderByType.Desc).Take(100).ToListAsync();
                            break;
                        case "1000":
                            result = await dataBaseService.Db.Queryable<ProduceInfoEntity>().Where(exp).SplitTable(tabs => tabs.Take(6)).OrderBy(it => it.CreateTime, SqlSugar.OrderByType.Desc).Take(1000).ToListAsync();
                            break;
                        case "10000":
                            result = await dataBaseService.Db.Queryable<ProduceInfoEntity>().Where(exp).SplitTable(tabs => tabs.Take(6)).OrderBy(it => it.CreateTime, SqlSugar.OrderByType.Desc).Take(10000).ToListAsync();
                            break;
                        default:
                            result = await dataBaseService.Db.Queryable<ProduceInfoEntity>().Where(exp).SplitTable(tabs => tabs.Take(6)).OrderBy(it => it.CreateTime, SqlSugar.OrderByType.Desc).Take(100).ToListAsync();
                            break;
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var item in result)
                        {
                            ProductInfoCollections.Add(mapper.Map<ProductShowInfoModel>(item));
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                snackbarMessageQueue.EnqueueEx("查询出错");
                logService.WriteLog($"日志生产信息出错:{ex.Message}", ex);
            }
            finally
            {
                eventAggregator.UpdateLoadingEvent(false);
                SearchBtnEnable = true;
            }
        }

        //导出生产产品信息
        private DelegateCommand saveDataToExcelProductCommand;
        public DelegateCommand SaveDataToExcelProductCommand =>
            saveDataToExcelProductCommand ?? (saveDataToExcelProductCommand = new DelegateCommand(ExecuteSaveDataToExcelProductCommand));

        void ExecuteSaveDataToExcelProductCommand()
        {
            //保存文件
            string saveFileName = "";
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.DefaultExt = "xlsx";
            saveFileDialog.Filter = "Excel文件(*.xls)|*.xls|Excel文件(*.xlsx)|*.xlsx";
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.Title = "Excel文件保存路径";
            saveFileDialog.FileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            MemoryStream ms = new MemoryStream(); //MemoryStream
            if (saveFileDialog.ShowDialog() == true)
            {
                saveFileName = saveFileDialog.FileName;
                //检测文件是否被占用
                if (!Extensions.DataTableExtensions.CheckFileOccupying(saveFileName))
                {
                    snackbarMessageQueue.EnqueueEx("文件被占用,请关闭文件".TryFindResourceEx() + saveFileName);
                    return;
                }
                DataTable dataTable = Extensions.DataTableExtensions.ToDataTable<ProductShowInfoModel>(ProductInfoCollections);
                Extensions.DataTableExtensions.TableToExcel(dataTable, saveFileName);
                snackbarMessageQueue.EnqueueEx("导出成功.");
            }
        }
    }
}
