using LogServiceInterface;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpinCoaterAndDeveloper.App.ViewModels
{
    public class VisionCalibrationViewModel : BindableBase, INavigationAware
    {
        private readonly ILogService logService;
        public VisionCalibrationViewModel(IContainerProvider containerProvider)
        {
            this.logService = containerProvider.Resolve<ILogService>();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {

        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {

        }
    }
}
