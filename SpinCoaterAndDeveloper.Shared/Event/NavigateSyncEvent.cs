using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.Event
{
    public class NavigateSyncModel
    {
        public bool IsOpen { get; set; }
        public string Fliter { get; set; }
    }
    public class NavigateSyncDownEvent : PubSubEvent<NavigateSyncModel>
    {
    }
    public class NavigateSyncUpEvent : PubSubEvent<NavigateSyncModel>
    {
    }
}
