using AutoMapper;
using DataBaseServiceInterface;
using LogServiceInterface;
using MaterialDesignThemes.Wpf;
using MotionCardServiceInterface;
using MotionControlActuation;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using SpinCoaterAndDeveloper.App.Common.Models;
using SpinCoaterAndDeveloper.App.Extensions;
using SpinCoaterAndDeveloper.Shared;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using SpinCoaterAndDeveloper.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class MCIODebugViewModel : BindableBase, INavigationAware
    {
        private static readonly object inputChangeLock = new object();
        private static readonly object outputChangeLock = new object();

        private readonly IDataBaseService dataBaseService;
        private readonly IMotionCardService motionCardService;
        private readonly IMapper mapper;
        private readonly ILogService logService;
        private readonly ISnackbarMessageQueue snackbarMessageQueue;

        private string _IOInputFilter;
        public string IOInputFilter
        {
            get { return _IOInputFilter; }
            set { SetProperty(ref _IOInputFilter, value); }
        }
        private string _IOOutputFilter;
        public string IOOutputFilter
        {
            get { return _IOOutputFilter; }
            set { SetProperty(ref _IOOutputFilter, value); }
        }

        private CancellationTokenSource cancellationTokenSource;
        public ObservableCollection<string> IOInputGroupCollection { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> IOOutputGroupCollection { get; set; } = new ObservableCollection<string>();

        public ObservableCollection<IOInputMonitorModel> IOInputCollection { get; set; } = new ObservableCollection<IOInputMonitorModel>();
        public ObservableCollection<IOOutputMonitorModel> IOOutputCollection { get; set; } = new ObservableCollection<IOOutputMonitorModel>();

        public MCIODebugViewModel(IContainerProvider containerProvider)
        {
            this.dataBaseService = containerProvider.Resolve<IDataBaseService>("MainDb");
            this.motionCardService = containerProvider.Resolve<IMotionCardService>();
            this.mapper = containerProvider.Resolve<IMapper>();
            this.logService = containerProvider.Resolve<ILogService>();
            this.snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();

            CollectionView ioInputCollectionView = CollectionViewSource.GetDefaultView(IOInputCollection) as CollectionView;
            ioInputCollectionView.Filter = x =>
            {
                if (string.IsNullOrWhiteSpace(IOInputFilter)) return true;
                return (x as IOInputMonitorModel).ShowOnUIName.Contains(IOInputFilter);
            };
            CollectionView ioOutputCollectionView = CollectionViewSource.GetDefaultView(IOOutputCollection) as CollectionView;
            ioOutputCollectionView.Filter = x =>
            {
                if (string.IsNullOrWhiteSpace(IOOutputFilter)) return true;
                return (x as IOOutputMonitorModel).ShowOnUIName.Contains(IOOutputFilter);
            };
        }
        private DelegateCommand _IOOutputFilterCommand;
        public DelegateCommand IOOutputFilterCommand =>
            _IOOutputFilterCommand ?? (_IOOutputFilterCommand = new DelegateCommand(ExecuteIOOutputFilterCommand));

        void ExecuteIOOutputFilterCommand()
        {
            CollectionViewSource.GetDefaultView(IOOutputCollection).Refresh();
        }
        private DelegateCommand _IOInputFilterCommand;
        public DelegateCommand IOInputFilterCommand =>
            _IOInputFilterCommand ?? (_IOInputFilterCommand = new DelegateCommand(ExecuteIOInputFilterCommand));

        void ExecuteIOInputFilterCommand()
        {
            CollectionViewSource.GetDefaultView(IOInputCollection).Refresh();
        }

        private DelegateCommand<string> _IOInputGroupChangedCommand;
        public DelegateCommand<string> IOInputGroupChangedCommand =>
            _IOInputGroupChangedCommand ?? (_IOInputGroupChangedCommand = new DelegateCommand<string>(ExecuteIOInputGroupChangedCommand));

        void ExecuteIOInputGroupChangedCommand(string parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter))
            {
                return;
            }
            var input = dataBaseService.Db.Queryable<IOInputInfoEntity>().Where(x => x.Group == parameter).OrderBy(x => x.Number).OrderBy(x => x.Id).ToList();
            lock (inputChangeLock)
            {
                IOInputCollection.Clear();
                mapper.Map(input, IOInputCollection);
            }
            snackbarMessageQueue.EnqueueEx("筛选成功");
        }

        private DelegateCommand<string> _IOOutputGroupChangedCommand;
        public DelegateCommand<string> IOOutputGroupChangedCommand =>
            _IOOutputGroupChangedCommand ?? (_IOOutputGroupChangedCommand = new DelegateCommand<string>(ExecuteIOOutputGroupChangedCommand));

        void ExecuteIOOutputGroupChangedCommand(string parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter))
            {
                return;
            }
            var output = dataBaseService.Db.Queryable<IOOutputInfoEntity>().Where(x => x.Group == parameter).OrderBy(x => x.Number).OrderBy(x => x.Id).ToList();
            lock (outputChangeLock)
            {
                IOOutputCollection.Clear();
                mapper.Map(output, IOOutputCollection);
            }
        }

        private DelegateCommand _ShowAllInputCommand;
        public DelegateCommand ShowAllInputCommand =>
            _ShowAllInputCommand ?? (_ShowAllInputCommand = new DelegateCommand(ExecuteShowAllInputCommand));

        void ExecuteShowAllInputCommand()
        {
            IOInputFilter = "";
            lock (inputChangeLock)
            {
                GetIOInputInfo();
            }
            IOInputGroupCollection.Clear();
            GetInputGroup();
            snackbarMessageQueue.EnqueueEx("显示所有成功");
        }

        private DelegateCommand _ShowAllOutputCommand;
        public DelegateCommand ShowAllOutputCommand =>
            _ShowAllOutputCommand ?? (_ShowAllOutputCommand = new DelegateCommand(ExecuteShowAllOutputCommand));

        void ExecuteShowAllOutputCommand()
        {
            IOOutputFilter = "";
            lock (outputChangeLock)
            {
                GetOutputInfo();
            }
            IOOutputGroupCollection.Clear();
            GetOutputGroup();
            snackbarMessageQueue.EnqueueEx("显示所有成功");
        }

        private DelegateCommand<IOOutputMonitorModel> _SetOutputStsCommand;
        public DelegateCommand<IOOutputMonitorModel> SetOutputStsCommand =>
            _SetOutputStsCommand ?? (_SetOutputStsCommand = new DelegateCommand<IOOutputMonitorModel>(ExecuteSetOutputStsCommand));

        void ExecuteSetOutputStsCommand(IOOutputMonitorModel parameter)
        {
            motionCardService.SetOuputStsEx(parameter.Name, !parameter.Status);
        }
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            IOInputGroupCollection.Clear();
            GetInputGroup();
            IOOutputGroupCollection.Clear();
            GetOutputGroup();

            GetIOInputInfo();
            GetOutputInfo();
            //启动刷新线程
            cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    Thread.Sleep(200);
                    try
                    {
                        lock (inputChangeLock)
                        {
                            foreach (var input in IOInputCollection)
                            {
                                input.Status = MotionControlResource.IOInputResource[input.Name].Status;
                            }
                        }
                        lock (outputChangeLock)
                        {
                            foreach (var output in IOOutputCollection)
                            {
                                output.Status = MotionControlResource.IOOutputResource[output.Name].Status;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logService.WriteLog(LogTypes.DB.ToString(), $@"点位监控页面刷新点位状态线程异常:{ex.Message}", ex);
                    }
                }
            }, cancellationTokenSource.Token);
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            cancellationTokenSource?.Cancel();
        }

        private void GetInputGroup()
        {
            var inputGroup = dataBaseService.Db.Queryable<IOInputInfoEntity>().Distinct().Select(x => x.Group).ToList();
            inputGroup.ForEach(x => { if (!string.IsNullOrEmpty(x) && !IOInputGroupCollection.Contains(x)) IOInputGroupCollection.Add(x); });
        }

        private void GetOutputGroup()
        {
            var outputGroup = dataBaseService.Db.Queryable<IOOutputInfoEntity>().Distinct().Select(x => x.Group).ToList();
            outputGroup.ForEach(x => { if (!string.IsNullOrWhiteSpace(x) && !IOOutputGroupCollection.Contains(x)) IOOutputGroupCollection.Add(x); });
        }

        private void GetIOInputInfo()
        {
            IOInputCollection.Clear();
            var inputMonitor = dataBaseService.Db.Queryable<IOInputInfoEntity>().OrderBy(x => x.Number).OrderBy(x => x.Id).ToList();
            mapper.Map(inputMonitor, IOInputCollection);
        }

        private void GetOutputInfo()
        {
            IOOutputCollection.Clear();
            var outputMonitor = dataBaseService.Db.Queryable<IOOutputInfoEntity>().OrderBy(x => x.Number).OrderBy(x => x.Id).ToList();
            mapper.Map(outputMonitor, IOOutputCollection);
        }
    }
}
