using SerialPortServiceInterface;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TemperatureControllerService.Service
{
    /// <summary>
    /// Modbus RTU协议助手类，基于串口通信，支持多种功能码
    /// </summary>
    public class ModbusHelper : ISerialPort
    {
        #region 私有字段

        private SerialPort _serialPort;       // 串口对象
        private bool _isConnected;            // 连接状态
        private int _timeout = 1000;          // 超时时间（毫秒）

        #endregion

        #region 公有属性

        public string IocName { get; internal set; }

        public string PortName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int BaudRate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Parity Parity { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public StopBits StopBits { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int DataBits { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int Timeout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsConnected => throw new NotImplementedException();

        #endregion

        #region 事件定义

        /// <summary>
        /// 接收数据事件，参数为从机地址、功能码、数据
        /// </summary>
        public event Action<byte, byte, byte[]> DataReceived;

        /// <summary>
        /// 错误事件，参数为错误信息
        /// </summary>
        public event Action<string> ErrorOccurred;
        public event Action<string, string> DataReceivedEvent;
        public event Action<string, string> ErrordEvent;

        #endregion

        /// <summary>
        /// 初始化Modbus RTU通信
        /// </summary>
        /// <param name="portName">串口名称</param>
        /// <param name="baudRate">波特率，默认值为9600</param>
        /// <param name="parity">校验位，默认值为偶校验</param>
        /// <param name="stopBits">停止位，默认值为1位停止位</param>
        /// <param name="dataBits">数据位，默认值为8位数据位</param>
        /// <param name="timeout">超时时间，默认值为1000毫秒</param>
        /// <returns>初始化是否成功</returns>
        public bool Init(string portName, int baudRate = 9600, Parity parity = Parity.None,
                         StopBits stopBits = StopBits.One, int dataBits = 8, int timeout = 1000)
        {
            try
            {
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
                LogExtension.Log(LogLevel.INFO, $"Modbus RTU初始化成功，串口：{portName}");
                return true;
            }
            catch (Exception ex)
            {
                LogExtension.Log(LogLevel.ERROR, $"Modbus RTU初始化失败：{ex.Message}");
                ErrorOccurred?.Invoke($"初始化失败：{ex.Message}");
                _isConnected = false;
                return false;
            }
        }

        #region Modbus核心功能（功能码实现）

        /// <summary>
        /// 读保持寄存器（功能码0x03）
        /// </summary>
        /// <param name="slaveAddress">从机地址（1-255）</param>
        /// <param name="startAddress">起始寄存器地址</param>
        /// <param name="count">读取数量（1-125）</param>
        /// <returns>寄存器数据（字节数组，每个寄存器2字节）</returns>
        public byte[] ReadHoldingRegisters(byte slaveAddress, ushort startAddress, ushort count)
        {
            if (!_isConnected || count < 1 || count > 125)
            {
                ErrorOccurred?.Invoke("读保持寄存器失败：参数无效或未连接");
                return null;
            }

            // 组装请求帧：从机地址 + 功能码0x03 + 起始地址（2字节） + 数量（2字节） + CRC（2字节）
            byte[] frame = new byte[8];
            frame[0] = slaveAddress;
            frame[1] = 0x03;
            frame[2] = (byte)(startAddress >> 8);       // 起始地址高8位
            frame[3] = (byte)(startAddress & 0xFF);     // 起始地址低8位
            frame[4] = (byte)(count >> 8);              // 数量高8位
            frame[5] = (byte)(count & 0xFF);            // 数量低8位

            // 计算CRC16校验
            ushort crc = Crc16(frame.Take(6).ToArray());
            frame[6] = (byte)(crc & 0xFF);              // CRC低8位
            frame[7] = (byte)(crc >> 8);                // CRC高8位

            // 发送请求并等待响应
            return SendAndReceive(frame, 5 + 2 * count); // 响应帧最小长度：5字节头 + 2*count数据
        }

        /// <summary>
        /// 写单个寄存器（功能码0x06）
        /// </summary>
        /// <param name="slaveAddress">从机地址</param>
        /// <param name="registerAddress">寄存器地址</param>
        /// <param name="value">写入值</param>
        /// <returns>是否成功</returns>
        public bool WriteSingleRegister(byte slaveAddress, ushort registerAddress, ushort value)
        {
            if (!_isConnected)
            {
                ErrorOccurred?.Invoke("写寄存器失败：未连接");
                return false;
            }

            // 组装请求帧：从机地址 + 功能码0x06 + 寄存器地址（2字节） + 写入值（2字节） + CRC（2字节）
            byte[] frame = new byte[8];
            frame[0] = slaveAddress;
            frame[1] = 0x06;
            frame[2] = (byte)(registerAddress >> 8);
            frame[3] = (byte)(registerAddress & 0xFF);
            frame[4] = (byte)(value >> 8);
            frame[5] = (byte)(value & 0xFF);

            // 计算CRC
            ushort crc = Crc16(frame.Take(6).ToArray());
            frame[6] = (byte)(crc & 0xFF);
            frame[7] = (byte)(crc >> 8);

            // 发送请求并验证响应（响应与请求帧一致）
            byte[] response = SendAndReceive(frame, 8);
            return response != null;
        }

        /// <summary>
        /// 写多个寄存器（功能码 0x10）
        /// </summary>
        /// <param name="slaveAddress">从机地址（1 - 255）</param>
        /// <param name="startAddress">起始寄存器地址</param>
        /// <param name="values">要写入的 16 位寄存器值数组（每个元素对应一个寄存器，最多 123 个，因为数据域最大 246 字节）</param>
        /// <returns>是否写入成功</returns>
        public bool WriteMultipleRegisters(byte slaveAddress, ushort startAddress, ushort[] values)
        {
            if (!_isConnected)
            {
                ErrorOccurred?.Invoke("写多个寄存器失败：未连接从机");
                return false;
            }
            if (values == null || values.Length == 0)
            {
                ErrorOccurred?.Invoke("写多个寄存器失败：写入值数组为空");
                return false;
            }
            // Modbus 协议限制：最多写入 123 个寄存器（因为数据域最大 246 字节，每个寄存器 2 字节）
            if (values.Length > 123)
            {
                ErrorOccurred?.Invoke($"写多个寄存器失败：最多支持写入 123 个寄存器，当前传入 {values.Length} 个");
                return false;
            }

            // 帧结构：从机地址(1B) + 功能码(1B=0x10) + 起始地址(2B) + 寄存器数量(2B) + 字节数(1B) + 数据(2*N B) + CRC(2B)
            int dataLength = values.Length * 2; 
            byte[] frame = new byte[7 + dataLength];

            frame[0] = slaveAddress;
            frame[1] = 0x10; 

            frame[2] = (byte)(startAddress >> 8);
            frame[3] = (byte)(startAddress & 0xFF);

            ushort registerCount = (ushort)values.Length;
            frame[4] = (byte)(registerCount >> 8);
            frame[5] = (byte)(registerCount & 0xFF);

            frame[6] = (byte)dataLength;

            for (int i = 0; i < values.Length; i++)
            {
                ushort value = values[i];
                frame[7 + i * 2] = (byte)(value >> 8);   // 高 8 位
                frame[7 + i * 2 + 1] = (byte)(value & 0xFF); // 低 8 位
            }

            ushort crc = Crc16(frame.Take(6 + dataLength).ToArray());
            frame[6 + dataLength] = (byte)(crc & 0xFF);  // CRC 低 8 位
            frame[7 + dataLength] = (byte)(crc >> 8);    // CRC 高 8 位

            byte[] response = SendAndReceive(frame, 8);
            if (response == null)
            {
                ErrorOccurred?.Invoke("写多个寄存器失败：未收到有效响应");
                return false;
            }
            if (response.Length != 8)
            {
                ErrorOccurred?.Invoke($"写多个寄存器失败：响应长度异常，预期 8 字节，实际 {response.Length} 字节");
                return false;
            }
            if (response[0] != slaveAddress)
            {
                ErrorOccurred?.Invoke("写多个寄存器失败：从机地址响应不匹配");
                return false;
            }
            if (response[1] != 0x10)
            {
                ErrorOccurred?.Invoke("写多个寄存器失败：功能码响应异常，可能从机不支持或请求有误");
                return false;
            }

            ushort respStartAddr = (ushort)((response[2] << 8) | response[3]);
            ushort respRegCount = (ushort)((response[4] << 8) | response[5]);
            if (respStartAddr != startAddress || respRegCount != registerCount)
            {
                ErrorOccurred?.Invoke("写多个寄存器失败：响应的起始地址或寄存器数量与请求不一致");
                return false;
            }

            return true;
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 发送请求帧并接收响应帧
        /// </summary>
        /// <param name="requestFrame">请求帧</param>
        /// <param name="expectedLength">预期响应长度（最小）</param>
        /// <returns>响应数据（去除从机地址、功能码、CRC）</returns>
        private byte[] SendAndReceive(byte[] requestFrame, int expectedLength)
        {
            try
            {
                _serialPort.DiscardInBuffer();
                _serialPort.Write(requestFrame, 0, requestFrame.Length);
                LogExtension.Log(LogLevel.INFO, $"发送Modbus帧：{BitConverter.ToString(requestFrame)}");

                // 等待响应（超时控制）
                DateTime startTime = DateTime.Now;
                while (_serialPort.BytesToRead < expectedLength)
                {
                    if ((DateTime.Now - startTime).TotalMilliseconds > _timeout)
                    {
                        ErrorOccurred?.Invoke("响应超时");
                        return null;
                    }
                    System.Threading.Thread.Sleep(10);
                }

                // 读取响应帧
                byte[] responseFrame = new byte[_serialPort.BytesToRead];
                _serialPort.Read(responseFrame, 0, responseFrame.Length);
                LogExtension.Log(LogLevel.INFO, $"接收Modbus帧：{BitConverter.ToString(responseFrame)}");

                // 校验CRC
                if (!CheckCrc(responseFrame))
                {
                    ErrorOccurred?.Invoke("CRC校验失败");
                    return null;
                }

                // 校验从机地址和功能码（是否与请求一致）
                if (responseFrame[0] != requestFrame[0] ||
                    (responseFrame[1] & 0x7F) != requestFrame[1]) // 高7位为功能码，最低位为错误标志
                {
                    ErrorOccurred?.Invoke($"响应异常：从机地址或功能码不匹配");
                    return null;
                }

                // 检查异常功能码（功能码最高位为1表示异常）
                if ((responseFrame[1] & 0x80) != 0)
                {
                    byte errorCode = responseFrame[2];
                    ErrorOccurred?.Invoke($"Modbus异常，错误码：{errorCode}");
                    return null;
                }

                // 提取有效数据（去除从机地址、功能码、CRC）
                byte[] data = responseFrame.Skip(2).Take(responseFrame.Length - 4).ToArray();
                DataReceived?.Invoke(responseFrame[0], responseFrame[1], data);
                return data;
            }
            catch (Exception ex)
            {
                LogExtension.Log(LogLevel.ERROR, $"Modbus通信失败：{ex.Message}");
                ErrorOccurred?.Invoke($"通信失败：{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 计算CRC16校验（Modbus RTU标准）
        /// </summary>
        /// <param name="data">待计算的数据</param>
        /// <returns>CRC16校验值</returns>
        private ushort Crc16(byte[] data)
        {
            ushort crc = 0xFFFF;
            foreach (byte b in data)
            {
                crc ^= (ushort)b;
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }
            return crc;
        }

        /// <summary>
        /// 检查响应帧的CRC校验
        /// </summary>
        /// <param name="frame">响应帧</param>
        /// <returns>CRC校验是否通过</returns>
        private bool CheckCrc(byte[] frame)
        {
            if (frame.Length < 2) return false;

            // 提取帧中的CRC
            ushort receivedCrc = (ushort)((frame[frame.Length - 1] << 8) | frame[frame.Length - 2]);
            // 计算数据部分的CRC
            ushort calculatedCrc = Crc16(frame.Take(frame.Length - 2).ToArray());
            return receivedCrc == calculatedCrc;
        }

        #endregion

        #region 连接管理
        /// <summary>
        /// 重新连接
        /// </summary>
        /// <returns>重新连接是否成功</returns>
        public bool ReConnect()
        {
            try
            {
                if (_serialPort?.IsOpen ?? false)
                    _serialPort.Close();

                _serialPort?.Open();
                _serialPort.DiscardInBuffer();
                _serialPort.DiscardOutBuffer();
                _isConnected = true;
                LogExtension.Log(LogLevel.INFO, $"Modbus重新连接成功，串口：{_serialPort.PortName}");
                return true;
            }
            catch (Exception ex)
            {
                LogExtension.Log(LogLevel.ERROR, $"Modbus重新连接失败：{ex.Message}");
                _isConnected = false;
                return false;
            }
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        public void Close()
        {
            if (_serialPort?.IsOpen ?? false)
            {
                _serialPort.Close();
                LogExtension.Log(LogLevel.INFO, "Modbus连接已关闭");
            }
            _isConnected = false;
        }

        public bool Write(string data)
        {
            throw new NotImplementedException();
        }

        public bool Write(byte[] data)
        {
            throw new NotImplementedException();
        }

        public bool ReadData(ref string data)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 析构函数，确保资源释放
        /// </summary>
        ~ModbusHelper()
        {
            Close();
        }
        #endregion
    }
}