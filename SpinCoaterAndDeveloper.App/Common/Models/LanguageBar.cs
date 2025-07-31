using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.App.Common.Models
{
    public class LanguageBar : BindableBase
    {
        private string _Name;
        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value); }
        }
        private string _Mark;
        public string Mark
        {
            get { return _Mark; }
            set { SetProperty(ref _Mark, value); }
        }
    }
}
