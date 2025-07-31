using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.App.Common.Models
{
    public class MenuBar : BindableBase
    {
        private string _Icon;
        public string Icon
        {
            get { return _Icon; }
            set { SetProperty(ref _Icon, value); }
        }

        private string _Title;
        public string Title
        {
            get { return _Title; }
            set { SetProperty(ref _Title, value); }
        }
        private string _NameSpace;
        public string NameSpace
        {
            get { return _NameSpace; }
            set { SetProperty(ref _NameSpace, value); }
        }
        private bool _IsShow;
        public bool IsShow
        {
            get { return _IsShow; }
            set { SetProperty(ref _IsShow, value); }
        }
        private string _LanguageKey;
        public string LanguageKey
        {
            get { return _LanguageKey; }
            set { SetProperty(ref _LanguageKey, value); }
        }
    }
}
