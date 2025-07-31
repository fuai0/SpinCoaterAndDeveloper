using Prism.Mvvm;
using SpinCoaterAndDeveloper.Shared.Models.MovementPointSecurityGraphModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.App.Common.Models
{
    public class MovementPointSecurityModel : BindableBase
    {
        private int _Id;
        public int Id
        {
            get { return _Id; }
            set { SetProperty(ref _Id, value); }
        }
        private int _MovementPointNameId;
        public int MovementPointNameId
        {
            get { return _MovementPointNameId; }
            set { SetProperty(ref _MovementPointNameId, value); }
        }
        private int _Sequence;
        public int Sequence
        {
            get { return _Sequence; }
            set { SetProperty(ref _Sequence, value); }
        }
        private string _Name;
        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value); }
        }
        private MovementPointSecurityTypes _SecurityTypes;
        public MovementPointSecurityTypes SecurityTypes
        {
            get { return _SecurityTypes; }
            set { SetProperty(ref _SecurityTypes, value); }
        }
        private bool _BoolSecurityTypeValue;
        public bool BoolSecurityTypeValue
        {
            get { return _BoolSecurityTypeValue; }
            set { SetProperty(ref _BoolSecurityTypeValue, value); }
        }
        private int _IOOutputSecurityTypeDelayValue;
        public int IOOutputSecurityTypeDelayValue
        {
            get { return _IOOutputSecurityTypeDelayValue; }
            set { SetProperty(ref _IOOutputSecurityTypeDelayValue, value); }
        }
    }
}
