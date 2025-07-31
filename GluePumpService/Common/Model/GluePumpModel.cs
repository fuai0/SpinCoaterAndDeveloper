using Prism.Mvvm;
using GluePumpService.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Windows;

namespace GluePumpService.Common.Model
{
    public class GluePumpModel : BindableBase
    {
        #region 设备配置属性
        private string _iocDiName;
        public string IocDiName
        {
            get => _iocDiName;
            set => SetProperty(ref _iocDiName, value);
        }

        private string _portName;
        public string PortName
        {
            get => _portName;
            set => SetProperty(ref _portName, value);
        }

        private int _baudRate = 9600;
        public int BaudRate
        {
            get => _baudRate;
            set => SetProperty(ref _baudRate, value);
        }

        private Parity _parity = Parity.None;
        public Parity Parity
        {
            get => _parity;
            set => SetProperty(ref _parity, value);
        }

        private StopBits _stopBits = StopBits.One;
        public StopBits StopBits
        {
            get => _stopBits;
            set => SetProperty(ref _stopBits, value);
        }

        private int _dataBits = 8;
        public int DataBits
        {
            get => _dataBits;
            set => SetProperty(ref _dataBits, value);
        }

        private int _timeout = 1000;
        public int Timeout
        {
            get => _timeout;
            set => SetProperty(ref _timeout, value);
        }

        private byte _controllerId = 0;
        public byte ControllerId
        {
            get => _controllerId;
            set => SetProperty(ref _controllerId, value);
        }

        private int _selectedRecipe = 0;
        public int SelectedRecipe
        {
            get => _selectedRecipe;
            set => SetProperty(ref _selectedRecipe, value);
        }

        public ObservableCollection<string> SerialPortPortNameCollection { get; set; }
        public ObservableCollection<int> SerialPortBaudRateCollection { get; set; }
        public ObservableCollection<Parity> SerialPortParityCollection { get; set; }
        public ObservableCollection<StopBits> SerialPortStopBitsCollection { get; set; }
        public ObservableCollection<int> SerialPortDataBitsCollection { get; set; }
        public ObservableCollection<int> RecipeNumberCollection { get; set; }

        #endregion

        #region 运行状态属性
        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (SetProperty(ref _isConnected, value))
                {
                    UpdateControlButtons();
                }
            }
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set => SetProperty(ref _isRunning, value);
        }

        private bool _isOriginReturned;
        public bool IsOriginReturned
        {
            get => _isOriginReturned;
            set => SetProperty(ref _isOriginReturned, value);
        }

        private double _currentFlowRate;
        public double CurrentFlowRate
        {
            get => _currentFlowRate;
            set => SetProperty(ref _currentFlowRate, value);
        }

        private double _targetFlowRate;
        public double TargetFlowRate
        {
            get => _targetFlowRate;
            set => SetProperty(ref _targetFlowRate, value);
        }

        private bool _isAlarm;
        public bool IsAlarm
        {
            get => _isAlarm;
            set => SetProperty(ref _isAlarm, value);
        }

        private string _alarmCode;
        public string AlarmCode
        {
            get => _alarmCode;
            set => SetProperty(ref _alarmCode, value);
        }

        private string _alarmMessage;
        public string AlarmMessage
        {
            get => _alarmMessage;
            set => SetProperty(ref _alarmMessage, value);
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        #endregion

        #region 界面绑定属性
        public bool OpenButtonIsEnabled => !IsConnected;
        public bool CloseButtonIsEnabled => IsConnected;
        public bool ControlButtonsIsEnabled => IsConnected && IsOriginReturned && !IsAlarm;
        public ObservableCollection<GluePumpData> GluePumpHistory { get; }
        public GluePumpHelper GluePumpClient { get; }

        #endregion

        public GluePumpModel()
        {
            // 初始化串口参数选项
            SerialPortPortNameCollection = new ObservableCollection<string>(SerialPort.GetPortNames());
            SerialPortBaudRateCollection = new ObservableCollection<int> { 9600, 14400, 19200, 38400, 57600, 115200 };
            SerialPortParityCollection = new ObservableCollection<Parity> { Parity.None, Parity.Odd, Parity.Even };
            SerialPortStopBitsCollection = new ObservableCollection<StopBits> { StopBits.One, StopBits.OnePointFive, StopBits.Two };
            SerialPortDataBitsCollection = new ObservableCollection<int> { 5, 6, 7, 8 };

            // 初始化配方选项
            RecipeNumberCollection = new ObservableCollection<int>(Enumerable.Range(0, 16));

            GluePumpHistory = new ObservableCollection<GluePumpData>();
            GluePumpClient = new GluePumpHelper();

            // 注册状态更新事件
            GluePumpClient.StatusUpdated += OnStatusUpdated;
            GluePumpClient.ErrorOccurred += OnErrorOccurred;
        }

        private void OnStatusUpdated(GluePumpStatus status)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                IsConnected = status.IsConnected;
                IsRunning = status.IsRunning;
                IsOriginReturned = status.IsOriginReturned;
                CurrentFlowRate = status.CurrentFlowRate;
                TargetFlowRate = status.TargetFlowRate;
                IsAlarm = status.IsAlarm;
                AlarmCode = status.AlarmCode;
                AlarmMessage = status.AlarmMessage;

                // 添加状态更新记录
                string message = status.IsAlarm
                    ? $"报警: {status.AlarmCode} - {status.AlarmMessage}"
                    : $"状态更新: 运行={status.IsRunning}, 原点={status.IsOriginReturned}, 流量={status.CurrentFlowRate:F1}L/min";

                GluePumpHistory.Add(new GluePumpData
                {
                    Time = DateTime.Now,
                    Type = status.IsAlarm ? DataRecordType.Error : DataRecordType.Status,
                    Message = message
                });
            }));
        }

        private void OnErrorOccurred(string error)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                StatusMessage = $"错误: {error}";
                GluePumpHistory.Add(new GluePumpData
                {
                    Time = DateTime.Now,
                    Type = DataRecordType.Error,
                    Message = error
                });
            }));
        }

        public void UpdateControlButtons()
        {
            RaisePropertyChanged(nameof(OpenButtonIsEnabled));
            RaisePropertyChanged(nameof(CloseButtonIsEnabled));
            RaisePropertyChanged(nameof(ControlButtonsIsEnabled));
        }
    }

    public class GluePumpData
    {
        public DateTime Time { get; set; }
        public DataRecordType Type { get; set; }
        public float Value { get; set; }
        public string Message { get; set; }
    }

    public enum DataRecordType
    {
        Read,
        Write,
        Status,
        Error
    }
}