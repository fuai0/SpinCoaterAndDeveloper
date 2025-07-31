using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.App.Common.Models
{
    public class IOInputInfoModel : BindableBase
    {
        private int _Id;
        public int Id
        {
            get { return _Id; }
            set { SetProperty(ref _Id, value); }
        }

        private string _Number;
        public string Number
        {
            get { return _Number; }
            set { SetProperty(ref _Number, value); }
        }

        private string _PhysicalLocation;
        public string PhysicalLocation
        {
            get { return _PhysicalLocation; }
            set { SetProperty(ref _PhysicalLocation, value); }
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

        private bool _ReverseEnable;
        public bool ReverseEnable
        {
            get { return _ReverseEnable; }
            set { SetProperty(ref _ReverseEnable, value); }
        }

        private bool _ShieldEnable;
        public bool ShieldEnable
        {
            get { return _ShieldEnable; }
            set { SetProperty(ref _ShieldEnable, value); }
        }

        private bool _ShiedlEnableDefaultValue;

        public bool ShiedlEnableDefaultValue
        {
            get { return _ShiedlEnableDefaultValue; }
            set { SetProperty(ref _ShiedlEnableDefaultValue, value); }
        }

        private string _Backup;
        public string Backup
        {
            get { return _Backup; }
            set { SetProperty(ref _Backup, value); }
        }

        private string _Group;
        public string Group
        {
            get { return _Group; }
            set { SetProperty(ref _Group, value); }
        }

        private string _Tag;
        public string Tag
        {
            get { return _Tag; }
            set { SetProperty(ref _Tag, value); }
        }

        private int _ProgramAddressGroup;
        public int ProgramAddressGroup
        {
            get { return _ProgramAddressGroup; }
            set { SetProperty(ref _ProgramAddressGroup, value); }
        }

        private int _ProgramAddressPosition;
        public int ProgramAddressPosition
        {
            get { return _ProgramAddressPosition; }
            set { SetProperty(ref _ProgramAddressPosition, value); }
        }
    }
}
