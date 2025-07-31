using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SpinCoaterAndDeveloper.Shared.Models.ActuationGraphModels
{
    public class ConnectorModel : BindableBase
    {
        private NodeModel _ParentNode;
        public NodeModel ParentNode
        {
            get { return _ParentNode; }
            set { SetProperty(ref _ParentNode, value); }
        }
        public string ActuationName { get; set; }

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
        private bool _Redirect;
        public bool Redirect
        {
            get { return _Redirect; }
            set { SetProperty(ref _Redirect, value); }
        }
    }
}
