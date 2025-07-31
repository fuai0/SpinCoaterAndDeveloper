using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.Event
{
    public class LaunchInfoEventModel
    {
        public string LaunchProgressInfo { get; set; }
        public string Filter { get; set; }
    }
    public class LaunchInfoEvent : PubSubEvent<LaunchInfoEventModel>
    {
    }
}
