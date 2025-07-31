using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.App.Common.Models
{
    public class FunctionShieldInfoModel : BindableBase
    {
        private int _Id;
        public int Id
        {
            get { return _Id; }
            set { SetProperty(ref _Id, value); }
        }
        private string _Name;
        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value); }
        }
        private string _CNName;
        public string CNName
        {
            get { return _CNName; }
            set { SetProperty(ref _CNName, value); }
        }
        private string _ENName;
        public string ENName
        {
            get { return _ENName; }
            set { SetProperty(ref _ENName, value); }
        }
        private string _VNName;
        public string VNName
        {
            get { return _VNName; }
            set { SetProperty(ref _VNName, value); }
        }
        private string _XXName;
        public string XXName
        {
            get { return _XXName; }
            set { SetProperty(ref _XXName, value); }
        }
        private bool _IsActive;
        public bool IsActive
        {
            get { return _IsActive; }
            set { SetProperty(ref _IsActive, value); }
        }
        private bool _EnableOnUI;
        public bool EnableOnUI
        {
            get { return _EnableOnUI; }
            set { SetProperty(ref _EnableOnUI, value); }
        }
        private string _Group;
        public string Group
        {
            get { return _Group; }
            set { SetProperty(ref _Group, value); }
        }
        private string _Backup;
        public string Backup
        {
            get { return _Backup; }
            set { SetProperty(ref _Backup, value); }
        }
        private int _ProductId;
        public int ProductId
        {
            get { return _ProductId; }
            set { SetProperty(ref _ProductId, value); }
        }
    }
}
