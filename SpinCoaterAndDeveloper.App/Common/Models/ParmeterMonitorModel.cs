using Prism.Mvvm;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using SpinCoaterAndDeveloper.Shared.Models.MotionControlModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.App.Common.Models
{
    public class ParmeterMonitorModel : BindableBase
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
        private string _ShowOnUIName;
        public string ShowOnUIName
        {
            get { return _ShowOnUIName; }
            set { SetProperty(ref _ShowOnUIName, value); }
        }
        private string _Data;
        public string Data
        {
            get { return _Data; }
            set { SetProperty(ref _Data, value); }
        }
        private ParmeterType _DataType;
        public ParmeterType DataType
        {
            get { return _DataType; }
            set { SetProperty(ref _DataType, value); }
        }
        private string _Unit;
        public string Unit
        {
            get { return _Unit; }
            set { SetProperty(ref _Unit, value); }
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
        private string _Tag;
        public string Tag
        {
            get { return _Tag; }
            set { SetProperty(ref _Tag, value); }
        }
        private int _ProductId;
        public int ProductId
        {
            get { return _ProductId; }
            set { SetProperty(ref _ProductId, value); }
        }
        private ProductInfoEntity _ProductInfo;
        public ProductInfoEntity ProductInfo
        {
            get { return _ProductInfo; }
            set { SetProperty(ref _ProductInfo, value); }
        }
        private bool _ErrorMark = true;
        public bool ErrorMark
        {
            get { return _ErrorMark; }
            set { SetProperty(ref _ErrorMark, value); }
        }
    }
}
