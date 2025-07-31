using Prism.Ioc;
using Prism.Modularity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TemperatureControllerService.Service;
using TemperatureControllerService.ViewModels;
using SerialPortServiceInterface;
using TemperatureControllerService.Views;
using Prism.Services.Dialogs;
using System.Windows;
using Application = System.Windows.Application;

namespace TemperatureControllerService
{
    public class TemperatureControllerServiceModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/SerialPortService;component/Resources/zh-cn.xaml") });
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/SerialPortService;component/Resources/en-us.xaml") });

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<TemperatureControllerView, TemperatureControllerViewModel>();

            TemperatureControllerConfig SerialPortConfig = new TemperatureControllerConfig();
            var services = SerialPortConfig.LoadConfigFromFile();

            if (services != null)
            {
                foreach (ServiceConfigurationElement service in services.Services)
                {
                    ModbusHelper ModbusClient = new ModbusHelper();
                    ModbusClient.IocName = service.IocName;
                    containerRegistry.RegisterInstance(typeof(ISerialPort), ModbusClient, service.IocName);
                }
            }
        }

    }
}
