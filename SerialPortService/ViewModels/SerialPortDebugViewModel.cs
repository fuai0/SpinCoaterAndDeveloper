using MaterialDesignThemes.Wpf;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using SerialPortService.Common.Model;
using SerialPortService.Service;
using SerialPortServiceInterface;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using Example;

namespace SerialPortService.ViewModels
{
    public class SerialPortDebugViewModel : BindableBase, INavigationAware
    {
        public IContainerProvider ContainerProvider { get; }
        public ISnackbarMessageQueue snackbarMessageQueue { get; set; }
        public ObservableCollection<SerialPortDebugModel> SerialPortCfgCollection { get; set; }

        public SerialPortDebugViewModel(IContainerProvider containerProvider)
        {
            SerialPortCfgCollection = new ObservableCollection<SerialPortDebugModel>();
            ContainerProvider = containerProvider;
            snackbarMessageQueue = containerProvider.Resolve<ISnackbarMessageQueue>();
        }
        private DelegateCommand<SerialPortDebugModel> openCommand;
        public DelegateCommand<SerialPortDebugModel> OpenCommand =>
            openCommand ?? (openCommand = new DelegateCommand<SerialPortDebugModel>(ExecuteOpenCommand));

        void ExecuteOpenCommand(SerialPortDebugModel parameter)
        {
            if (parameter.serialPort.Init(parameter.PortName, parameter.BaudRate, parameter.Parity, parameter.StopBits, parameter.DataBits, parameter.Timeout))
            {
                parameter.ConnectSuccess = true;
                parameter.OpenButtonIsEnabled = false;
                parameter.CloseButtonIsEnabled = true;
                snackbarMessageQueue.Enqueue($"{parameter.IocDiName}   {Application.Current.TryFindResource("SerialPort_Open")}{Application.Current.TryFindResource("SerialPort_Successful")}");
                return;
            }
            snackbarMessageQueue.Enqueue($"{parameter.IocDiName}   {Application.Current.TryFindResource("SerialPort_Open")}{Application.Current.TryFindResource("SerialPort_Fail")}");
        }

        private DelegateCommand<SerialPortDebugModel> closeCommand;
        public DelegateCommand<SerialPortDebugModel> CloseCommand =>
            closeCommand ?? (closeCommand = new DelegateCommand<SerialPortDebugModel>(ExecuteCloseCommand));

        void ExecuteCloseCommand(SerialPortDebugModel parameter)
        {
            parameter.serialPort.Close();
            parameter.ConnectSuccess = false;
            parameter.OpenButtonIsEnabled = true;
            parameter.CloseButtonIsEnabled = false;
            snackbarMessageQueue.Enqueue($"{parameter.IocDiName}   {Application.Current.TryFindResource("SerialPort_Close")}{Application.Current.TryFindResource("SerialPort_Successful")}");
        }

        private DelegateCommand<SerialPortDebugModel> saveCommand;
        public DelegateCommand<SerialPortDebugModel> SaveCommand =>
            saveCommand ?? (saveCommand = new DelegateCommand<SerialPortDebugModel>(ExecuteSaveCommand));

        void ExecuteSaveCommand(SerialPortDebugModel parameter)
        {
            ServiceSerialPortConfigurationStore store = new ServiceSerialPortConfigurationStore();
            store.SaveCfg(parameter.IocDiName, parameter.PortName, parameter.BaudRate, parameter.Parity, parameter.StopBits, parameter.DataBits, parameter.Timeout);
            snackbarMessageQueue.Enqueue($"{parameter.IocDiName}   {Application.Current.TryFindResource("SerialPort_SaveSetting")}{Application.Current.TryFindResource("SerialPort_Successful")}");
        }

        private DelegateCommand<SerialPortDebugModel> sendCommand;
        public DelegateCommand<SerialPortDebugModel> SendCommand =>
            sendCommand ?? (sendCommand = new DelegateCommand<SerialPortDebugModel>(ExecuteSendCommand));

