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
    /// <summary>
    /// 复位流程最后结束指令需要添加[ActuationSuccessWithProcessBreak(true)]特性,以表示流程结束,结束管理线程
    /// </summary>
    public class ActuationGlobleResetGroupManager : ActuationManagerAbs
    {
        private readonly IContainerProvider containerProvider;

        public ActuationGlobleResetGroupManager(IContainerProvider containerProvider)
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
            VIOModel MachineResetEnable = new VIOModel() { Name = "MachineResetEnable", Status = false };

            dics.Add(MachineResetEnable.Name, MachineResetEnable);
            return dics;
        }

        public override Dictionary<string, object> DeclearSoftStatus()
        {
            Dictionary<string, object> dics = new Dictionary<string, object>();

            return dics;
        }
    }
}
