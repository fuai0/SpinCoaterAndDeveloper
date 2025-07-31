using System;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GluePumpService.Service
{
    public class GluePumpHelper
    {
        #region 私有字段

        private SerialPort _serialPort;
        private bool _isConnected;
        private int _timeout = 1000;
        private byte _controllerId { get; set; }

        #endregion

        #region 公有属性

        public string IocName { get; internal set; }
        public string PortName => _serialPort?.PortName;
        public int BaudRate => _serialPort?.BaudRate ?? 0;
        public Parity Parity => _serialPort?.Parity ?? Parity.None;
        public StopBits StopBits => _serialPort?.StopBits ?? StopBits.One;
        public int DataBits => _serialPort?.DataBits ?? 0;
        public int Timeout => _timeout;
        public bool IsConnected => _isConnected && _serialPort?.IsOpen == true;

        #endregion

        #region 事件定义

        public event Action<byte, byte, byte[]> DataReceived;
        public event Action<string> ErrorOccurred;
        public event Action<string, string> DataReceivedEvent;
        public event Action<string, string> ErrordEvent;
        public event Action<GluePumpStatus> StatusUpdated;

        #endregion

        public bool Init(string portName, int baudRate = 9600, Parity parity = Parity.None,
                         StopBits stopBits = StopBits.One, int dataBits = 8, int timeout = 1000, byte controllerId = 0)
        {
            try
            {
                _controllerId = controllerId;
                _serialPort = new SerialPort
                {
                    PortName = portName,
                    BaudRate = baudRate,
                    Parity = parity,
                    StopBits = stopBits,
                    DataBits = dataBits,
                    ReadTimeout = timeout,
                    WriteTimeout = timeout,
                    ReadBufferSize = 1024,
                    WriteBufferSize = 1024
                };

                _serialPort.Open();
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();

                _timeout = timeout;
                _isConnected = true;
                Console.WriteLine($"Glue Pump initialized successfully, Serial Port: {portName}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Glue Pump initialization failed: {ex.Message}");
                ErrorOccurred?.Invoke($"Initialization failed: {ex.Message}");
                _isConnected = false;
                return false;
            }
        }

        #region 胶泵操作

        // 核心通信方法（按手册2-281节协议实现）
        public async Task<byte[]> SendCommandAsync(string command)
        {
            if (!_isConnected || _serialPort == null)
            {
                ErrorOccurred?.Invoke("未连接到设备");
                return null;
            }

            try
            {
                // 帧格式：<起始码(FF FF FF)><控制器编号><字符数><命令><CR/LF(0D 0A)><CRC(40 40)>
                var startCode = new byte[] { 0xFF, 0xFF, 0xFF };
                var cmdBytes = Encoding.ASCII.GetBytes(command);
                var crlf = new byte[] { 0x0D, 0x0A };
                var crc = new byte[] { 0x40, 0x40 };

                // 计算字符数（命令+CRLF的字节数）
                var charCount = (byte)(cmdBytes.Length + crlf.Length);

                // 组装完整帧
                var frame = new byte[startCode.Length + 2 + cmdBytes.Length + crlf.Length + crc.Length];
                Array.Copy(startCode, 0, frame, 0, startCode.Length);
                frame[startCode.Length] = _controllerId; // 控制器编号（00-0F）
                frame[startCode.Length + 1] = charCount;
                Array.Copy(cmdBytes, 0, frame, startCode.Length + 2, cmdBytes.Length);
                Array.Copy(crlf, 0, frame, startCode.Length + 2 + cmdBytes.Length, crlf.Length);
                Array.Copy(crc, 0, frame, frame.Length - crc.Length, crc.Length);

                // 发送命令（每字节间隔10ms，文档2-285节）
                for (int i = 0; i < frame.Length; i++)
                {
                    _serialPort.Write(frame, i, 1);
                    await Task.Delay(10);
                }

                // 读取响应（固定60字节，文档2-292节）
                var responseBuffer = new byte[60];
                var bytesRead = await Task.Run(() =>
                    _serialPort.Read(responseBuffer, 0, responseBuffer.Length));

                // 提取有效数据（去除NULL填充）
                var validData = responseBuffer.TakeWhile(b => b != 0x00).ToArray();
                return validData;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke($"通信异常: {ex.Message}");
                return null;
            }
        }

        // 回原点操作（@ORG命令，文档2-333节）
        public async Task<bool> ReturnToOriginAsync()
        {
            // 发送回原点命令
            var response = await SendCommandAsync("@ORG");
            if (response == null) return false;

            // 轮询回原点状态（@?ORG命令，返回1表示完成，文档2-359节）
            for (int i = 0; i < 30; i++) // 超时30秒
            {
                var statusResponse = await SendCommandAsync("@?ORG");
                if (statusResponse != null)
                {
                    var statusStr = Encoding.ASCII.GetString(statusResponse).Trim();
                    if (statusStr == "1")
                    {
                        StatusUpdated?.Invoke(new GluePumpStatus { IsOriginReturned = true });
                        return true;
                    }
                }
                await Task.Delay(1000);
            }

            ErrorOccurred?.Invoke("回原点超时（文档2-634节：上电后必须回原点）");
            return false;
        }

        // 运行配方（@MOVR命令，文档2-338节）
        public async Task<bool> RunRecipeAsync(int recipeNumber)
        {
            // 检查回原点状态（文档2-635节：未回原点无法运行配方）
            var originStatus = await SendCommandAsync("@?ORG");
            if (originStatus == null || Encoding.ASCII.GetString(originStatus).Trim() != "1")
            {
                ErrorOccurred?.Invoke("请先执行回原点操作（文档2-634节）");
                return false;
            }

            // 检查配方编号范围（0-15，文档2-219节）
            if (recipeNumber < 0 || recipeNumber > 15)
            {
                ErrorOccurred?.Invoke("配方编号必须为0-15（文档2-219节）");
                return false;
            }

            // 发送运行配方命令
            var response = await SendCommandAsync($"@MOVR_{recipeNumber}");
            if (response == null) return false;

            var responseStr = Encoding.ASCII.GetString(response);
            if (responseStr.Contains("OK"))
            {
                StatusUpdated?.Invoke(new GluePumpStatus { IsRunning = true });
                return true;
            }
            else if (responseStr.Contains("NG"))
            {
                ErrorOccurred?.Invoke($"运行配方失败: {responseStr}（文档2-342节）");
            }
            return false;
        }

        // 停止运行（@STOP命令，文档2-364节）
        public async Task<bool> StopRunningAsync()
        {
            var response = await SendCommandAsync("@STOP");
            if (response == null) return false;

            var responseStr = Encoding.ASCII.GetString(response);
            if (responseStr.Contains("OK"))
            {
                StatusUpdated?.Invoke(new GluePumpStatus { IsRunning = false });
                return true;
            }

            return false;
        }

        // 读取当前流量（@?POS命令，文档2-348节）
        public async Task<double> ReadCurrentFlowRateAsync()
        {
            var response = await SendCommandAsync("@?POS");
            if (response == null) return -1;

            var responseStr = Encoding.ASCII.GetString(response).Trim();
            if (double.TryParse(responseStr, out var flowRate))
            {
                // 文档2-349节：吸入时为吐出量减去吸入量
                StatusUpdated?.Invoke(new GluePumpStatus { CurrentFlowRate = flowRate });
                return flowRate;
            }

            ErrorOccurred?.Invoke($"读取流量失败: {responseStr}");
            return -1;
        }

        // 设置流量（@SET_POS命令，文档2-353节）
        public async Task<bool> SetFlowRateAsync(double flowRate)
        {
            // 流量范围检查（根据设备型号调整）
            if (flowRate < 0 || flowRate > 100)
            {
                ErrorOccurred?.Invoke("流量值超出范围（0-100）");
                return false;
            }

            var response = await SendCommandAsync($"@SET_POS_{flowRate:F1}");
            if (response == null) return false;

            var responseStr = Encoding.ASCII.GetString(response);
            if (responseStr.Contains("OK"))
            {
                StatusUpdated?.Invoke(new GluePumpStatus { TargetFlowRate = flowRate });
                return true;
            }

            return false;
        }

        // 读取报警状态（@?ERR命令，文档2-381节）
        public async Task<string> GetAlarmStatusAsync()
        {
            var response = await SendCommandAsync("@?ERR");
            if (response == null) return null;

            var responseStr = Encoding.ASCII.GetString(response).Trim();
            if (string.IsNullOrEmpty(responseStr))
            {
                StatusUpdated?.Invoke(new GluePumpStatus { IsAlarm = false });
                return null;
            }

            // 解析错误代码（文档2-685节）
            if (responseStr.Contains(":"))
            {
                var errorCode = responseStr.Split(':')[0];
                var errorMsg = responseStr.Split(':')[1].Trim();
                StatusUpdated?.Invoke(new GluePumpStatus
                {
                    IsAlarm = true,
                    AlarmCode = errorCode,
                    AlarmMessage = errorMsg
                });
                return $"{errorCode}: {errorMsg}";
            }

            return responseStr;
        }

        #endregion

        #region 连接管理

        public void Close()
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Close();
                    _isConnected = false;
                    Console.WriteLine($"Glue Pump disconnected, Serial Port: {_serialPort.PortName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Glue Pump disconnection failed: {ex.Message}");
                ErrorOccurred?.Invoke($"Disconnection failed: {ex.Message}");
            }
        }

        public bool ReConnect()
        {
            throw new NotImplementedException();
        }

        ~GluePumpHelper()
        {
            Close();
        }

        #endregion
    }

    // 胶泵状态模型
    public class GluePumpStatus
    {
        public bool IsConnected { get; set; }
        public bool IsRunning { get; set; }
        public bool IsOriginReturned { get; set; }
        public double CurrentFlowRate { get; set; }
        public double TargetFlowRate { get; set; }
        public bool IsAlarm { get; set; }
        public string AlarmCode { get; set; }
        public string AlarmMessage { get; set; }
    }
}