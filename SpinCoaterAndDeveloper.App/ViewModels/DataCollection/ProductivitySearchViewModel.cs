using AutoMapper;
using DataBaseServiceInterface;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LogServiceInterface;
using MaterialDesignThemes.Wpf;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SpinCoaterAndDeveloper.App.Common.Models;
using SpinCoaterAndDeveloper.App.Extensions;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class ProductivitySearchViewModel : BindableBase
    {
        private readonly IDataBaseService dataBaseService;
        private readonly IMapper mapper;
        private readonly ILogService logService;
        private readonly ISnackbarMessageQueue snackbarMessageQueue;

        private ObservableCollection<ObservableValue> totalObservableValues;
        private ObservableCollection<ObservableValue> failObservableValues;

        private string productivityStartDate;

        public string ProductivityStartDate
        {
            get { return productivityStartDate; }
            set { SetProperty(ref productivityStartDate, value); }
        }
        private string productivityEndDate;

        public string ProductivityEndDate
        {
            get { return productivityEndDate; }
            set { SetProperty(ref productivityEndDate, value); }
        }

        public Axis[] ProductivityChartXAxis { get; set; }
        public Axis[] ProductivityChartYAxis { get; set; }
        public ObservableCollection<ISeries> ProductivityChartSeries { get; set; } = new ObservableCollection<ISeries>();
        public ObservableCollection<ProductivityShowModel> ProductivityListCollections { get; set; } = new ObservableCollection<ProductivityShowModel>();
        public ProductivitySearchViewModel(IContainerProvider containerProvider)
        {
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
            this.mapper = containerProvider.Resolve<IMapper>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();

            ProductivityChartXAxis = new Axis[] { new Axis {
                LabelsRotation = 45,
                IsVisible = true,
                TextSize = 14,
                MinStep = 1,
                Padding = new LiveChartsCore.Drawing.Padding(0),
                Labels = new List<string>() {"00-01", "01-02", "02-03", "03-04", "04-05", "05-06", "06-07", "07-08", "08-09", "09-10", "10-11", "11-12",
                                             "12-13", "13-14", "14-15", "15-16", "16-17", "17-18", "18-19", "19-20", "20-21", "21-22", "22-23", "23-24" } } };
            ProductivityChartYAxis = new Axis[] { new Axis {
                IsVisible = true,
                MinLimit = 0,
                TextSize = 14,
                MinStep = 1,
                Padding = new LiveChartsCore.Drawing.Padding(0), } };

            totalObservableValues = new ObservableCollection<ObservableValue>()
            {
                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
            };
            failObservableValues = new ObservableCollection<ObservableValue>()
            {
                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
            };
            ProductivityChartSeries.Add(new ColumnSeries<ObservableValue>()
            {
                Name = "Total",
                Values = totalObservableValues,
            });
            ProductivityChartSeries.Add(new ColumnSeries<ObservableValue>()
            {
                Name = "Fail",
                Values = failObservableValues,
            });
        }
        private DelegateCommand<string> searchProductivityCommand;
        public DelegateCommand<string> SearchProductivityCommand =>
            searchProductivityCommand ?? (searchProductivityCommand = new DelegateCommand<string>(ExecuteSearchProductivityCommand));

        void ExecuteSearchProductivityCommand(string parameter)
        {
            switch (parameter)
            {
                case "ListShow":
                    StatsProductivityList();
                    break;
                case "ChartShow":
                    StatsProductivityChart();
                    break;
                default:
                    break;
            }
        }
        private async void StatsProductivityList()
        {
            if (string.IsNullOrEmpty(ProductivityStartDate) || string.IsNullOrEmpty(ProductivityEndDate))
            {
                snackbarMessageQueue.EnqueueEx("请选择查询日期");
                return;
            }
            ProductivityListCollections.Clear();
            DateTime startTime = Convert.ToDateTime(ProductivityStartDate + " 00:00:00");
            DateTime endTime = Convert.ToDateTime(ProductivityEndDate + " 00:00:00");
            var resultTotal = await dataBaseService.Db.Queryable<ProduceInfoEntity>()
                .Where(it => SqlFunc.Between(it.ProductStartTime, startTime, endTime))
                .SplitTable(tabs => tabs.Take(6))
                .Select(it => new { code = it.ProductCode, hour = it.ProductStartTime.Hour })
                .MergeTable()
                .GroupBy(it => new { it.hour })
                .Select(it => new { count = SqlFunc.AggregateCount(it.code), time = it.hour })
                .ToListAsync();
            var ressutlFail = await dataBaseService.Db.Queryable<ProduceInfoEntity>()
                .Where(it => SqlFunc.Between(it.ProductStartTime, startTime, endTime) && it.ProductResult == false)
                .SplitTable(tabs => tabs.Take(6))
                .Select(it => new { code = it.ProductCode, hour = it.ProductStartTime.Hour })
                .MergeTable()
                .GroupBy(it => new { it.hour })
                .Select(it => new { count = SqlFunc.AggregateCount(it.code), time = it.hour })
                .ToListAsync();
            for (int i = 0; i < 24; i++)
            {
                var showItem = new ProductivityShowModel() { Time = $"{i}:00-{i + 1}:00" };
                foreach (var totalResult in resultTotal)
                {
                    if (totalResult.time == i)
                    {
                        showItem.TotalNums = totalResult.count;
                    }
                }
                foreach (var failResult in ressutlFail)
                {
                    if (failResult.time == i)
                    {
                        showItem.FailNums = failResult.count;
                    }
                }
                if (showItem.TotalNums != 0)
                {
                    showItem.Percentage = (((double)showItem.TotalNums - (double)showItem.FailNums) / (double)showItem.TotalNums * 100).ToString("f2") + "%";
                }
                else
                {
                    showItem.Percentage = "0%";
                }
                ProductivityListCollections.Add(showItem);
            }
        }
        private async void StatsProductivityChart()
        {
            if (string.IsNullOrEmpty(ProductivityStartDate) || string.IsNullOrEmpty(ProductivityEndDate))
            {
                snackbarMessageQueue.EnqueueEx("请选择查询日期");
                return;
            }
            DateTime startTime = Convert.ToDateTime(ProductivityStartDate + " 00:00:00");
            DateTime endTime = Convert.ToDateTime(ProductivityEndDate + " 00:00:00");
            var resultTotal = await dataBaseService.Db.Queryable<ProduceInfoEntity>()
                .Where(it => SqlFunc.Between(it.ProductStartTime, startTime, endTime))
                .SplitTable(tabs => tabs.Take(6))
                .Select(it => new { code = it.ProductCode, hour = it.ProductStartTime.Hour })
                .MergeTable()
                .GroupBy(it => new { it.hour })
                .Select(it => new { count = SqlFunc.AggregateCount(it.code), time = it.hour })
                .ToListAsync();
            var ressutlFail = await dataBaseService.Db.Queryable<ProduceInfoEntity>()
                .Where(it => SqlFunc.Between(it.ProductStartTime, startTime, endTime) && it.ProductResult == false)
                .SplitTable(tabs => tabs.Take(6))
                .Select(it => new { code = it.ProductCode, hour = it.ProductStartTime.Hour })
                .MergeTable()
                .GroupBy(it => new { it.hour })
                .Select(it => new { count = SqlFunc.AggregateCount(it.code), time = it.hour })
                .ToListAsync();
            foreach (var item in resultTotal)
            {
                totalObservableValues[item.time].Value = item.count;
            }
            foreach (var item in ressutlFail)
            {
                failObservableValues[item.time].Value = item.count;
            }
        }
    }
}
