using MotionControlActuation;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.App.Common.Models
{
    public class ActuationMonitorModel : BindableBase
    {
        private string _Name;
        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value); }
        }
        private ActionStatus _Status;
        public ActionStatus Status
        {
            get { return _Status; }
            set { SetProperty(ref _Status, value); }
        }
        private long _Times;
        public long Times
        {
            get { return _Times; }
            set { SetProperty(ref _Times, value); }
        }
        private string _AsyncKeyWord;
        public string AsyncKeyWord
        {
            get { return _AsyncKeyWord; }
            set { SetProperty(ref _AsyncKeyWord, value); }
        }
        private string _AsyncRedirectName;
        public string AsyncRedirectName
        {
            get { return _AsyncRedirectName; }
            set { SetProperty(ref _AsyncRedirectName, value); }
        }
        private ActionStatus _AsyncStatus;
        public ActionStatus AsyncStatus
        {
            get { return _AsyncStatus; }
            set { SetProperty(ref _AsyncStatus, value); }
        }
        private DateTime _StartTime;
        public DateTime StartTime
        {
            get { return _StartTime; }
            set { SetProperty(ref _StartTime, value); }
        }
    }
}
