using MaterialDesignThemes.Wpf;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using TemperatureControllerService.Common.Model;
using TemperatureControllerService.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using SerialPortServiceInterface;

namespace TemperatureControllerService.ViewModels
{
    /// <summary>
    /// 温控器视图模型，处理界面交互逻辑和业务逻辑
    /// </summary>
    public class TemperatureControllerViewModel : BindableBase, INavigationAware
    {
        #region 依赖注入与基础属性
        private readonly IContainerProvider _containerProvider;
        private readonly ISnackbarMessageQueue _snackbarQueue;

        /// <summary>
        /// 获取温控器模型集合
        /// </summary>
        public ObservableCollection<TemperatureControllerModel> TemperatureControllerCollection { get; set; }

        private TemperatureControllerModel _selectedDevice;
        /// <summary>
        /// 获取或设置选中的设备
        /// </summary>
        public TemperatureControllerModel SelectedDevice
        {
            get => _selectedDevice;
            set => SetProperty(ref _selectedDevice, value);
        }
        #endregion

        #region 命令定义

        private DelegateCommand<TemperatureControllerModel> connectCommand;
        /// <summary>
        /// 获取连接命令
        /// </summary>
        public DelegateCommand<TemperatureControllerModel> ConnectCommand =>
            connectCommand ?? (connectCommand = new DelegateCommand<TemperatureControllerModel>(ExecuteConnectCommand));

        private DelegateCommand<TemperatureControllerModel> disconnectCommand;
        /// <summary>
        /// 获取断开连接命令
        /// </summary>
        public DelegateCommand<TemperatureControllerModel> DisconnectCommand =>
            disconnectCommand ?? (disconnectCommand = new DelegateCommand<TemperatureControllerModel>(ExecuteDisconnectCommand));

        private DelegateCommand<TemperatureControllerModel> readTemperatureCommand;
        /// <summary>
        /// 获取读取温度命令
        /// </summary>
        public DelegateCommand<TemperatureControllerModel> ReadTemperatureCommand =>
            readTemperatureCommand ?? (readTemperatureCommand = new DelegateCommand<TemperatureControllerModel>(ExecuteReadTemperatureCommand));

        private DelegateCommand<TemperatureControllerModel> setTemperatureCommand;
        /// <summary>
        /// 获取设置温度命令
        /// </summary>
        public DelegateCommand<TemperatureControllerModel> SetTemperatureCommand =>
            setTemperatureCommand ?? (setTemperatureCommand = new DelegateCommand<TemperatureControllerModel>(ExecuteSetTemperatureCommand));

        private DelegateCommand<TemperatureControllerModel> saveConfigCommand;
        /// <summary>
        /// 获取保存配置命令
        /// </summary>
        public DelegateCommand<TemperatureControllerModel> SaveConfigCommand =>
            saveConfigCommand ?? (saveConfigCommand = new DelegateCommand<TemperatureControllerModel>(ExecuteSaveConfigCommand));

        #endregion

        /// <summary>
        /// 初始化TemperatureControllerViewModel实例
        /// </summary>
        /// <param name="containerProvider">容器提供者</param>
        public TemperatureControllerViewModel(IContainerProvider containerProvider)
        {
            _containerProvider = containerProvider;
            _snackbarQueue = containerProvider.Resolve<ISnackbarMessageQueue>();
            TemperatureControllerCollection = new ObservableCollection<TemperatureControllerModel>();
        }

