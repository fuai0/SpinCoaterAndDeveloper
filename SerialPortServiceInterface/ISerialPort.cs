using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialPortServiceInterface
{
    public interface ISerialPort
    {
        /// <summary>
        /// 接收数据事件
        /// </summary>
        event Action<string, string> DataReceivedEvent;
        /// <summary>
        /// 发生错误事件
        /// </summary>
        event Action<string, string> ErrordEvent;
        /// <summary>
        /// 串口的IocName
        /// </summary>
        string IocName { get; }
        /// <summary>
        /// 端口名
        /// </summary>
        string PortName { get; set; }

        /// <summary>
        /// 波特率
        /// </summary>
        int BaudRate { get; set; }

        /// <summary>
        /// 检验位
        /// </summary>
        Parity Parity { get; set; }

        /// <summary>
        /// 停止位
        /// </summary>
        StopBits StopBits { get; set; }

        /// <summary>
        /// 数据位
        /// </summary>
        int DataBits { get; set; }

        /// <summary>
        /// 超时时间
        /// </summary>
        int Timeout { get; set; }

        /// <summary>
        /// 串口连接状态
        /// </summary>
        bool IsConnected { get; }


        /// <summary>
        /// 初始化，打开串口
        /// </summary>
        /// <param name="portName"></param>
        /// <param name="baudRate"></param>
        /// <param name="parity"></param>
        /// <param name="stopBits"></param>
        /// <param name="dataBits"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        bool Init(string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits, int timeout);


        /// <summary>
        /// 发送数据
        /// </summary>
        bool Write(string data);

        /// <summary>
        /// 发送数据
        /// </summary>
        bool Write(byte[] data);

        /// <summary>
        /// 读缓冲区数据
        /// </summary>
        /// <returns></returns>
        bool ReadData(ref string data);

        /// <summary>
        /// 关闭串口
        /// </summary>
        void Close();

        /// <summary>
        /// 重新打开串口
        /// </summary>
        /// <returns></returns>
        bool ReConnect();
    }
}
