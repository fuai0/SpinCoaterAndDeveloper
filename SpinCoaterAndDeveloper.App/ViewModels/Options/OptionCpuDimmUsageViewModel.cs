using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LogServiceInterface;
using NPOI.OpenXmlFormats.Dml.Diagram;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using SkiaSharp;
using SpinCoaterAndDeveloper.Shared;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class OptionCpuDimmUsageViewModel : BindableBase
    {
        private ulong totalMemory = 0;
        private ObservableCollection<int> cpuObservableValues;
        private ObservableCollection<int> dimmObservableValues;
        private CancellationTokenSource cancellationTokenSource;

        private readonly ILogService logService;

        public Axis[] CpuUsageXAxes { get; set; }
        public Axis[] CpuUsageYAxes { get; set; }
        public Axis[] DimmUsageXAxes { get; set; }
        public Axis[] DimmUsageYAxes { get; set; }
        #region Binding
        private string _CPUType;
        public string CPUType
        {
            get { return _CPUType; }
            set { SetProperty(ref _CPUType, value); }
        }
        private int _CpuUsage;
        public int CpuUsage
        {
            get { return _CpuUsage; }
            set { SetProperty(ref _CpuUsage, value); }
        }
        private string _DimmType;
        public string DimmType
        {
            get { return _DimmType; }
            set { SetProperty(ref _DimmType, value); }
        }
        private int _DimmUsage;
        public int DimmUsage
        {
            get { return _DimmUsage; }
            set { SetProperty(ref _DimmUsage, value); }
        }
        #endregion

        public ObservableCollection<ISeries> CpuUsageSeries { get; set; } = new ObservableCollection<ISeries>();
        public ObservableCollection<ISeries> DimmUsageSeries { get; set; } = new ObservableCollection<ISeries>();
        public OptionCpuDimmUsageViewModel(IContainerProvider containerProvider)
        {
            this.logService = containerProvider.Resolve<ILogService>();

            CpuUsageXAxes = new Axis[] { new Axis { IsVisible = true, Name = null, MinLimit = 0, MaxLimit = 9, MinStep = 5, TextSize = 0, Padding = new LiveChartsCore.Drawing.Padding(0) } };
            CpuUsageYAxes = new Axis[] { new Axis { IsVisible = true, Name = null, MinLimit = 0, MaxLimit = 100, MinStep = 50, TextSize = 0, Padding = new LiveChartsCore.Drawing.Padding(0) } };
            DimmUsageXAxes = new Axis[] { new Axis { IsVisible = true, Name = null, MinLimit = 0, MaxLimit = 9, MinStep = 5, TextSize = 0, Padding = new LiveChartsCore.Drawing.Padding(0) } };
            DimmUsageYAxes = new Axis[] { new Axis { IsVisible = true, Name = null, MinLimit = 0, MaxLimit = 100, MinStep = 50, TextSize = 0, Padding = new LiveChartsCore.Drawing.Padding(0) } };

            cpuObservableValues = new ObservableCollection<int>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            CpuUsageSeries.Add(new LineSeries<int>()
            {
                Values = cpuObservableValues,
                GeometrySize = 0,
                Stroke = null,
            });
            dimmObservableValues = new ObservableCollection<int>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            DimmUsageSeries.Add(new LineSeries<int>()
            {
                Values = dimmObservableValues,
                GeometrySize = 0,
                Stroke = null,
            });

            cancellationTokenSource = new CancellationTokenSource();
            Task.Run(async () =>
            {
                GetCpuType();
                GetDimmType();
                PerformanceCounter cpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total", true);
                PerformanceCounter memoryCounter = new PerformanceCounter("Memory", "Available MBytes");

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        CpuUsage = Convert.ToInt16(Math.Round(cpuCounter.NextValue(), MidpointRounding.AwayFromZero));
                        cpuObservableValues.Add(CpuUsage);
                        cpuObservableValues.RemoveAt(0);
                        switch (CpuUsage)
                        {
                            case int x when (x >= 0 && x <= 25):
                                ((LineSeries<int>)CpuUsageSeries[0]).Fill = new SolidColorPaint(new SKColor(83, 172, 122, 150));
                                break;
                            case int x when (x > 25 && x <= 50):
                                ((LineSeries<int>)CpuUsageSeries[0]).Fill = new SolidColorPaint(new SKColor(84, 151, 193, 150));
                                break;
                            case int x when (x > 50 && x <= 75):
                                ((LineSeries<int>)CpuUsageSeries[0]).Fill = new SolidColorPaint(new SKColor(243, 150, 91, 150));
                                break;
                            case int x when (x > 75 && x <= 100):
                                ((LineSeries<int>)CpuUsageSeries[0]).Fill = new SolidColorPaint(new SKColor(228, 94, 105, 150));
                                break;
                            default:
                                break;
                        }

                        float availableMem = memoryCounter.NextValue();
                        if (totalMemory != 0)
                        {
                            DimmUsage = Convert.ToInt16(Math.Round((1 - availableMem / (totalMemory / 1048576)) * 100, MidpointRounding.AwayFromZero));
                        }
                        dimmObservableValues.Add(DimmUsage);
                        dimmObservableValues.RemoveAt(0);
                        switch (DimmUsage)
                        {
                            case int x when (x >= 0 && x <= 25):
                                ((LineSeries<int>)DimmUsageSeries[0]).Fill = new SolidColorPaint(new SKColor(83, 172, 122, 150));
                                break;
                            case int x when (x > 25 && x <= 50):
                                ((LineSeries<int>)DimmUsageSeries[0]).Fill = new SolidColorPaint(new SKColor(84, 151, 193, 150));
                                break;
                            case int x when (x > 50 && x <= 75):
                                ((LineSeries<int>)DimmUsageSeries[0]).Fill = new SolidColorPaint(new SKColor(243, 150, 91, 150));
                                break;
                            case int x when (x > 75 && x <= 100):
                                ((LineSeries<int>)DimmUsageSeries[0]).Fill = new SolidColorPaint(new SKColor(228, 94, 105, 150));
                                break;
                            default:
                                break;
                        }
                        if (GlobalValues.MachineStatus == FSMStateCode.Running && (CpuUsage >= 80 || DimmUsage >= 80))
                        {
                            logService.WriteLog(LogTypes.DB.ToString(), $@"CPU:{CpuUsage}%,DIMM:{DimmUsage}%", MessageDegree.INFO);
                        }
                    }
                    catch (TaskCanceledException) { }
                    catch (Exception ex)
                    {
                        logService.WriteLog(LogTypes.DB.ToString(), $@"刷新CPU/DIMM使用率线程异常:{ex.Message}", ex);
                    }
                    await Task.Delay(3000, cancellationTokenSource.Token);
                }
            }, cancellationTokenSource.Token);
        }

        private void GetCpuType()
        {
            ManagementObjectSearcher mos = new ManagementObjectSearcher("Select * from Win32_Processor");
            try
            {
                string cpuName = "";
                foreach (ManagementObject mo in mos.Get().Cast<ManagementObject>())
                {
                    cpuName = mo["Name"].ToString();
                }
                CPUType = cpuName;
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"刷新CPU类型异常:{ex.Message}", ex);
            }
            finally
            {
                mos.Dispose();
            }
        }

        private void GetDimmType()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Capacity, DeviceLocator FROM Win32_PhysicalMemory");
            try
            {
                string dimmDes = "";
                //获取厂商
                //ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Manufacturer FROM Win32_PhysicalMemory");
                //foreach (ManagementObject queryObj in searcher.Get().Cast<ManagementObject>())
                //{
                //    Console.WriteLine(queryObj["Manufacturer"]);
                //}
                foreach (ManagementObject queryObj in searcher.Get().Cast<ManagementObject>())
                {
                    dimmDes += queryObj["DeviceLocator"] + " " + ConvertBytesToReadable(queryObj["Capacity"]) + "\r\n";
                    totalMemory += Convert.ToUInt64(queryObj["Capacity"]);
                }
                DimmType = dimmDes.Substring(0, dimmDes.Length - 2);
            }
            catch (Exception ex)
            {
                logService.WriteLog(LogTypes.DB.ToString(), $@"刷新Dimm类型异常:{ex.Message}", ex);
            }
            finally
            {
                searcher.Dispose();
            }
        }
        private static string ConvertBytesToReadable(object bytes)
        {
            if (bytes == null) return "N/A";
            double size = Convert.ToDouble(bytes);
            if (size >= 1073741824) // GB
            {
                return Math.Round(size / 1073741824, 2) + " GB";
            }
            else if (size >= 1048576) // MB
            {
                return Math.Round(size / 1048576, 2) + " MB";
            }
            else // KB
            {
                return Math.Round(size / 1024, 2) + " KB";
            }
        }

        ~OptionCpuDimmUsageViewModel()
        {
            cancellationTokenSource?.Cancel();
        }
    }
}
