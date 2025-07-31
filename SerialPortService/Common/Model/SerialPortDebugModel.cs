using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SerialPortServiceInterface;
using Prism.Ioc;
using System.IO.Ports;
using System.Collections.ObjectModel;

namespace SerialPortService.Common.Model
{
    public class SerialPortDebugModel : BindableBase
    {
        public ISerialPort serialPort;

        public ObservableCollection<string> SerialPortPortNameCollection { get; set; }
        public ObservableCollection<int> SerialPortBaudRateCollection { get; set; }
        public ObservableCollection<Parity> SerialPortParityCollection { get; set; }
        public ObservableCollection<StopBits> SerialPortStopBitsCollection { get; set; }
        public ObservableCollection<int> SerialPortDataBitsCollection { get; set; }
        public ObservableCollection<SerialPortDataModel> SerialPortDataModelCollection { get; set; }

        public SerialPortDebugModel(IContainerProvider containerProvider, ISerialPort serialPort)
        {
            this.serialPort = serialPort;
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
            SerialPortDataModelCollection = new ObservableCollection<SerialPortDataModel>();

             ConnectSuccess = this.serialPort.IsConnected;
        }

        private string iocDiName;

        public string IocDiName
        {
            get { return iocDiName; }
            set { SetProperty(ref iocDiName, value); }
        }

        private string portName;

        public string PortName
        {
            get { return portName; }
            set { SetProperty(ref portName, value); }
        }

        private int baudRate;

        public int BaudRate
        {
            get { return baudRate; }
            set { SetProperty(ref baudRate, value); }
        }

        private Parity parity;

        public Parity Parity
        {
            get { return parity; }
            set { SetProperty(ref parity, value); }
        }

        private StopBits stopBits;

        public StopBits StopBits
        {
            get { return stopBits; }
            set { SetProperty(ref stopBits, value); }
        }

        private int dataBits;

        public int DataBits
        {
            get { return dataBits; }
            set { SetProperty(ref dataBits, value); }
        }

        private int timeout = 1500;

        public int Timeout
        {
            get { return timeout; }
            set { SetProperty(ref timeout, value); }
        }

        private string sendDataTextBox;

        public string SendDataTextBox
        {
            get { return sendDataTextBox; }
            set { SetProperty(ref sendDataTextBox, value); }
        }

        private bool connectSuccess;

        public bool ConnectSuccess
        {
            get { return connectSuccess; }
            set { SetProperty(ref connectSuccess, value); }
        }

        private bool openButtonIsEnabled;

        public bool OpenButtonIsEnabled
        {
            get { return openButtonIsEnabled; }
            set { SetProperty(ref openButtonIsEnabled, value); }
        }

        private bool closeButtonIsEnabled;

        public bool CloseButtonIsEnabled
        {
            get { return closeButtonIsEnabled; }
            set { SetProperty(ref closeButtonIsEnabled, value); }
        }
    }
}
