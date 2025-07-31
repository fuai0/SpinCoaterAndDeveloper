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
    public class ActuationBurnInGroupManager : ActuationManagerAbs
    {
        private readonly IContainerProvider containerProvider;
        public ActuationBurnInGroupManager(IContainerProvider containerProvider)
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
            VIOModel MachineBurnInEnabel = new VIOModel() { Name = "MachineBurnInEnable", Status = false };

            dics.Add(MachineBurnInEnabel.Name, MachineBurnInEnabel);
            return dics;
        }

        public override Dictionary<string, object> DeclearSoftStatus()
        {
            Dictionary<string, object> dics = new Dictionary<string, object>();

            return dics;
        }
    }
}
