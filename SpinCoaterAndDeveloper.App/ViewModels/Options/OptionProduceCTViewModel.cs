using DataBaseServiceInterface;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LogServiceInterface;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using SkiaSharp;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class OptionProduceCTViewModel : BindableBase
    {
        private ObservableCollection<ObservableValue> observableValues;
        private CancellationTokenSource cancellationTokenSource;

        private readonly ILogService logService;
        private readonly IDataBaseService dataBaseService;

        public Axis[] XAxes { get; set; }
        public Axis[] YAxes { get; set; }
        public ObservableCollection<ISeries> ProduceCTSeries { get; set; } = new ObservableCollection<ISeries>();
        public OptionProduceCTViewModel(IContainerProvider containerProvider)
        {
            this.logService = containerProvider.Resolve<ILogService>();
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");

            XAxes = new Axis[] { new Axis {
                IsVisible = true,
                Name = null,
                MinStep = 5,
                TextSize = 0,
                Padding = new LiveChartsCore.Drawing.Padding(0), } };
            YAxes = new Axis[] { new Axis {
                IsVisible = true,
                Name = "CT(s)",
                MinLimit = 0,
                MinStep = 1,
                NameTextSize = 14,
                TextSize = 12,
                Labeler = x=> (x/1000).ToString("F1")+"s",
                Padding = new LiveChartsCore.Drawing.Padding(0) } };
            observableValues = new ObservableCollection<ObservableValue>()
            {
                new ObservableValue(0), new ObservableValue(0), new ObservableValue(0), new ObservableValue(0), new ObservableValue(0),
                new ObservableValue(0), new ObservableValue(0), new ObservableValue(0), new ObservableValue(0), new ObservableValue(0),
            };
            ProduceCTSeries.Add(new ColumnSeries<ObservableValue>()
            {
                Values = observableValues,
                Fill = new SolidColorPaint(new SKColor(76, 170, 233)),
                DataLabelsFormatter = x => (x.Model.Value / 1000)?.ToString("F1") + "s",
                DataLabelsPaint = new SolidColorPaint(new SKColor(0x48, 0x48, 0x48)),
                DataLabelsSize = 12,
            });

            cancellationTokenSource = new CancellationTokenSource();
            Task.Run(async () =>
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        var cts = dataBaseService.Db.Queryable<ProduceInfoEntity>().SplitTable(tabs => tabs.Take(2)).OrderBy(it => it.Id, SqlSugar.OrderByType.Desc).Take(10).ToList();
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            int i = 9;
                            foreach (var ct in cts)
                            {
                                observableValues[i].Value = ct.ProductCT;
                                i--;
                            }
                        });
                    }
                    catch (TaskCanceledException) { }
                    catch (Exception ex)
                    {
                        logService.WriteLog(LogTypes.DB.ToString(), $@"刷新最近10个产品CT线程异常:{ex.Message}", MessageDegree.INFO);
                    }
                    await Task.Delay(10000, cancellationTokenSource.Token);
                }
            }, cancellationTokenSource.Token);
        }

        ~OptionProduceCTViewModel()
        {
            cancellationTokenSource?.Cancel();
        }
    }
}