        /// <summary>
        /// 加载温控器配置信息
        /// </summary>
        private void LoadConfiguration()
        {
            // 清空现有的温控器集合
            TemperatureControllerCollection.Clear();

            // 创建配置类实例
            TemperatureControllerConfig config = new TemperatureControllerConfig();

            // 从文件中加载配置信息
            var services = config.LoadConfigFromFile();

            if (services != null)
            {
                // 遍历每个配置项
                foreach (ServiceConfigurationElement service in services.Services)
                {
                    // 创建新的温控器模型实例
                    TemperatureControllerModel temp = new TemperatureControllerModel();

                    // 将配置信息应用到模型上
                    temp.IocDiName = service.IocName;
                    temp.PortName = service.PortName;
                    temp.BaudRate = service.BaudRate;
                    temp.Parity = service.Parity;
                    temp.StopBits = service.StopBits;
                    temp.DataBits = service.DataBits;
                    temp.Timeout = service.Timeout;

                    // 将模型添加到集合中
                    TemperatureControllerCollection.Add(temp);
                }
            }
        }

        #region 命令实现（核心业务逻辑）

        /// <summary>
        /// 连接温控器
        /// </summary>
        /// <param name="device">要连接的设备</param>
        private void ExecuteConnectCommand(TemperatureControllerModel device)
        {
            if (device == null) return;

            try
            {
                bool isSuccess = device.ModbusClient.Init(
                    device.PortName,
                    device.BaudRate,
                    device.Parity,
                    device.StopBits,
                    device.DataBits,
                    device.Timeout);

                if (isSuccess)
                {
                    device.IsConnected = true;
                    device.StatusMessage = "连接成功";
                    device.TemperatureHistory.Add(new TemperatureData
                    {
                        Time = DateTime.Now,
                        Type = DataRecordType.Status,
                        Message = "Modbus设备已连接"
                    });
                    _snackbarQueue.Enqueue($"{device.IocDiName} 连接成功");
                }
                else
                {
                    device.StatusMessage = "连接失败";
                    _snackbarQueue.Enqueue($"{device.IocDiName} 连接失败");
                }
            }
            catch (Exception ex)
            {
                device.StatusMessage = $"连接异常: {ex.Message}";
                _snackbarQueue.Enqueue($"{device.IocDiName} 连接异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 断开温控器连接
        /// </summary>
        /// <param name="device">要断开连接的设备</param>
        private void ExecuteDisconnectCommand(TemperatureControllerModel device)
        {
            if (device == null || !device.IsConnected) return;

            try
            {
                device.ModbusClient.Close();
                device.IsConnected = false;
                device.StatusMessage = "已断开连接";
                device.TemperatureHistory.Add(new TemperatureData
                {
                    Time = DateTime.Now,
                    Type = DataRecordType.Status,
                    Message = "Modbus设备已断开"
                });
                _snackbarQueue.Enqueue($"{device.IocDiName} 已断开");
            }
            catch (Exception ex)
            {
                device.StatusMessage = $"断开异常: {ex.Message}";
                _snackbarQueue.Enqueue($"{device.IocDiName} 断开异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 读取当前温度（从Modbus寄存器）
        /// </summary>
        /// <param name="device">要读取温度的设备</param>
        private void ExecuteReadTemperatureCommand(TemperatureControllerModel device)
        {
            if (device == null || !device.IsConnected) return;

            // 异步读取，避免阻塞UI
            Task.Run(() =>
            {
                try
                {
                    // 假设当前温度存于寄存器0x0000，目标温度存于0x0001（连续读取2个寄存器）
                    byte[] response = device.ModbusClient.ReadHoldingRegisters(
                        device.SlaveAddress,  // 从机地址
                        0x0000,               // 起始寄存器地址
                        2);                   // 读取数量
                    if (response != null && response.Length >= 5)
                    {
                        // 跳过第1个字节（数据长度），从第2个字节开始解析温度值
                        byte[] dataBytes = new byte[4];
                        Array.Copy(response, 1, dataBytes, 0, 4); // 复制数据部分

                        // 处理字节序（根据设备实际情况调整）
                        if (BitConverter.IsLittleEndian)
                        {
                            // 假设设备返回大端序，而系统是小端序，需要反转每个寄存器的字节
                            for (int i = 0; i < 4; i += 2)
                            {
                                Array.Reverse(dataBytes, i, 2);
                            }
                        }

                        // 解析温度值（放大10倍）
                        short currentRaw = BitConverter.ToInt16(dataBytes, 0);
                        short targetRaw = BitConverter.ToInt16(dataBytes, 2);

                        // UI线程更新数据
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            device.CurrentTemperature = currentRaw / 10.0f;
                            device.TargetTemperature = targetRaw / 10.0f;
                            device.StatusMessage = $"读取成功：{device.CurrentTemperature}℃";
                            device.TemperatureHistory.Add(new TemperatureData
                            {
                                Time = DateTime.Now,
                                Type = DataRecordType.Read,
                                Value = device.CurrentTemperature,
                                Message = $"当前温度：{device.CurrentTemperature}℃，目标温度：{device.TargetTemperature}℃"
                            });
                        });
                    }
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        device.StatusMessage = $"读取失败：{ex.Message}";
                    });
                }
            });
        }

        /// <summary>
        /// 设置目标温度（写入Modbus寄存器）
        /// </summary>
        /// <param name="device">要设置温度的设备</param>
        private void ExecuteSetTemperatureCommand(TemperatureControllerModel device)
        {
            if (device == null || !device.IsConnected) return;

            // 异步写入，避免阻塞UI
            Task.Run(() =>
            {
                try
                {
                    // 转换：温度值放大10倍（如25.5℃ → 255）
                    ushort targetRaw = (ushort)(device.TargetTemperature * 10);

                    // 写入目标温度到寄存器0x0001
                    bool isSuccess = device.ModbusClient.WriteSingleRegister(
                        device.SlaveAddress,  // 从机地址
                        0x0001,               // 目标寄存器地址
                        targetRaw);           // 写入值

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (isSuccess)
                        {
                            device.StatusMessage = $"设置成功：{device.TargetTemperature}℃";
                            device.TemperatureHistory.Add(new TemperatureData
                            {
                                Time = DateTime.Now,
                                Type = DataRecordType.Write,
                                Value = device.TargetTemperature,
                                Message = $"已设置目标温度：{device.TargetTemperature}℃"
                            });
                        }
                        else
                        {
                            device.StatusMessage = "设置失败，设备无响应";
                        }
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        device.StatusMessage = $"设置失败：{ex.Message}";
                    });
                }
            });
        }

