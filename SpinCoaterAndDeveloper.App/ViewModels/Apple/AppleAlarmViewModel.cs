using AutoMapper;
using DataBaseServiceInterface;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.SkiaSharpView;
using LogServiceInterface;
using MaterialDesignThemes.Wpf;
using PermissionServiceInterface;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using SkiaSharp;
using SpinCoaterAndDeveloper.App.Common.Models;
using SpinCoaterAndDeveloper.App.Extensions;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using SpinCoaterAndDeveloper.Shared.Extensions;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class AppleAlarmViewModel : BindableBase, INavigationAware
    {
        private Dictionary<string, PieSeries<ObservableValue>> logErrorStatictisSeries = new Dictionary<string, PieSeries<ObservableValue>>();
        private Dictionary<string, StackedColumnSeries<ObservableValue>> logErrorPeriodStatictisSeries = new Dictionary<string, StackedColumnSeries<ObservableValue>>();

        private readonly IMapper mapper;
        private readonly ILogService logService;
        private readonly IDataBaseService dataBaseService;
        private readonly IEventAggregator eventAggregator;
        private readonly IPermissionService permissionService;
        private readonly ISnackbarMessageQueue snackbarMessageQueue;

        public Axis[] ErrorPeriodXAxes { get; set; }
        public Axis[] ErrorPeriodYAxes { get; set; }
        private CancellationTokenSource cancellationTokenSource;

        #region Binding
        private string _ErrorStartDate;
        public string ErrorStartDate
        {
            get { return _ErrorStartDate; }
            set { SetProperty(ref _ErrorStartDate, value); }
        }
        private string _ErrorStartTime;
        public string ErrorStartTime
        {
            get { return _ErrorStartTime; }
            set { SetProperty(ref _ErrorStartTime, value); }
        }
        private string _ErrorEndDate;
        public string ErrorEndDate
        {
            get { return _ErrorEndDate; }
            set { SetProperty(ref _ErrorEndDate, value); }
        }
        private string _ErrorEndTime;
        public string ErrorEndTime
        {
            get { return _ErrorEndTime; }
            set { SetProperty(ref _ErrorEndTime, value); }
        }
        private bool searchBtnEnable = true;
        public bool SearchBtnEnable
        {
            get { return searchBtnEnable; }
            set { SetProperty(ref searchBtnEnable, value); }
        }
        private string _ErrorFilter;
        public string ErrorFilter
        {
            get { return _ErrorFilter; }
            set { SetProperty(ref _ErrorFilter, value); }
        }
        #endregion

        public ObservableCollection<ISeries> ErrorStatisticPieChartSeries { get; set; } = new ObservableCollection<ISeries>();
        public ObservableCollection<ISeries> ErrorStatisticPeriodChartSeries { get; set; } = new ObservableCollection<ISeries>();
        public ObservableCollection<ErrorKeywodType> ErrorKeywords { get; set; } = new ObservableCollection<ErrorKeywodType>();
        public ObservableCollection<LogShowInfoModel> ErrorCollection { get; set; } = new ObservableCollection<LogShowInfoModel>();

        public AppleAlarmViewModel(IContainerProvider containerProvider)
        {
            this.mapper = containerProvider.Resolve<IMapper>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
            this.eventAggregator = containerProvider.Resolve<IEventAggregator>();
            this.permissionService = containerProvider.Resolve<IPermissionService>();
            this.snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();

            ErrorPeriodXAxes = new Axis[] { new Axis {
                LabelsRotation = 45,
                IsVisible= true,
                TextSize = 12,
                MinStep = 1,
                Padding = new Padding(0),
                Labels = new List<string>() {"00-01", "01-02", "02-03", "03-04", "04-05", "05-06", "06-07", "07-08", "08-09", "09-10", "10-11", "11-12",
                                             "12-13", "13-14", "14-15", "15-16", "16-17", "17-18", "18-19", "19-20", "20-21", "21-22", "22-23", "23-24" } } };
            ErrorPeriodYAxes = new Axis[] { new Axis {
                IsVisible= true,
                MinLimit=0,
                MinStep = 1,
                TextSize = 12,
                Padding = new Padding(0), } };
            var staticPropertyInfos = typeof(LogTypes).GetProperties(BindingFlags.Static | BindingFlags.Public);
            int outer = 0;
            foreach (var ststicProperty in staticPropertyInfos)
            {
                var attr = ststicProperty.GetCustomAttribute(typeof(LogStatisticAttr), false);
                if (attr != null && attr is LogStatisticAttr _attr)
                {
                    if (_attr.EnableStatistic && _attr.Category != default)
                    {
                        ErrorKeywords.Add(new ErrorKeywodType()
                        {
                            ErrorKeyword = _attr.Category.ToString(),
                            ErrorKeywordShowOnUI = _attr.Category.ToString().TryFindResourceEx()
                        });
                        var pieSeries = new PieSeries<ObservableValue>()
                        {
                            Name = _attr.Category.ToString().TryFindResourceEx(),
                            Values = new ObservableCollection<ObservableValue>() { new ObservableValue(0) },
                            InnerRadius = 20,
                            OuterRadiusOffset = outer,
                        };
                        outer += 20;
                        logErrorStatictisSeries.Add(_attr.Category.ToString(), pieSeries);
                        ErrorStatisticPieChartSeries.Add(pieSeries);
                        var stackedColumnSeries = new StackedColumnSeries<ObservableValue>()
                        {
                            Name = _attr.Category.ToString().TryFindResourceEx(),
                            Values = new ObservableCollection<ObservableValue>
                            {
                                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                            }
                        };
                        logErrorPeriodStatictisSeries.Add(_attr.Category.ToString(), stackedColumnSeries);
                        ErrorStatisticPeriodChartSeries.Add(stackedColumnSeries);
                    }
                }
            }

            CollectionView collectionView = CollectionViewSource.GetDefaultView(ErrorCollection) as CollectionView;
            collectionView.Filter = x =>
            {
                if (string.IsNullOrWhiteSpace(ErrorFilter)) return true;
                return (x as LogShowInfoModel).Message.Contains(ErrorFilter);
            };
        }

        private DelegateCommand<ErrorKeywodType> _SearchErrorCommand;
        public DelegateCommand<ErrorKeywodType> SearchErrorCommand =>
            _SearchErrorCommand ?? (_SearchErrorCommand = new DelegateCommand<ErrorKeywodType>(ExecuteSearchErrorCommand).ObservesCanExecute(() => SearchBtnEnable));

        async void ExecuteSearchErrorCommand(ErrorKeywodType parameter)
        {
            try
            {
                SearchBtnEnable = false;
                ErrorCollection.Clear();

                eventAggregator.UpdateLoadingEvent(true);
                var expable = Expressionable.Create<LogInfoEntity>();
                if (!string.IsNullOrEmpty(ErrorStartDate))
                {
                    DateTime startTime = Convert.ToDateTime(ErrorStartDate + (string.IsNullOrEmpty(ErrorStartTime) ? " 00:00:00" : $" {ErrorStartTime}:00"));
                    expable.And(it => it.Time > startTime);
                }
                if (!string.IsNullOrEmpty(ErrorEndDate))
                {
                    DateTime endTime = Convert.ToDateTime(ErrorEndDate + (string.IsNullOrEmpty(ErrorEndTime) ? " 00:00:00" : $" {ErrorEndTime}:00"));
                    expable.And(it => it.Time < endTime);
                }
                expable.AndIF(parameter != null, it => it.Keyword.Contains(parameter.ErrorKeyword));
                ErrorKeywords.ToList().ForEach(x => expable.OrIF(parameter == null, it => it.Keyword.Contains(x.ErrorKeyword)));
                var exp = expable.ToExpression();
                //默认最多查询一万条
                var result = await dataBaseService.Db.Queryable<LogInfoEntity>().Where(exp).OrderBy(it => it.Id, OrderByType.Desc).Take(10000).ToListAsync();
                mapper.Map(result, ErrorCollection);
                snackbarMessageQueue.EnqueueEx("查询成功");
            }
            catch (Exception ex)
            {
                snackbarMessageQueue.EnqueueEx("查询出错");
                logService.WriteLog($"报警查询出错:{ex.Message}", ex);
            }
            finally
            {
                eventAggregator.UpdateLoadingEvent(false);
                SearchBtnEnable = true;
            }
        }

        private DelegateCommand _ErrorResultFilterCommand;
        public DelegateCommand ErrorResultFilterCommand =>
            _ErrorResultFilterCommand ?? (_ErrorResultFilterCommand = new DelegateCommand(ExecuteErrorResultFilterCommand));

        void ExecuteErrorResultFilterCommand()
        {
            CollectionViewSource.GetDefaultView(ErrorCollection).Refresh();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}离开报警页面", MessageDegree.INFO);
            cancellationTokenSource?.Cancel();
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            logService.WriteLog(LogTypes.DB.ToString(), $@"用户{permissionService.CurrentUserName}进入报警页面", MessageDegree.INFO);
            cancellationTokenSource = new CancellationTokenSource();
            Task.Run(async () =>
            {
                DateTime startTime = DateTime.Now.Date;
                DateTime endTime = DateTime.Now.Date.AddDays(1);
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        foreach (var errorKeyword in ErrorKeywords)
                        {
                            var count = dataBaseService.Db.Queryable<LogInfoEntity>().Where(x => x.Keyword.Contains(errorKeyword.ErrorKeyword) && x.Time >= DateTime.Now.Date).Count();
                            if (((ObservableCollection<ObservableValue>)logErrorStatictisSeries[errorKeyword.ErrorKeyword].Values)[0].Value != count)
                            {
                                ((ObservableCollection<ObservableValue>)(logErrorStatictisSeries[errorKeyword.ErrorKeyword].Values))[0].Value = count;
                            }
                            Thread.Sleep(50);
                            var periodCount = dataBaseService.Db.Queryable<LogInfoEntity>()
                            .Where(it => SqlFunc.Between(it.Time, startTime, endTime) && it.Keyword.Contains(errorKeyword.ErrorKeyword))
                            .Select(it => new { keyword = it.Keyword, hour = it.Time.Hour })
                            .MergeTable()
                            .GroupBy(it => new { it.hour })
                            .Select(it => new { count = SqlFunc.AggregateCount(it.keyword), time = it.hour })
                            .ToList();
                            foreach (var item in periodCount)
                            {
                                if (((ObservableCollection<ObservableValue>)logErrorPeriodStatictisSeries[errorKeyword.ErrorKeyword].Values)[item.time].Value != item.count)
                                {
                                    ((ObservableCollection<ObservableValue>)logErrorPeriodStatictisSeries[errorKeyword.ErrorKeyword].Values)[item.time].Value = item.count;
                                }
                            }
                        }
                        await Task.Delay(5000, cancellationTokenSource.Token);
                    }
                    catch (TaskCanceledException) { }
                    catch (Exception ex)
                    {
                        logService.WriteLog(LogTypes.DB.ToString(), $@"错误统计刷新线程异常:{ex.Message}", ex);
                    }
                }
            }, cancellationTokenSource.Token);
        }
    }
    public class ErrorKeywodType
    {
        public string ErrorKeyword { get; set; }
        public string ErrorKeywordShowOnUI { get; set; }
    }
}
