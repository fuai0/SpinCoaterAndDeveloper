using Prism.Ioc;
using Prism.Modularity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SerialPortService.Service;
using SerialPortServiceInterface;
using Prism.Services.Dialogs;
using System.Windows;
using SerialPortService.Views;
using SerialPortService.ViewModels;
using System.Net.Sockets;
using DryIoc;
using Prism.DryIoc;

namespace SerialPortService
{
    public class SerialPortServiceModule : IModule
    {

        public void OnInitialized(IContainerProvider containerProvider)
        {

            SerialPortConfig serialPortConfig = new SerialPortConfig();
            var services = serialPortConfig.LoadConfigFromFile();
            if (services != null)
            {
                foreach (ServiceConfigurationElement service in services.Services)
                {
                    var temp = containerProvider.Resolve<ISerialPort>(service.IocName);
                    if (!temp.Init(service.PortName, service.BaudRate, service.Parity, service.StopBits, service.DataBits, service.Timeout))
                    {
                        DialogParameters par = new DialogParameters();
                        par.Add("Title", $"提示");
                        par.Add("Content", $"{service.IocName} 打开失败");
                        containerProvider.Resolve<IDialogService>().ShowDialog("SerialPortMessageView", par, r => { });
                    }
                }
            }
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/SerialPortService;component/Resources/zh-cn.xaml") });
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/SerialPortService;component/Resources/en-us.xaml") });

        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<SerialPortDebugView, SerialPortDebugViewModel>();
            containerRegistry.RegisterDialog<SerialPortMessageView, SerialPortMessageViewModel>();

            SerialPortConfig SerialPortConfig = new SerialPortConfig();
            var services = SerialPortConfig.LoadConfigFromFile();

            if (services != null)
            {
                foreach (ServiceConfigurationElement service in services.Services)
                {
                    SerialPortHelper SerialPort = new SerialPortHelper();
                    SerialPort.IocName = service.IocName;
                    containerRegistry.RegisterInstance(typeof(ISerialPort), SerialPort, service.IocName);
                }
            }

        }
    }
}
