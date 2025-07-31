using MaterialDesignThemes.Wpf;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using GluePumpService.Common.Model;
using GluePumpService.Service;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.ObjectModel;
using System.Text;
using System.Linq;

namespace GluePumpService.ViewModels
{
    /// <summary>
    /// 胶泵模块视图模型，负责处理胶泵设备的交互逻辑
    /// </summary>
    public class GluePumpViewModel : BindableBase, INavigationAware
    {
        #region 依赖注入与基础属性

        /// <summary>
        /// 容器提供者，用于获取依赖服务
        /// </summary>
        private readonly IContainerProvider _containerProvider;

        /// <summary>
        /// 消息队列，用于显示Material Design风格的通知消息
        /// </summary>
        private readonly ISnackbarMessageQueue _snackbarQueue;

        /// <summary>
        /// 胶泵设备集合，绑定到视图显示所有设备
        /// </summary>
        public ObservableCollection<GluePumpModel> GluePumpCollection { get; set; }

        /// <summary>
        /// 当前选中的胶泵设备
        /// </summary>
        private GluePumpModel _selectedDevice;
        public GluePumpModel SelectedDevice
        {
            get => _selectedDevice;
            set => SetProperty(ref _selectedDevice, value);
        }

        #endregion

        #region 命令定义

        private DelegateCommand<GluePumpModel> connectCommand;
        /// <summary>
        /// 连接设备命令，用于建立与胶泵的通信连接
        /// </summary>
        public DelegateCommand<GluePumpModel> ConnectCommand =>
            connectCommand ?? (connectCommand = new DelegateCommand<GluePumpModel>(ExecuteConnectCommand));

        private DelegateCommand<GluePumpModel> disconnectCommand;
        /// <summary>
        /// 断开连接命令，用于终止与胶泵的通信连接
        /// </summary>
        public DelegateCommand<GluePumpModel> DisconnectCommand =>
            disconnectCommand ?? (disconnectCommand = new DelegateCommand<GluePumpModel>(ExecuteDisconnectCommand));

        private DelegateCommand<GluePumpModel> returnToOriginCommand;
        /// <summary>
        /// 回原点命令，控制胶泵执行回原点操作（设备上电后必须执行）
        /// </summary>
        public DelegateCommand<GluePumpModel> ReturnToOriginCommand =>
            returnToOriginCommand ?? (returnToOriginCommand = new DelegateCommand<GluePumpModel>(ExecuteReturnToOriginCommand));

        private DelegateCommand<GluePumpModel> runRecipeCommand;
        /// <summary>
        /// 运行配方命令，执行指定编号的配方程序
        /// </summary>
        public DelegateCommand<GluePumpModel> RunRecipeCommand =>
            runRecipeCommand ?? (runRecipeCommand = new DelegateCommand<GluePumpModel>(ExecuteRunRecipeCommand));

        private DelegateCommand<GluePumpModel> stopRunningCommand;
        /// <summary>
        /// 停止运行命令，终止当前胶泵的运行程序
        /// </summary>
        public DelegateCommand<GluePumpModel> StopRunningCommand =>
            stopRunningCommand ?? (stopRunningCommand = new DelegateCommand<GluePumpModel>(ExecuteStopRunningCommand));

        private DelegateCommand<GluePumpModel> readFlowRateCommand;
        /// <summary>
        /// 读取流量命令，获取胶泵当前的流量值
        /// </summary>
        public DelegateCommand<GluePumpModel> ReadFlowRateCommand =>
            readFlowRateCommand ?? (readFlowRateCommand = new DelegateCommand<GluePumpModel>(ExecuteReadFlowRateCommand));

        private DelegateCommand<GluePumpModel> setFlowRateCommand;
        /// <summary>
        /// 设置流量命令，配置胶泵的目标流量值
        /// </summary>
        public DelegateCommand<GluePumpModel> SetFlowRateCommand =>
            setFlowRateCommand ?? (setFlowRateCommand = new DelegateCommand<GluePumpModel>(ExecuteSetFlowRateCommand));

