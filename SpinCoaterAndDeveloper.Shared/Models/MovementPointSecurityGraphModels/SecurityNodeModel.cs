using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SpinCoaterAndDeveloper.Shared.Models.MovementPointSecurityGraphModels
{
    public class SecurityNodeModel : BindableBase
    {
        private MovementPointSecurityTypes _NodeType;
        public MovementPointSecurityTypes NodeType
        {
            get { return _NodeType; }
            set { SetProperty(ref _NodeType, value); }
        }
        private string _NodeName;
        public string NodeName
        {
            get { return _NodeName; }
            set { SetProperty(ref _NodeName, value); }
        }
        private Point _Location;
        public Point Location
        {
            get { return _Location; }
            set { SetProperty(ref _Location, value); }
        }
        private bool _BoolValue;
        public bool BoolValue
        {
            get { return _BoolValue; }
            set { SetProperty(ref _BoolValue, value); }
        }
        private int _IntDelayTime;
        public int IntDelayTime
        {
            get { return _IntDelayTime; }
            set { SetProperty(ref _IntDelayTime, value); }
        }
        public ObservableCollection<SecurityConnectorModel> Inputs { get; set; } = new ObservableCollection<SecurityConnectorModel>();
        public ObservableCollection<SecurityConnectorModel> Outputs { get; set; } = new ObservableCollection<SecurityConnectorModel>();
    }
}
