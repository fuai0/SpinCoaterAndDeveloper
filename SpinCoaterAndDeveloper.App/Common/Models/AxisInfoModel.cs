using MotionControlActuation;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.App.Common.Models
{
    public class AxisInfoModel : BindableBase
    {
        private int _Id;

        public int Id
        {
            get { return _Id; }
            set { SetProperty(ref _Id, value); }
        }

        private int _AxisIdOnCard;

        public int AxisIdOnCard
        {
            get { return _AxisIdOnCard; }
            set { SetProperty(ref _AxisIdOnCard, value); }
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

        private int _HomeMethod;

        public int HomeMethod
        {
            get { return _HomeMethod; }
            set { SetProperty(ref _HomeMethod, value); }
        }

        private double _HomeHighVel;

        public double HomeHighVel
        {
            get { return _HomeHighVel; }
            set { SetProperty(ref _HomeHighVel, value); }
        }

        private double _HomeLowVel;

        public double HomeLowVel
        {
            get { return _HomeLowVel; }
            set { SetProperty(ref _HomeLowVel, value); }
        }

        private double _HomeAcc;

        public double HomeAcc
        {
            get { return _HomeAcc; }
            set { SetProperty(ref _HomeAcc, value); }
        }

        private int _HomeTimeout;

        public int HomeTimeout
        {
            get { return _HomeTimeout; }
            set { SetProperty(ref _HomeTimeout, value); }
        }

        private int _Proportion;

        public int Proportion
        {
            get { return _Proportion; }
            set { SetProperty(ref _Proportion, value); }
        }

        private double _HomeOffset;

        public double HomeOffset
        {
            get { return _HomeOffset; }
            set { SetProperty(ref _HomeOffset, value); }
        }

        private AxisType _Type;
        public AxisType Type
        {
            get { return _Type; }
            set { SetProperty(ref _Type, value); }
        }

        private bool _SoftLimitEnable;
        public bool SoftLimitEnable
        {
            get { return _SoftLimitEnable; }
            set { SetProperty(ref _SoftLimitEnable, value); }
        }

        private double _SoftPositiveLimitPos;
        public double SoftPositiveLimitPos
        {
            get { return _SoftPositiveLimitPos; }
            set { SetProperty(ref _SoftPositiveLimitPos, value); }
        }

        private double _SoftNegativeLimitPos;
        public double SoftNegativeLimitPos
        {
            get { return _SoftNegativeLimitPos; }
            set { SetProperty(ref _SoftNegativeLimitPos, value); }
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

        private double _JogVel;

        public double JogVel
        {
            get { return _JogVel; }
            set { SetProperty(ref _JogVel, value); }
        }

        private bool _SafeAxisEnable;

        public bool SafeAxisEnable
        {
            get { return _SafeAxisEnable; }
            set { SetProperty(ref _SafeAxisEnable, value); }
        }

        private double _SafeAxisPosition;

        public double SafeAxisPosition
        {
            get { return _SafeAxisPosition; }
            set { SetProperty(ref _SafeAxisPosition, value); }
        }

        private double relMoveDistance;

        public double RelMoveDistance
        {
            get { return relMoveDistance; }
            set { SetProperty(ref relMoveDistance, value); }
        }
        private double _TargetLocationGap;
        public double TargetLocationGap
        {
            get { return _TargetLocationGap; }
            set { SetProperty(ref _TargetLocationGap, value); }
        }
    }
}
