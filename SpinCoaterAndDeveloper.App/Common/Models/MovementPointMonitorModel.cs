using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.App.Common.Models
{
    public class MovementPointMonitorModel : BindableBase
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
        private string _ShowOnUIName;
        public string ShowOnUIName
        {
            get { return _ShowOnUIName; }
            set { SetProperty(ref _ShowOnUIName, value); }
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

        private bool _ManualMoveSecurityEnable;
        public bool ManualMoveSecurityEnable
        {
            get { return _ManualMoveSecurityEnable; }
            set { SetProperty(ref _ManualMoveSecurityEnable, value); }
        }
        private double _ManualMoveSecurityTimeOut;
        public double ManualMoveSecurityTimeOut
        {
            get { return _ManualMoveSecurityTimeOut; }
            set { SetProperty(ref _ManualMoveSecurityTimeOut, value); }
        }
        /// <summary>
        /// 运动点相关轴数据集合
        /// </summary>
        public ObservableCollection<MovementPointPositionMonitorModel> MovementPointPositionsMonitorCollection { get; set; } = new ObservableCollection<MovementPointPositionMonitorModel>();
        /// <summary>
        /// 运动点位安全轴集合
        /// </summary>
        public ObservableCollection<MovementPointSecurityMonitorModel> MovementPointSecuritiesMonitorCollection { get; set; } = new ObservableCollection<MovementPointSecurityMonitorModel>();
    }
}
