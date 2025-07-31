using GluePumpService.Service;
using GluePumpService.ViewModels;
using GluePumpService.Views;
using Prism.Ioc;
using Prism.Modularity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static System.Net.Mime.MediaTypeNames;
using Application = System.Windows.Application;

namespace GluePumpService
{
    public class GluePumpServiceModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/SerialPortService;component/Resources/zh-cn.xaml") });
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/SerialPortService;component/Resources/en-us.xaml") });

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<GluePumpView, GluePumpViewModel>();

            GluePumpConfig SerialPortConfig = new GluePumpConfig();
            var services = SerialPortConfig.LoadConfigFromFile();

            if (services != null)
            {
                foreach (ServiceConfigurationElement service in services.Services)
                {
                    GluePumpHelper ModbusClient = new GluePumpHelper();
                    ModbusClient.IocName = service.IocName;
                    containerRegistry.RegisterInstance(typeof(GluePumpHelper), ModbusClient, service.IocName);
                }
            }
        }
    }
}
