using LogServiceInterface;
using MotionControlActuation;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Actuation.Actuation
{
    public class ActuationRunningGroupManager : ActuationManagerAbs
    {
        private readonly IContainerProvider containerProvider;

        public ActuationRunningGroupManager(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
        }
        public override Dictionary<string, ActuationAbs> DeclearActuations()
        {
            Dictionary<string, ActuationAbs> dics = new Dictionary<string, ActuationAbs>();

            return dics;
        }

        public override Dictionary<string, VIOModel> DeclearVIOs()
        {
            Dictionary<string, VIOModel> dics = new Dictionary<string, VIOModel>();
            VIOModel MachineRunningEnable = new VIOModel() { Name = "MachineRunningEnable", Status = false };

            dics.Add(MachineRunningEnable.Name, MachineRunningEnable);
            return dics;
        }

        public override Dictionary<string, object> DeclearSoftStatus()
        {
            Dictionary<string, object> dics = new Dictionary<string, object>();

            return dics;
        }
    }
}
