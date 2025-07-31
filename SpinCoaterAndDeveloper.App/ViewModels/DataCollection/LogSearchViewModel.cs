using AutoMapper;
using DataBaseServiceInterface;
using K4os.Compression.LZ4;
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
using System.Windows.Data;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class LogSearchViewModel : BindableBase
    {
        private readonly IDataBaseService dataBaseService;
        private readonly IMapper mapper;
        private readonly IEventAggregator eventAggregator;
        private readonly ILogService logService;
        private readonly ISnackbarMessageQueue snackbarMessageQueue;

        public ObservableCollection<LogShowInfoModel> LogInfoCollections { get; set; } = new ObservableCollection<LogShowInfoModel>();
        #region DataBinding
        private bool searchBtnEnable = true;
        public bool SearchBtnEnable
        {
            get { return searchBtnEnable; }
            set { SetProperty(ref searchBtnEnable, value); }
        }
        private string logStartDate;
        public string LogStartDate
        {
            get { return logStartDate; }
            set { SetProperty(ref logStartDate, value); }
        }
        private string logStartTime;
        public string LogStartTime
        {
            get { return logStartTime; }
            set { SetProperty(ref logStartTime, value); }
        }
        private string logEndDate;
        public string LogEndDate
        {
            get { return logEndDate; }
            set { SetProperty(ref logEndDate, value); }
        }
        private string logEndTime;
        public string LogEndTime
        {
            get { return logEndTime; }
            set { SetProperty(ref logEndTime, value); }
        }
        private string logLevel;
        public string LogLevel
        {
            get { return logLevel; }
            set { SetProperty(ref logLevel, value); }
        }
        private string logNums;
        public string LogNums
        {
            get { return logNums; }
            set { SetProperty(ref logNums, value); }
        }
        private string _LogFilter;
        public string LogFilter
        {
            get { return _LogFilter; }
            set { SetProperty(ref _LogFilter, value); }
        }
        #endregion
        public LogSearchViewModel(IContainerProvider containerProvider)
        {
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
            this.mapper = containerProvider.Resolve<IMapper>();
            this.eventAggregator = containerProvider.Resolve<IEventAggregator>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();

            CollectionView collectionView = CollectionViewSource.GetDefaultView(LogInfoCollections) as CollectionView;
            collectionView.Filter = x =>
            {
                if (string.IsNullOrWhiteSpace(LogFilter)) return true;
                return (x as LogShowInfoModel).Message.Contains(LogFilter);
            };
        }
        //查询日志
        private DelegateCommand searchLogCommand;
        public DelegateCommand SearchLogCommand =>
            searchLogCommand ?? (searchLogCommand = new DelegateCommand(ExecuteSearchLogCommand).ObservesCanExecute(() => SearchBtnEnable));

        void ExecuteSearchLogCommand()
        {
            try
            {
                SearchBtnEnable = false;
                LogInfoCollections.Clear();

                eventAggregator.UpdateLoadingEvent(true);
                var expable = Expressionable.Create<LogInfoEntity>();
                if (!string.IsNullOrEmpty(LogStartDate))
                {
                    DateTime startTime = Convert.ToDateTime(LogStartDate + (string.IsNullOrEmpty(LogStartTime) ? " 00:00:00" : $" {LogStartTime}:00"));
                    expable.And(it => it.Time > startTime);
                }
                if (!string.IsNullOrEmpty(LogEndDate))
                {
                    DateTime endTime = Convert.ToDateTime(LogEndDate + (string.IsNullOrEmpty(LogEndTime) ? " 00:00:00" : $" {LogEndTime}:00"));
                    expable.And(it => it.Time < endTime);
                }
                switch (logLevel)
                {
                    case "ALL":
                        break;
                    default:
                        expable.AndIF(LogLevel != null, it => it.Level == LogLevel);
                        break;
                }

                var exp = expable.ToExpression();

                Task.Run(async () =>
                {
                    List<LogInfoEntity> result = new List<LogInfoEntity>();
                    switch (LogNums)
                    {
                        case "100":
                            result = await dataBaseService.Db.Queryable<LogInfoEntity>().Where(exp).OrderBy(it => it.Id, SqlSugar.OrderByType.Desc).Take(100).ToListAsync();
                            break;
                        case "1000":
                            result = await dataBaseService.Db.Queryable<LogInfoEntity>().Where(exp).OrderBy(it => it.Id, SqlSugar.OrderByType.Desc).Take(1000).ToListAsync();
                            break;
                        case "10000":
                        default:
                            result = await dataBaseService.Db.Queryable<LogInfoEntity>().Where(exp).OrderBy(it => it.Id, SqlSugar.OrderByType.Desc).Take(10000).ToListAsync();
                            break;
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        mapper.Map<List<LogInfoEntity>, ObservableCollection<LogShowInfoModel>>(result, LogInfoCollections);
                    });
                });
                snackbarMessageQueue.EnqueueEx("查询成功");
            }
            catch (Exception ex)
            {
                snackbarMessageQueue.EnqueueEx("查询出错");
                logService.WriteLog($"日志查询出错:{ex.Message}", ex);
            }
            finally
            {
                eventAggregator.UpdateLoadingEvent(false);
                SearchBtnEnable = true;
            }
        }
        //日志导出
        private DelegateCommand saveDataToExcelCommand;
        public DelegateCommand SaveDataToExcelCommand =>
            saveDataToExcelCommand ?? (saveDataToExcelCommand = new DelegateCommand(ExecuteSaveDataToExcelCommand));

        void ExecuteSaveDataToExcelCommand()
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
                    snackbarMessageQueue.EnqueueEx("文件被占用,请关闭文件" + saveFileName);
                    return;
                }
                DataTable dataTable = Extensions.DataTableExtensions.ToDataTable<LogShowInfoModel>(LogInfoCollections);
                Extensions.DataTableExtensions.TableToExcel(dataTable, saveFileName);
                snackbarMessageQueue.EnqueueEx("导出成功.");
            }
        }

        private DelegateCommand _LogResultFilterCommand;
        public DelegateCommand LogResultFilterCommand =>
            _LogResultFilterCommand ?? (_LogResultFilterCommand = new DelegateCommand(ExecuteLogResultFilterCommand));

        void ExecuteLogResultFilterCommand()
        {
            CollectionViewSource.GetDefaultView(LogInfoCollections).Refresh();
        }
    }
}