        private DelegateCommand<GluePumpModel> getAlarmStatusCommand;
        /// <summary>
        /// 读取警报状态命令，查询设备当前是否存在报警
        /// </summary>
        public DelegateCommand<GluePumpModel> GetAlarmStatusCommand =>
            getAlarmStatusCommand ?? (getAlarmStatusCommand = new DelegateCommand<GluePumpModel>(ExecuteGetAlarmStatusCommand));

        private DelegateCommand<GluePumpModel> saveConfigCommand;
        /// <summary>
        /// 保存配置命令，将设备参数持久化到配置文件
        /// </summary>
        public DelegateCommand<GluePumpModel> SaveConfigCommand =>
            saveConfigCommand ?? (saveConfigCommand = new DelegateCommand<GluePumpModel>(ExecuteSaveConfigCommand));

        #endregion

        /// <summary>
        /// 构造函数，初始化依赖注入和设备集合
        /// </summary>
        /// <param name="containerProvider">容器提供者，用于解析服务</param>
        public GluePumpViewModel(IContainerProvider containerProvider)
        {
            _containerProvider = containerProvider;
            _snackbarQueue = containerProvider.Resolve<ISnackbarMessageQueue>();
            GluePumpCollection = new ObservableCollection<GluePumpModel>();
        }

        /// <summary>
        /// 加载配置文件中的胶泵设备信息
        /// </summary>
        private void LoadConfiguration()
        {
            // 清空现有设备列表
            GluePumpCollection.Clear();

            // 读取配置文件
            GluePumpConfig config = new GluePumpConfig();
            var services = config.LoadConfigFromFile();

            // 配置文件存在时，初始化设备模型
            if (services != null)
            {
                foreach (ServiceConfigurationElement service in services.Services)
                {
                    GluePumpModel temp = new GluePumpModel();

                    // 从配置项映射设备属性
                    temp.IocDiName = service.IocName;
                    temp.PortName = service.PortName;
                    temp.BaudRate = service.BaudRate;
                    temp.Parity = service.Parity;
                    temp.StopBits = service.StopBits;
                    temp.DataBits = service.DataBits;
                    temp.Timeout = service.Timeout;
                    temp.ControllerId = service.ControllerId;

                    // 添加到设备集合
                    GluePumpCollection.Add(temp);
                }
            }
        }

        #region 命令实现

