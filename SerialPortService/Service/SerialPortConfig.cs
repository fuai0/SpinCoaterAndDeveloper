using Prism.Modularity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace SerialPortService.Service
{
    public class SerialPortConfig
    {
        private ServiceSerialPortConfigurationStore Store { get; set; }
        public SerialPortConfig()
        {
            Store = new ServiceSerialPortConfigurationStore();
        }

        public ServiceSerialPortConfigurationSection LoadConfigFromFile()
        {
            ServiceSerialPortConfigurationSection section = Store.RetrieveServicesSerialPortConfigurationSection();
            return section == null ? null : section;
        }
    }

    public class ServiceSerialPortConfigurationStore
    {
        private Configuration config;
        public ServiceSerialPortConfigurationSection RetrieveServicesSerialPortConfigurationSection()
        {
            ExeConfigurationFileMap exeMap = new ExeConfigurationFileMap() { ExeConfigFilename = "SerialPortService.config" };
            config = ConfigurationManager.OpenMappedExeConfiguration(exeMap, ConfigurationUserLevel.None);

            return config.GetSection("SerialPortConfigGroup") as ServiceSerialPortConfigurationSection;
        }

        public void SaveCfg(string iocDiName, string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits, int timeout)
        {
            ServiceSerialPortConfigurationSection section = this.RetrieveServicesSerialPortConfigurationSection();
            foreach (ServiceConfigurationElement element in section.Services)
            {
                if (element.IocName == iocDiName)
                {
                    element.PortName = portName;
                    element.BaudRate = baudRate;
                    element.Parity = parity;
                    element.StopBits = stopBits;
                    element.DataBits = dataBits;
                    element.Timeout = timeout;
                }
            }
            config.Save(ConfigurationSaveMode.Modified);
        }
    }

    public class ServiceSerialPortConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("", IsDefaultCollection = true, IsKey = false)]
        public ServiceConfigurationElementCollection Services
        {
            get { return (ServiceConfigurationElementCollection)base[""]; }
            set { base[""] = value; }
        }
    }

    public class ServiceConfigurationElementCollection : ConfigurationElementCollection
    {
        public ServiceConfigurationElementCollection()
        {

        }
        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }
        protected override string ElementName
        {
            get { return "SerialPortConfig"; }
        }
        public ServiceConfigurationElementCollection(ServiceConfigurationElement[] services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            foreach (ServiceConfigurationElement service in services)
            {
                BaseAdd(service);
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ServiceConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ServiceConfigurationElement)element).IocName;
        }
    }

    public class ServiceConfigurationElement : ConfigurationElement
    {
        public ServiceConfigurationElement()
        {

        }
        public ServiceConfigurationElement(string iocName, string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits, int timeout)
        {
            base["iocName"] = iocName;
            base["portName"] = portName;
            base["baudRate"] = baudRate;
            base["parity"] = parity;
            base["stopBits"] = stopBits;
            base["dataBits"] = dataBits;
            base["timeout"] = timeout;
        }

        [ConfigurationProperty("iocName", IsRequired = true)]
        public string IocName
        {
            get { return (string)this["iocName"]; }
            set { this["iocName"] = value; }
        }

        [ConfigurationProperty("portName", IsRequired = true)]
        public string PortName
        {
            get { return (string)this["portName"]; }
            set { this["portName"] = value; }
        }

        [ConfigurationProperty("baudRate", IsRequired = true)]
        public int BaudRate
        {
            get { return (int)this["baudRate"]; }
            set { this["baudRate"] = value; }
        }

        [ConfigurationProperty("parity", IsRequired = true)]
        public Parity Parity
        {
            get { return (Parity)this["parity"]; }
            set { this["parity"] = value; }
        }

        [ConfigurationProperty("stopBits", IsRequired = true)]
        public StopBits StopBits
        {
            get { return (StopBits)this["stopBits"]; }
            set { this["stopBits"] = value; }
        }

        [ConfigurationProperty("dataBits", IsRequired = true)]
        public int DataBits
        {
            get { return (int)this["dataBits"]; }
            set { this["dataBits"] = value; }
        }

        [ConfigurationProperty("timeout", IsRequired = true)]
        public int Timeout
        {
            get { return (int)this["timeout"]; }
            set { this["timeout"] = value; }
        }
    }
}
