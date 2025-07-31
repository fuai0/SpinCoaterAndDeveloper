using DataBaseServiceInterface;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using LogServiceInterface;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using SkiaSharp;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class OptionUphViewModel : BindableBase
    {
        private ObservableCollection<ObservableValue> totalObservableValues;
        private ObservableCollection<ObservableValue> failObservableValues;
        private CancellationTokenSource cancellationTokenSource;

        private readonly IDataBaseService dataBaseService;
        private readonly ILogService logService;

        public Axis[] XAxis { get; set; }
        public Axis[] YAxis { get; set; }
        public ObservableCollection<ISeries> UphSeries { get; set; } = new ObservableCollection<ISeries>();
        public OptionUphViewModel(IContainerProvider containerProvider)
        {
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
            this.logService = containerProvider.Resolve<ILogService>();

            XAxis = new Axis[] { new Axis() {
                LabelsRotation = 45,
                IsVisible = true,
                TextSize = 12,
                MinStep = 1,
                Padding = new LiveChartsCore.Drawing.Padding(0),
                Labels = new List<string>() {"00-01", "01-02", "02-03", "03-04", "04-05", "05-06", "06-07", "07-08", "08-09", "09-10", "10-11", "11-12",
                                             "12-13", "13-14", "14-15", "15-16", "16-17", "17-18", "18-19", "19-20", "20-21", "21-22", "22-23", "23-24" }} };
            YAxis = new Axis[] { new Axis() {
                IsVisible = true,
                MinLimit = 0,
                Name = "UPH",
                MinStep = 1,
                NameTextSize = 14,
                TextSize = 12,
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
            UphSeries.Add(new ColumnSeries<ObservableValue>()
            {
                Name = "Total",
                Values = totalObservableValues,
                Fill = new LinearGradientPaint(new[] { new SKColor(102, 217, 56), new SKColor(76, 170, 233) }, new SKPoint(0.5f, 0), new SKPoint(0.5f, 1)),
            });
            failObservableValues = new ObservableCollection<ObservableValue>()
            {
                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
                new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d), new ObservableValue(0d),
            };
            UphSeries.Add(new LineSeries<ObservableValue, SVGPathGeometry>()
            {
                Name = "Fail",
                Values = failObservableValues,
                GeometrySize = 10,
                Stroke = new LinearGradientPaint(new[] { new SKColor(243, 150, 91), new SKColor(228, 95, 105) }) { StrokeThickness = 2 },
                GeometryStroke = new LinearGradientPaint(new[] { new SKColor(125, 114, 187), new SKColor(194, 72, 134) }) { StrokeThickness = 2 },
                GeometrySvg = SVGPoints.Star,
                Fill = null
            });

            cancellationTokenSource = new CancellationTokenSource();
            Task.Run(async () =>
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        DateTime startTime = DateTime.Now.Date;
                        DateTime endTime = DateTime.Now.Date.AddDays(1);
                        var totalCount = dataBaseService.Db.Queryable<ProduceInfoEntity>()
                            .Where(it => SqlFunc.Between(it.ProductStartTime, startTime, endTime))
                            .SplitTable(tabs => tabs.Take(1))
                            .Select(it => new { time = it.CreateTime, hour = it.ProductStartTime.Hour })
                            .MergeTable()
                            .GroupBy(it => new { it.hour })
                            .Select(it => new { count = SqlFunc.AggregateCount(it.time), time = it.hour })
                            .ToList();
                        List<bool> totalCountChangeList = new List<bool>() { false, false, false, false, false, false,
                                                                             false, false, false, false, false, false,
                                                                             false, false, false, false, false, false,
                                                                             false, false, false, false, false, false, };
                        foreach (var item in totalCount)
                        {
                            totalObservableValues[item.time].Value = item.count;
                            totalCountChangeList[item.time] = true;
                        }
                        for (int i = 0; i < 24; i++)
                        {
                            if (!totalCountChangeList[i])
                                totalObservableValues[i].Value = 0;
                        }

                        var failCount = dataBaseService.Db.Queryable<ProduceInfoEntity>()
                            .Where(it => SqlFunc.Between(it.ProductStartTime, startTime, endTime) && it.ProductResult == false)
                            .SplitTable(tabs => tabs.Take(1))
                            .Select(it => new { time = it.CreateTime, hour = it.ProductStartTime.Hour })
                            .MergeTable()
                            .GroupBy(it => new { it.hour })
                            .Select(it => new { count = SqlFunc.AggregateCount(it.time), time = it.hour })
                            .ToList();
                        List<bool> failCountChangeList = new List<bool>() { false, false, false, false, false, false,
                                                                            false, false, false, false, false, false,
                                                                            false, false, false, false, false, false,
                                                                            false, false, false, false, false, false, };
                        foreach (var item in failCount)
                        {
                            failObservableValues[item.time].Value = item.count;
                            failCountChangeList[item.time] = true;
                        }
                        for (int i = 0; i < 24; i++)
                        {
                            if (!failCountChangeList[i])
                                failObservableValues[i].Value = 0;
                        }
                    }
                    catch (TaskCanceledException) { }
                    catch (Exception ex)
                    {
                        logService.WriteLog(LogTypes.DB.ToString(), $@"刷新UPH线程异常:{ex.Message}", ex);
                    }
                    await Task.Delay(5000, cancellationTokenSource.Token);
                }
            }, cancellationTokenSource.Token);
        }

        ~OptionUphViewModel()
        {
            cancellationTokenSource?.Cancel();
        }
    }
}