        /// <summary>
        /// 执行连接设备操作
        /// </summary>
        /// <param name="device">目标胶泵设备</param>
        private void ExecuteConnectCommand(GluePumpModel device)
        {
            if (device == null) return;

            try
            {
                // 调用设备客户端初始化连接
                bool isSuccess = device.GluePumpClient.Init(
                    device.PortName,
                    device.BaudRate,
                    device.Parity,
                    device.StopBits,
                    device.DataBits,
                    device.Timeout,
                    device.ControllerId);

                if (isSuccess)
                {
                    // 连接成功，更新设备状态
                    device.IsConnected = true;
                    device.StatusMessage = "连接成功";
                    // 记录操作历史
                    device.GluePumpHistory.Add(new GluePumpData
                    {
                        Time = DateTime.Now,
                        Type = DataRecordType.Status,
                        Message = "Glue Pump设备已连接"
                    });
                    // 显示通知消息
                    _snackbarQueue.Enqueue($"{device.IocDiName} 连接成功");

                    // 连接后自动读取一次报警状态
                    ExecuteGetAlarmStatusCommand(device);
                }
                else
                {
                    // 连接失败
                    device.StatusMessage = "连接失败";
                    _snackbarQueue.Enqueue($"{device.IocDiName} 连接失败");
                }
            }
            catch (Exception ex)
            {
                // 处理连接异常
                device.StatusMessage = $"连接异常: {ex.Message}";
                _snackbarQueue.Enqueue($"{device.IocDiName} 连接异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行断开连接操作
        /// </summary>
        /// <param name="device">目标胶泵设备</param>
        private void ExecuteDisconnectCommand(GluePumpModel device)
        {
            if (device == null || !device.IsConnected) return;

            try
            {
                // 关闭设备连接
                device.GluePumpClient.Close();
                // 更新设备状态
                device.IsConnected = false;
                device.IsRunning = false;
                device.StatusMessage = "已断开连接";
                // 记录操作历史
                device.GluePumpHistory.Add(new GluePumpData
                {
                    Time = DateTime.Now,
                    Type = DataRecordType.Status,
                    Message = "Glue Pump设备已断开"
                });
                // 显示通知消息
                _snackbarQueue.Enqueue($"{device.IocDiName} 已断开");
            }
            catch (Exception ex)
            {
                // 处理断开异常
                device.StatusMessage = $"断开异常: {ex.Message}";
                _snackbarQueue.Enqueue($"{device.IocDiName} 断开异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行回原点操作（异步）
        /// </summary>
        /// <param name="device">目标胶泵设备</param>
        private async void ExecuteReturnToOriginCommand(GluePumpModel device)
        {
            if (device == null || !device.IsConnected) return;

            // 更新状态信息
            device.StatusMessage = "正在回原点...";
            // 调用设备客户端执行回原点命令
            var isSuccess = await device.GluePumpClient.ReturnToOriginAsync();

            if (isSuccess)
            {
                // 回原点成功
                device.StatusMessage = "回原点完成";
                _snackbarQueue.Enqueue($"{device.IocDiName} 回原点成功");
            }
            else
            {
                // 回原点失败
                _snackbarQueue.Enqueue($"{device.IocDiName} 回原点失败");
            }
        }

        /// <summary>
        /// 执行运行配方操作（异步）
        /// </summary>
        /// <param name="device">目标胶泵设备</param>
        private async void ExecuteRunRecipeCommand(GluePumpModel device)
        {
            if (device == null || !device.IsConnected) return;

            // 更新状态信息
            device.StatusMessage = $"正在运行配方 {device.SelectedRecipe}...";
            // 调用设备客户端执行配方命令
            var isSuccess = await device.GluePumpClient.RunRecipeAsync(device.SelectedRecipe);

            if (isSuccess)
            {
                // 运行成功
                device.StatusMessage = $"配方 {device.SelectedRecipe} 运行中";
                _snackbarQueue.Enqueue($"配方 {device.SelectedRecipe} 已启动");
            }
        }

        /// <summary>
        /// 执行停止运行操作（异步）
        /// </summary>
        /// <param name="device">目标胶泵设备</param>
        private async void ExecuteStopRunningCommand(GluePumpModel device)
        {
            if (device == null || !device.IsConnected) return;

            // 更新状态信息
            device.StatusMessage = "正在停止...";
            // 调用设备客户端执行停止命令
            var isSuccess = await device.GluePumpClient.StopRunningAsync();

            if (isSuccess)
            {
                // 停止成功
                device.StatusMessage = "已停止";
                _snackbarQueue.Enqueue("运行已停止");
            }
        }

        /// <summary>
        /// 执行读取流量操作（异步）
        /// </summary>
        /// <param name="device">目标胶泵设备</param>
        private async void ExecuteReadFlowRateCommand(GluePumpModel device)
        {
            if (device == null || !device.IsConnected) return;

            // 更新状态信息
            device.StatusMessage = "正在读取流量...";
            // 调用设备客户端读取流量
            var flowRate = await device.GluePumpClient.ReadCurrentFlowRateAsync();

            if (flowRate >= 0)
            {
                // 读取成功
                device.StatusMessage = $"当前流量: {flowRate:F1} L/min";
                _snackbarQueue.Enqueue($"读取成功: {flowRate:F1} L/min");
            }
            else
            {
                // 读取失败
                _snackbarQueue.Enqueue("读取流量失败");
            }
        }

        /// <summary>
        /// 执行设置流量操作（异步）
        /// </summary>
        /// <param name="device">目标胶泵设备</param>
        private async void ExecuteSetFlowRateCommand(GluePumpModel device)
        {
            if (device == null || !device.IsConnected) return;

            // 更新状态信息
            device.StatusMessage = $"正在设置流量为 {device.TargetFlowRate:F1} L/min...";
            // 调用设备客户端设置流量
            var isSuccess = await device.GluePumpClient.SetFlowRateAsync(device.TargetFlowRate);

            if (isSuccess)
            {
                // 设置成功
                device.StatusMessage = $"流量已设置为 {device.TargetFlowRate:F1} L/min";
                _snackbarQueue.Enqueue($"流量设置成功: {device.TargetFlowRate:F1} L/min");
            }
            else
            {
                // 设置失败
                _snackbarQueue.Enqueue("流量设置失败");
            }
        }

        /// <summary>
        /// 执行读取报警状态操作（异步）
        /// </summary>
        /// <param name="device">目标胶泵设备</param>
        private async void ExecuteGetAlarmStatusCommand(GluePumpModel device)
        {
            if (device == null || !device.IsConnected) return;

            // 更新状态信息
            device.StatusMessage = "正在检查报警状态...";
            // 调用设备客户端读取报警状态
            var alarmStatus = await device.GluePumpClient.GetAlarmStatusAsync();

            if (alarmStatus == null)
            {
                // 无报警
                device.StatusMessage = "设备正常，无报警";
                _snackbarQueue.Enqueue("设备正常");
            }
            else
            {
                // 存在报警，显示报警信息并提供清除按钮
                device.StatusMessage = $"报警: {alarmStatus}";
                _snackbarQueue.Enqueue($"报警: {alarmStatus}", "清除报警", () => ExecuteClearAlarmCommand(device));
            }
        }

        /// <summary>
        /// 执行清除报警操作（异步）
        /// </summary>
        /// <param name="device">目标胶泵设备</param>
        private async void ExecuteClearAlarmCommand(GluePumpModel device)
        {
            if (device == null || !device.IsConnected) return;

            // 更新状态信息
            device.StatusMessage = "正在清除报警...";

            // 发送清除报警命令（根据设备协议调整命令字符串）
            var response = await device.GluePumpClient.SendCommandAsync("@CLR_ALM");
            if (response != null && Encoding.ASCII.GetString(response).Contains("OK"))
            {
                // 清除成功
                device.StatusMessage = "报警已清除";
                _snackbarQueue.Enqueue("报警已清除");
                // 再次检查报警状态
                ExecuteGetAlarmStatusCommand(device);
            }
            else
            {
                // 清除失败
                device.StatusMessage = "清除报警失败";
                _snackbarQueue.Enqueue("清除报警失败");
            }
        }

        /// <summary>
        /// 执行保存配置操作
        /// </summary>
        /// <param name="device">目标胶泵设备</param>
        private void ExecuteSaveConfigCommand(GluePumpModel device)
        {
            if (device == null) return;

            try
            {
                // 保存设备配置到文件
                ServiceGluePumpConfigurationStore store = new ServiceGluePumpConfigurationStore();
                store.SaveCfg(
                    device.IocDiName,
                    device.PortName,
                    device.BaudRate,
                    device.Parity,
                    device.StopBits,
                    device.DataBits,
                    device.Timeout,
                    device.ControllerId);

                // 保存成功
                device.StatusMessage = "配置保存成功";
                _snackbarQueue.Enqueue($"{device.IocDiName} 配置保存成功");
            }
            catch (Exception ex)
            {
                // 保存异常
                device.StatusMessage = $"配置保存异常: {ex.Message}";
                _snackbarQueue.Enqueue($"{device.IocDiName} 配置保存异常: {ex.Message}");
            }
        }

        #endregion

        #region 导航实现

        /// <summary>
        /// 导航到页面时加载多语言资源并加载配置
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
            // 清理资源
            foreach (var device in GluePumpCollection)
            {
                if (device.IsConnected)
                {
                    device.GluePumpClient.Close();
                }
            }
        }

        #endregion
    }
}