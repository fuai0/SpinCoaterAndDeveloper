using AutoMapper;
using DataBaseServiceInterface;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LogServiceInterface;
using MaterialDesignThemes.Wpf;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SkiaSharp;
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
    public class ProductCTSearchViewModel : BindableBase
    {
        private readonly IDataBaseService dataBaseService;
        private readonly IMapper mapper;
        private readonly ISnackbarMessageQueue snackbarMessageQueue;
        private readonly ILogService logService;


        private ObservableCollection<ObservableValue> observableValues;
        public Axis[] ProductCTXAxes { get; set; }
        public Axis[] ProductCTYAxes { get; set; }

        private string productCTStartDate;

        public string ProductCTStartDate
        {
            get { return productCTStartDate; }
            set { SetProperty(ref productCTStartDate, value); }
        }
        private string productCTEndDate;

        public string ProductCTEndDate
        {
            get { return productCTEndDate; }
            set { SetProperty(ref productCTEndDate, value); }
        }

        public ObservableCollection<ISeries> ProductCTChartSeries { get; set; } = new ObservableCollection<ISeries>();
        public ObservableCollection<ProductCTShowModel> ProductCTCollections { get; set; } = new ObservableCollection<ProductCTShowModel>();
        public ProductCTSearchViewModel(IContainerProvider containerProvider)
        {
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
            this.mapper = containerProvider.Resolve<IMapper>();
            this.snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();
            this.logService = containerProvider.Resolve<ILogService>();

            ProductCTXAxes = new Axis[] { new Axis {
            LabelsRotation=45,
            IsVisible = true,
            TextSize = 14,
            MinStep = 1,
            Padding = new LiveChartsCore.Drawing.Padding(0),
            Labels = new List<string>() {"00-01", "01-02", "02-03", "03-04", "04-05", "05-06", "06-07", "07-08", "08-09", "09-10", "10-11", "11-12",
                                         "12-13", "13-14", "14-15", "15-16", "16-17", "17-18", "18-19", "19-20", "20-21", "21-22", "22-23", "23-24" } } };
            ProductCTYAxes = new Axis[] { new Axis {
            IsVisible = true,
            MinLimit = 0,
            Name = "CT(s)",
            NameTextSize = 14,
            MinStep = 1,
            Labeler = x=> (x/1000).ToString("F1")+"s",
            TextSize = 14,
            Padding = new LiveChartsCore.Drawing.Padding(0) } };
            observableValues = new ObservableCollection<ObservableValue>()
            {
                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
            };
            ProductCTChartSeries.Add(new ColumnSeries<ObservableValue>()
            {
                Values = observableValues,
                Fill = new SolidColorPaint(new SKColor(76, 170, 233)),
                DataLabelsFormatter = x => (x.Model.Value / 1000)?.ToString("F1") + "s",
                DataLabelsPaint = new SolidColorPaint(new SKColor(0x48, 0x48, 0x48)),
                DataLabelsSize = 14,
            });
        }
        private DelegateCommand<string> searchProductCTCommand;
        public DelegateCommand<string> SearchProductCTCommand =>
            searchProductCTCommand ?? (searchProductCTCommand = new DelegateCommand<string>(ExecuteSearchProductCTCommand));

        void ExecuteSearchProductCTCommand(string parameter)
        {
            switch (parameter)
            {
                case "ListShow":
                    StatsProductCTList();
                    break;
                case "ChartShow":
                    StatsProductCTChart();
                    break;
                default:
                    break;
            }
        }
        private async void StatsProductCTList()
        {
            if (string.IsNullOrEmpty(ProductCTStartDate) || string.IsNullOrEmpty(ProductCTEndDate))
            {
                snackbarMessageQueue.EnqueueEx("请选择查询日期");
                return;
            }
            ProductCTCollections.Clear();
            DateTime startTime = Convert.ToDateTime(ProductCTStartDate + " 00:00:00");
            DateTime endTime = Convert.ToDateTime(ProductCTEndDate + " 00:00:00");
            var result = await dataBaseService.Db.Queryable<ProduceInfoEntity>()
                .Where(it => SqlFunc.Between(it.ProductStartTime, startTime, endTime) && it.ProductResult == true)
                .SplitTable(tabs => tabs.Take(6))
                .Select(it => new { code = it.ProductCode, hour = it.ProductStartTime.Hour, ct = it.ProductCT })
                .MergeTable()
                .GroupBy(it => new { it.hour })
                .Select(it => new { count = SqlFunc.AggregateCount(it.code), avg = SqlFunc.AggregateAvg(it.ct), time = it.hour })
                .ToListAsync();
            for (int i = 0; i < 24; i++)
            {
                var showItem = new ProductCTShowModel() { Time = $"{i}:00-{i + 1}:00" };
                foreach (var item in result)
                {
                    if (item.time == i)
                    {
                        showItem.Nums = item.count;
                        showItem.Avg = item.avg;
                    }
                }
                ProductCTCollections.Add(showItem);
            }
        }
        private async void StatsProductCTChart()
        {
            if (string.IsNullOrEmpty(ProductCTStartDate) || string.IsNullOrEmpty(ProductCTEndDate))
            {
                snackbarMessageQueue.EnqueueEx("请选择查询日期");
                return;
            }
            DateTime startTime = Convert.ToDateTime(ProductCTStartDate + " 00:00:00");
            DateTime endTime = Convert.ToDateTime(ProductCTEndDate + " 00:00:00");
            var result = await dataBaseService.Db.Queryable<ProduceInfoEntity>()
                .Where(it => SqlFunc.Between(it.ProductStartTime, startTime, endTime) && it.ProductResult == true)
                .SplitTable(tabs => tabs.Take(6))
                .Select(it => new { code = it.ProductCode, hour = it.ProductStartTime.Hour, ct = it.ProductCT })
                .MergeTable()
                .GroupBy(it => new { it.hour })
                .Select(it => new { count = SqlFunc.AggregateCount(it.code), avg = SqlFunc.AggregateAvg(it.ct), time = it.hour })
                .ToListAsync();
            foreach (var item in result)
            {
                observableValues[item.time].Value = (double)item.avg;
            }
        }
    }
}
