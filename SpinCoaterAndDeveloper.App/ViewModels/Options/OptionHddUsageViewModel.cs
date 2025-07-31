using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LogServiceInterface;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using SkiaSharp;
using SpinCoaterAndDeveloper.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class OptionHddUsageViewModel : BindableBase
    {
        private CancellationTokenSource cancellationTokenSource;
        private readonly ILogService logService;

        public ObservableCollection<HDDUsageMode> HDDCollection { get; set; } = new ObservableCollection<HDDUsageMode>();
        public OptionHddUsageViewModel(IContainerProvider containerProvider)
        {
            this.logService = containerProvider.Resolve<ILogService>();
            cancellationTokenSource = new CancellationTokenSource();
            ;
            Task.Run(async () =>
            {
                GetHddType();
                List<PerformanceCounter> performanceCounters = new List<PerformanceCounter>();
                PerformanceCounterCategory diskCounter = new PerformanceCounterCategory("PhysicalDisk");
                string[] instanceNames = diskCounter.GetInstanceNames();

                foreach (var hdd in HDDCollection)
                {
                    for (int i = 0; i < instanceNames.Length; i++)
                    {
                        if (instanceNames[i].Contains(hdd.HddIndex))
                        {
                            performanceCounters.Add(new PerformanceCounter("PhysicalDisk", "% Idle Time", instanceNames[i]));
                        }
                    }
                }

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        for (int i = 0; i < performanceCounters.Count; i++)
                        {
                            HDDCollection[i].HddUsage = Convert.ToInt16(Math.Round(100 - performanceCounters[i].NextValue(), MidpointRounding.AwayFromZero));
                            HDDCollection[i].observableValues.Add(HDDCollection[i].HddUsage);
                            HDDCollection[i].observableValues.RemoveAt(0);

                            switch (HDDCollection[i].HddUsage)
                            {
                                case int x when (x >= 0 && x <= 25):
                                    ((LineSeries<int>)HDDCollection[i].HddUsageSeries[0]).Fill = new SolidColorPaint(new SKColor(83, 172, 122, 150));
                                    break;
                                case int x when (x > 25 && x <= 50):
                                    ((LineSeries<int>)HDDCollection[i].HddUsageSeries[0]).Fill = new SolidColorPaint(new SKColor(84, 151, 193, 150));
                                    break;
                                case int x when (x > 50 && x <= 75):
                                    ((LineSeries<int>)HDDCollection[i].HddUsageSeries[0]).Fill = new SolidColorPaint(new SKColor(243, 150, 91, 150));
                                    break;
                                case int x when (x > 75 && x <= 100):
                                    ((LineSeries<int>)HDDCollection[i].HddUsageSeries[0]).Fill = new SolidColorPaint(new SKColor(228, 94, 105, 150));
                                    break;
                                default:
                                    break;
                            }
                        }
                        if (GlobalValues.MachineStatus == FSMStateCode.Running)
                        {
                            var record = HDDCollection.Where(x => x.HddUsage >= 80).FirstOrDefault();
                            if (record != null)
                            {
                                string temp = "";
                                HDDCollection.ToList().ForEach(hdd => temp += $"({hdd.HddIndex})" + hdd.HddUsage + "% ");
                                logService.WriteLog(LogTypes.DB.ToString(), $@"HDD:{temp}", MessageDegree.INFO);
                            }
                        }
                    }
                    catch (TaskCanceledException) { }
                    catch (Exception ex)
                    {
                        logService.WriteLog(LogTypes.DB.ToString(), $@"刷新HDD使用率线程异常:{ex.Message}", ex);
                    }
                    await Task.Delay(3000, cancellationTokenSource.Token);
                }
            }, cancellationTokenSource.Token);
        }

        private void GetHddType()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
            try
            {
                foreach (ManagementObject queryObj in searcher.Get().Cast<ManagementObject>())
                {
                    //去除可移除磁盘
                    if (queryObj["MediaType"].ToString().Contains("Removable")) continue;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        HDDCollection.Add(new HDDUsageMode()
                        {
                            HddType = $"{queryObj["Model"]}: {(ulong)queryObj["Size"] / 1073741824}GB",
                            HddIndex = $"{queryObj["Index"]}",
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"获取HDD类型异常:{ex.Message}", ex);
            }
            finally
            {
                searcher.Dispose();
            }
        }
        ~OptionHddUsageViewModel()
        {
            cancellationTokenSource?.Cancel();
        }
    }

    public class HDDUsageMode : BindableBase
    {
        public ObservableCollection<int> observableValues;

        private int _HddUsage;
        public int HddUsage
        {
            get { return _HddUsage; }
            set { SetProperty(ref _HddUsage, value); }
        }
        private string _HddType;
        public string HddType
        {
            get { return _HddType; }
            set { SetProperty(ref _HddType, value); }
        }
        private string _HddIndex;
        public string HddIndex
        {
            get { return _HddIndex; }
            set { SetProperty(ref _HddIndex, value); }
        }
        public Axis[] HddUsageXAxes { get; set; }
        public Axis[] HddUsageYAxes { get; set; }
        public ObservableCollection<ISeries> HddUsageSeries { get; set; } = new ObservableCollection<ISeries>();
        public HDDUsageMode()
        {
            HddUsageXAxes = new Axis[] { new Axis { IsVisible = true, Name = null, MinLimit = 0, MaxLimit = 9, MinStep = 5, TextSize = 0, Padding = new LiveChartsCore.Drawing.Padding(0) } };
            HddUsageYAxes = new Axis[] { new Axis { IsVisible = true, Name = null, MinLimit = 0, MaxLimit = 100, MinStep = 50, TextSize = 0, Padding = new LiveChartsCore.Drawing.Padding(0) } };

            observableValues = new ObservableCollection<int>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            HddUsageSeries.Add(new LineSeries<int>()
            {
                Values = observableValues,
                GeometrySize = 0,
                Stroke = null,
            });
        }

    }
}
