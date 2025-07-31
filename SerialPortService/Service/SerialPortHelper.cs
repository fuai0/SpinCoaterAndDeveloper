using SerialPortServiceInterface;
using SerialPortService.Common.Model;
using MaterialDesignThemes.Wpf;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Markup;

namespace SerialPortService.Service
{
    /// <summary>
    /// 串口通讯类
    /// </summary>
    public class SerialPortHelper : ISerialPort
    {
        private SerialPort serialPort;

        public event Action<string, string> DataReceivedEvent;
        public event Action<string, string> ErrordEvent;

        private byte[] RX_Buffer { get; set; } = new byte[512];

        private string RecData { get; set; } = "";
        public string IocName { get; internal set; }
        /// <summary>
        /// COM口
        /// </summary>
        public string PortName
        {
            get { return serialPort.PortName; }
            set { serialPort.PortName = value; }
        }

        /// <summary>
        /// 波特率
        /// </summary>
        public int BaudRate
        {
            get { return serialPort.BaudRate; }
            set { serialPort.BaudRate = value; }
        }

        /// <summary>
        /// 校验位
        /// </summary>
        public Parity Parity
        {
            get { return serialPort.Parity; }
            set { serialPort.Parity = value; }
        }

        /// <summary>
        /// 停止位
        /// </summary>
        public StopBits StopBits
        {
            get { return serialPort.StopBits; }
            set { serialPort.StopBits = value; }
        }

        /// <summary>
        /// 数据位
        /// </summary>
        public int DataBits
        {
            get { return serialPort.DataBits; }
            set { serialPort.DataBits = value; }
        }

        /// <summary>
        /// 超时时间
        /// </summary>
        public int Timeout { get; set; }
        public bool IsConnected
        {
            get;
            private set;
        }

        public bool Init(string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits, int timeout)
        {
            try
            {
                serialPort = new SerialPort();

                PortName = portName;
                BaudRate = baudRate;
                Parity = parity;
                StopBits = stopBits;
                DataBits = dataBits;
                Timeout = timeout;

                serialPort.ReadBufferSize = 128;
                serialPort.WriteBufferSize = 128;

                serialPort.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(this.serialPort_DataReceived);

                serialPort.Open();
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();
                LogExtension.Log(LogLevel.INFO, $"打开串口{serialPort.PortName}");
                IsConnected = true;
                return true;

            }
            catch (Exception ex)
            {
                LogExtension.Log(LogLevel.ERROR, $"{serialPort.PortName}串口打开失败：{ex.Message}");
                ErrordEvent?.Invoke(serialPort.PortName, $"{serialPort.PortName}串口打开失败：{ex.Message}");
                IsConnected = false;
                return false;
            }
        }

        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                System.Threading.Thread.Sleep(30);
                int bytes = serialPort.BytesToRead;
                RX_Buffer = new byte[bytes];
                if (bytes > 0)
                {
                    serialPort.Read(RX_Buffer, 0, bytes);
                }

                string RecData = "";
                for (int i = 0; i < RX_Buffer.Length; i++)
                {
                    if ((RX_Buffer[i] > 31 && RX_Buffer[i] < 127))
                        RecData = RecData + Convert.ToChar(RX_Buffer[i]);
                }
                LogExtension.Log(LogLevel.INFO, $"{serialPort.PortName}接收到数据：{RecData}");
                this.RecData = RecData;
                DataReceivedEvent?.Invoke(serialPort.PortName, RecData);
                IsConnected = true;
            }
            catch (Exception ex)
            {
                LogExtension.Log(LogLevel.ERROR, $"{serialPort.PortName}串口接收数据错误：{ex.Message}");
                ErrordEvent?.Invoke(serialPort.PortName, $"{serialPort.PortName}串口接收数据错误：{ex.Message}");
                RecData = "";
                IsConnected = false;
            }
        }

        public bool ReadData(ref string data)
        {
            try
            {
                data = "";
                DateTime starTime = DateTime.Now;
                while (string.IsNullOrEmpty(RecData))
                {
                    if ((DateTime.Now - starTime).TotalMilliseconds > Timeout)
                    {

                        return false;
                    }
                    Thread.Sleep(10);
                }
                data = RecData;
                RecData = "";
                IsConnected = true;
                return true;
            }
            catch (Exception ex)
            {
                LogExtension.Log(LogLevel.ERROR, $"{serialPort.PortName}读取错误：{ex.Message}");
                ErrordEvent?.Invoke(serialPort.PortName, $"{serialPort.PortName}读取错误：{ex.Message}");
                IsConnected = false;
                return false;
            }
        }

        public void Close()
        {
            serialPort.Close();
            LogExtension.Log(LogLevel.INFO, $"关闭串口{serialPort.PortName}");
            IsConnected = false;
        }

        public bool Write(string data)
        {
            try
            {
                RecData = "";
                serialPort.Write(data);
                LogExtension.Log(LogLevel.INFO, $"{serialPort.PortName}发送数据：{data}");
                IsConnected = true;
                return true;
            }
            catch (Exception ex)
            {
                LogExtension.Log(LogLevel.ERROR, $"{serialPort.PortName}发送数据错误：{ex.Message}");
                ErrordEvent?.Invoke(serialPort.PortName, $"{serialPort.PortName}发送数据错误：{ex.Message}");
                IsConnected = false;
                return false;
            }
        }

        public bool Write(byte[] data)
        {
            try
            {
                RecData = "";
                serialPort.Write(data, 0, data.Length);
                LogExtension.Log(LogLevel.INFO, $"{serialPort.PortName}发送数据：{data}");
                IsConnected = true;
                return true;
            }
            catch (Exception ex)
            {
                LogExtension.Log(LogLevel.ERROR, $"{serialPort.PortName}发送数据错误：{ex.Message}");
                ErrordEvent?.Invoke(serialPort.PortName, $"{serialPort.PortName}发送数据错误：{ex.Message}");
                IsConnected = false;
                return false;
            }
        }

        public bool ReConnect()
        {
            try
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }
                serialPort.Open();
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();
                LogExtension.Log(LogLevel.INFO, $"重新打开串口{serialPort.PortName}成功");
                IsConnected = true;
                return true;
            }
            catch(Exception ex)
            {
                LogExtension.Log(LogLevel.INFO, $"重新打开串口{serialPort.PortName}失败:{ex.Message}");
                IsConnected = false;
                return false;
            }
        }

        ~SerialPortHelper()
        {
            serialPort?.Close();
        }
    }
}
