using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SpinCoaterAndDeveloper.Shared.Models.MovementPointSecurityGraphModels
{
    public class SecurityConnectorModel : BindableBase
    {
        private SecurityNodeModel _ParentNode;
        public SecurityNodeModel ParentNode
        {
            get { return _ParentNode; }
            set { SetProperty(ref _ParentNode, value); }
        }
        private SecurityNodeModel _ConnectorNode = default;
        public SecurityNodeModel ConnectorNode
        {
            get { return _ConnectorNode; }
            set { SetProperty(ref _ConnectorNode, value); }
        }
        private Point _Anchor;
        public Point Anchor
        {
            get { return _Anchor; }
            set { SetProperty(ref _Anchor, value); }
        }
        private bool _IsConnected;
        public bool IsConnected
        {
            get { return _IsConnected; }
            set { SetProperty(ref _IsConnected, value); }
        }
        private bool _IsInput;
        public bool IsInput
        {
            get { return _IsInput; }
            set { SetProperty(ref _IsInput, value); }
        }
        private bool _HasConnected;
        public bool HasConnected
        {
            get { return _HasConnected; }
            set { SetProperty(ref _HasConnected, value); }
        }

        public void ConnectorReset()
        {
            ConnectorNode = default;
            HasConnected = false;
        }
    }
}
