using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using PrismAutomationPlatformExt;
using SpinCoaterAndDeveloper.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class LaunchViewModel : BindableBase, IDialogAware
    {
        private readonly IEventAggregator eventAggregator;

        private string launchInfo;

        public event Action<IDialogResult> RequestClose;

        public string LaunchInfo
        {
            get { return launchInfo; }
            set { SetProperty(ref launchInfo, value); }
        }

        public string Title { get; set; }

        public LaunchViewModel(IContainerProvider containerProvider)
        {
            this.eventAggregator = containerProvider.Resolve<IEventAggregator>();
            this.eventAggregator.RegisterLaunchInfo(x => { LaunchInfo = x.LaunchProgressInfo; });
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {

        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            var configureService = parameters.GetValue<IConfigureService>("ConfigureService");
            Task.Run(() =>
            {
                //延时已加载界面,防止速度过快界面未显示就已结束
                Thread.Sleep(500);
                configureService?.ConfigureAsync();
                Thread.Sleep(500);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    RequestClose?.Invoke(new DialogResult(ButtonResult.None));
                });
            });
        }
    }
}
