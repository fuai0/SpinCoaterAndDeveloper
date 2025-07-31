using Prism.Modularity;
using System;
using System.Configuration;
using System.IO.Ports;

namespace GluePumpService.Service
{
    public class GluePumpConfig
    {
        private ServiceGluePumpConfigurationStore Store { get; set; }
        public GluePumpConfig()
        {
            Store = new ServiceGluePumpConfigurationStore();
        }

        public ServiceGluePumpConfigurationSection LoadConfigFromFile()
        {
            return Store.RetrieveServicesGluePumpConfigurationSection();
        }
    }

    public class ServiceGluePumpConfigurationStore
    {
        private Configuration config;
        public ServiceGluePumpConfigurationSection RetrieveServicesGluePumpConfigurationSection()
        {
            ExeConfigurationFileMap exeMap = new ExeConfigurationFileMap() { ExeConfigFilename = "GluePumpService.config" };
            config = ConfigurationManager.OpenMappedExeConfiguration(exeMap, ConfigurationUserLevel.None);

            return config.GetSection("GluePumpConfigGroup") as ServiceGluePumpConfigurationSection;
        }

        public void SaveCfg(string iocDiName, string portName, int baudRate, Parity parity, StopBits stopBits, int dataBits, int timeout, byte controllerId)
        {
            ServiceGluePumpConfigurationSection section = this.RetrieveServicesGluePumpConfigurationSection();
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
                    element.ControllerId = controllerId;
                }
            }
            config.Save(ConfigurationSaveMode.Modified);
        }
    }

    public class ServiceGluePumpConfigurationSection : ConfigurationSection
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
        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }
        protected override string ElementName
        {
            get { return "GluePumpConfig"; }
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

        [ConfigurationProperty("controllerId", IsRequired = true)]
        public byte ControllerId
        {
            get { return (byte)this["controllerId"]; }
            set { this["controllerId"] = value; }
        }
    }
}