        /// <summary>
        /// 保存设备配置到文件
        /// </summary>
        /// <param name="device">要保存配置的设备</param>
        private void ExecuteSaveConfigCommand(TemperatureControllerModel device)
        {
            if (device == null) return;

            try
            { 
                // 串口配置存储逻辑
                var configStore = new ServiceTemperatureControllerConfigurationStore();
                configStore.SaveCfg(
                    device.IocDiName,
                    device.PortName,
                    device.BaudRate,
                    device.Parity,
                    device.StopBits,
                    device.DataBits,
                    device.Timeout);

                _snackbarQueue.Enqueue($"{device.IocDiName} 配置保存成功");
            }
            catch (Exception ex)
            {
                _snackbarQueue.Enqueue($"{device.IocDiName} 配置保存失败：{ex.Message}");
            }
        }

        #endregion

        #region 导航接口实现（Prism区域导航）
        /// <summary>
        /// 导航到页面时加载多语言资源
        /// </summary>
        /// <param name="navigationContext">导航上下文</param>
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

            LoadConfiguration();
        }

        /// <summary>
        /// 判断是否为导航目标
        /// </summary>
        /// <param name="navigationContext">导航上下文</param>
        /// <returns>是否为导航目标</returns>
        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        /// <summary>
        /// 离开页面时断开所有连接
        /// </summary>
        /// <param name="navigationContext">导航上下文</param>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 离开页面时断开所有连接
            foreach (var device in TemperatureControllerCollection)
            {
                if (device.IsConnected)
                {
                    device.ModbusClient.Close();
                    device.IsConnected = false;
                }
            }
        }

        #endregion
    }
}