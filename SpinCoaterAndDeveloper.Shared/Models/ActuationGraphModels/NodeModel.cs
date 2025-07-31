using MotionControlActuation;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SpinCoaterAndDeveloper.Shared.Models.ActuationGraphModels
{
    public class NodeModel : BindableBase
    {
        public string ActuationName { get; set; }

        private double _Row;
        public double Row
        {
            get { return _Row; }
            set { SetProperty(ref _Row, value); }
        }
        private double _Col;
        public double Col
        {
            get { return _Col; }
            set { SetProperty(ref _Col, value); }
        }

        private Point _Location;
        public Point Location
        {
            get { return _Location; }
            set { SetProperty(ref _Location, value); }
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
        private DateTime _StartTime;
        public DateTime StartTime
        {
            get { return _StartTime; }
            set { SetProperty(ref _StartTime, value); }
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

        public ObservableCollection<ConnectorModel> Inputs { get; set; } = new ObservableCollection<ConnectorModel>();
        public ObservableCollection<ConnectorModel> Outputs { get; set; } = new ObservableCollection<ConnectorModel>();
    }
}
