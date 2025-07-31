using MotionCardServiceDelta.Service;
using MotionCardServiceInterface;
using Prism.Ioc;
using Prism.Modularity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionCardServiceDelta
{
    public class MotionCardServiceModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
          
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IMotionCardService, MotionCardServiceDeltaMC>();
        }
    }
}
