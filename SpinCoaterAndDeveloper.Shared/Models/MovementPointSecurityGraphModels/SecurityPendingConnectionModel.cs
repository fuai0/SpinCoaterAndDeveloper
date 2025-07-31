using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.Models.MovementPointSecurityGraphModels
{
    public class SecurityPendingConnectionModel : BindableBase
    {
        private bool _IsVisiable;
        public bool IsVisiable
        {
            get { return _IsVisiable; }
            set { SetProperty(ref _IsVisiable, value); }
        }
    }
}