        void ExecuteSendCommand(SerialPortDebugModel parameter)
        {
            parameter.serialPort.Write(parameter.SendDataTextBox);
            parameter.SerialPortDataModelCollection.Add(new SerialPortDataModel() { Time = DateTime.Now, Type = Common.Model.Type.Write, Data = parameter.SendDataTextBox });
        }


        private DelegateCommand<SerialPortDebugModel> clearCommand;
        public DelegateCommand<SerialPortDebugModel> ClearCommand =>
            clearCommand ?? (clearCommand = new DelegateCommand<SerialPortDebugModel>(ExecuteClearCommand));

        void ExecuteClearCommand(SerialPortDebugModel parameter)
        {
            parameter.SerialPortDataModelCollection.Clear();
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            //多语言切换
            List<ResourceDictionary> dictionaryList = new List<ResourceDictionary>();
            foreach (ResourceDictionary dictionary in Application.Current.Resources.MergedDictionaries)
            {
                if (dictionary.Source != null)
                {
                    dictionaryList.Add(dictionary);
                }
            }
            string requestedCulture = "";
            switch (System.Threading.Thread.CurrentThread.CurrentUICulture.ToString())
            {
                case "zh-CN":
                    requestedCulture = "pack://application:,,,/SerialPortService;component/Resources/zh-cn.xaml";
                    break;
                case "en-US":
                    requestedCulture = "pack://application:,,,/SerialPortService;component/Resources/en-us.xaml";
                    break;
                default:
                    break;
            }
            ResourceDictionary resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString.Equals(requestedCulture));
            Application.Current.Resources.MergedDictionaries.Remove(resourceDictionary);
            Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);

            SerialPortCfgCollection.Clear();
            SerialPortConfig SerialPortConfig = new SerialPortConfig();
            var services = SerialPortConfig.LoadConfigFromFile();
            foreach (ServiceConfigurationElement service in services.Services)
            {
                SerialPortDebugModel temp = new SerialPortDebugModel(ContainerProvider, ContainerProvider.Resolve<ISerialPort>(service.IocName)) { IocDiName = service.IocName, PortName = service.PortName, BaudRate = service.BaudRate, Parity = service.Parity, StopBits = service.StopBits, DataBits = service.DataBits, Timeout = service.Timeout };
                temp.serialPort.DataReceivedEvent += SerialPort_DataReceivedEvent;
                temp.serialPort.ErrordEvent += SerialPort_ErrordEvent;
                SerialPortCfgCollection.Add(temp);
                if(temp.serialPort.IsConnected)
                {
                    temp.OpenButtonIsEnabled = false;
                    temp.CloseButtonIsEnabled = true;
                }
                else
                {
                    temp.OpenButtonIsEnabled = true;
                    temp.CloseButtonIsEnabled = false;
                }
            }
        }

        private void SerialPort_ErrordEvent(string name,string obj)
        {
            foreach(var cfg in SerialPortCfgCollection)
            {
                if(cfg.serialPort.PortName == name)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        cfg.SerialPortDataModelCollection.Add(new SerialPortDataModel() { Time = DateTime.Now, Type = Common.Model.Type.Error, Data = obj });
                    }));
                }
            }
        }

        private void SerialPort_DataReceivedEvent(string name, string obj)
        {
            foreach (var cfg in SerialPortCfgCollection)
            {
                if (cfg.serialPort.PortName == name)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        cfg.SerialPortDataModelCollection.Add(new SerialPortDataModel() { Time = DateTime.Now, Type = Common.Model.Type.Read, Data = obj });
                    }));
                }
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            foreach (var temp in SerialPortCfgCollection)
            {
                temp.serialPort.DataReceivedEvent -= SerialPort_DataReceivedEvent;
                temp.serialPort.ErrordEvent -= SerialPort_ErrordEvent;
            }
        }
    }
}