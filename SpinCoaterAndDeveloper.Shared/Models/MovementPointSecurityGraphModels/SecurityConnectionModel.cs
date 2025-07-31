using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.Models.MovementPointSecurityGraphModels
{
    public class SecurityConnectionModel : BindableBase
    {
        public SecurityConnectorModel Source { get; set; }
        public SecurityConnectorModel Target { get; set; }
    }
}
