using MotionControlActuation;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.Models.ActuationGraphModels
{
    public class ConnectionModel : BindableBase
    {
        private bool _Redirect;
        public bool Redirect
        {
            get { return _Redirect; }
            set { SetProperty(ref _Redirect, value); }
        }

        public ConnectorModel Source { get; set; }
        public ConnectorModel Target { get; set; }
    }
}
