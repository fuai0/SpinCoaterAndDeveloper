using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.Event
{
    public class UpdateLoadingModel
    {
        public bool IsOpen { get; set; }
    }
    public class UpdateLoadingEvent : PubSubEvent<UpdateLoadingModel>
    {
    }
}
