using Prism.Mvvm;
using TemperatureControllerService.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Prism.Ioc;

namespace TemperatureControllerService.Common.Model
{
    /// <summary>
    /// 温控器数据模型，用于存储设备配置、运行状态和历史数据
    /// </summary>
    public class TemperatureControllerModel : BindableBase
    {
        #region 设备配置属性（Modbus通信参数）

        private string _iocDiName;
        /// <summary>
        /// 获取或设置IOC DI名称
        /// </summary>
        public string IocDiName
        {
            get => _iocDiName;
            set => SetProperty(ref _iocDiName, value);
        }

        private string _portName;
        /// <summary>
        /// 获取或设置串口名称
        /// </summary>
        public string PortName
        {
            get => _portName;
            set => SetProperty(ref _portName, value);
        }

        private int _baudRate = 9600;
        /// <summary>
        /// 获取或设置波特率，默认值为9600
        /// </summary>
        public int BaudRate
        {
            get => _baudRate;
            set => SetProperty(ref _baudRate, value);
        }

        private Parity _parity = Parity.Even;
        /// <summary>
        /// 获取或设置校验位，默认值为偶校验
        /// </summary>
        public Parity Parity
        {
            get => _parity;
            set => SetProperty(ref _parity, value);
        }

        private StopBits _stopBits = StopBits.One;
        /// <summary>
        /// 获取或设置停止位，默认值为1位停止位
        /// </summary>
        public StopBits StopBits
        {
            get => _stopBits;
            set => SetProperty(ref _stopBits, value);
        }

        private int _dataBits = 8;
        /// <summary>
        /// 获取或设置数据位，默认值为8位数据位
        /// </summary>
        public int DataBits
        {
            get => _dataBits;
            set => SetProperty(ref _dataBits, value);
        }

        private int _timeout = 1000;
        /// <summary>
        /// 获取或设置超时时间，默认值为1000毫秒
        /// </summary>
        public int Timeout
        {
            get => _timeout;
            set => SetProperty(ref _timeout, value);
        }

        private byte _slaveAddress = 1;
        /// <summary>
        /// 获取或设置从机地址，默认值为1
        /// </summary>
        public byte SlaveAddress
        {
            get => _slaveAddress;
            set => SetProperty(ref _slaveAddress, value);
        }

        public ObservableCollection<string> SerialPortPortNameCollection { get; set; }
        public ObservableCollection<int> SerialPortBaudRateCollection { get; set; }
        public ObservableCollection<Parity> SerialPortParityCollection { get; set; }
        public ObservableCollection<StopBits> SerialPortStopBitsCollection { get; set; }
        public ObservableCollection<int> SerialPortDataBitsCollection { get; set; }

        #endregion

        #region 运行状态属性（实时数据）

        private bool _isConnected;
        /// <summary>
        /// 获取或设置连接状态，状态变化时更新按钮可用性
        /// </summary>
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (SetProperty(ref _isConnected, value))
                {
                    // 状态变化时更新按钮可用性
                    UpdateControlButtons();
                }
            }
        }

        private float _currentTemperature;
        /// <summary>
        /// 获取或设置当前温度
        /// </summary>
        public float CurrentTemperature
        {
            get => _currentTemperature;
            set => SetProperty(ref _currentTemperature, value);
        }

        private float _targetTemperature;
        /// <summary>
        /// 获取或设置目标温度
        /// </summary>
        public float TargetTemperature
        {
            get => _targetTemperature;
            set => SetProperty(ref _targetTemperature, value);
        }

        private string _statusMessage;
        /// <summary>
        /// 获取或设置状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        #endregion

        #region 界面绑定属性（按钮状态、历史记录）

        /// <summary>
        /// 获取打开按钮是否可用，未连接时可用
        /// </summary>
        public bool OpenButtonIsEnabled => !IsConnected;
        /// <summary>
        /// 获取关闭按钮是否可用，已连接时可用
        /// </summary>
        public bool CloseButtonIsEnabled => IsConnected;
        /// <summary>
        /// 获取控制按钮是否可用，已连接时可用
        /// </summary>
        public bool ControlButtonsIsEnabled => IsConnected;

        /// <summary>
        /// 获取温度历史记录集合
        /// </summary>
        public ObservableCollection<TemperatureData> TemperatureHistory { get; }
        /// <summary>
        /// 获取Modbus客户端实例
        /// </summary>
        public ModbusHelper ModbusClient { get; }

        #endregion

        /// <summary>
        /// 初始化TemperatureControllerModel实例
        /// </summary>
        public TemperatureControllerModel()
        {
            // 初始化集合和Modbus客户端
            SerialPortPortNameCollection = new ObservableCollection<string>();
            foreach (var port in SerialPort.GetPortNames())
            {
                SerialPortPortNameCollection.Add(port);
            }
            SerialPortBaudRateCollection = new ObservableCollection<int>
            {
                9600,
                14400,
                19200,
                38400,
                57600,
                115200
            };
            SerialPortParityCollection = new ObservableCollection<Parity>
            {
                Parity.None,
                Parity.Odd,
                Parity.Even
            };
            SerialPortStopBitsCollection = new ObservableCollection<StopBits>
            {
                StopBits.None,
                StopBits.One,
                StopBits.OnePointFive,
                StopBits.Two
            };
            SerialPortDataBitsCollection = new ObservableCollection<int> { 5, 6, 7, 8 };
            TemperatureHistory = new ObservableCollection<TemperatureData>();

            ModbusClient = new ModbusHelper();
            ModbusClient.ErrorOccurred += OnModbusError;
        }

        /// <summary>
        /// 更新按钮状态，触发属性变更通知
        /// </summary>
        public void UpdateControlButtons()
        {
            RaisePropertyChanged(nameof(OpenButtonIsEnabled));
            RaisePropertyChanged(nameof(CloseButtonIsEnabled));
            RaisePropertyChanged(nameof(ControlButtonsIsEnabled));
        }

        /// <summary>
        /// Modbus错误回调，更新状态消息和历史记录
        /// </summary>
        /// <param name="error">错误信息</param>
        private void OnModbusError(string error)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                StatusMessage = $"错误: {error}";
                TemperatureHistory.Add(new TemperatureData
                {
                    Time = DateTime.Now,
                    Type = DataRecordType.Error,
                    Message = error
                });
            }));
        }
    }

    /// <summary>
    /// 温度历史数据模型
    /// </summary>
    public class TemperatureData
    {
        /// <summary>
        /// 获取或设置记录时间
        /// </summary>
        public DateTime Time { get; set; }
        /// <summary>
        /// 获取或设置数据记录类型
        /// </summary>
        public DataRecordType Type { get; set; }
        /// <summary>
        /// 获取或设置温度值
        /// </summary>
        public float Value { get; set; }
        /// <summary>
        /// 获取或设置记录消息
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// 数据记录类型枚举
    /// </summary>
    public enum DataRecordType
    {
        Read,    // 读取数据
        Write,   // 写入数据
        Status,  // 状态信息
        Error    // 错误信息
    }
}