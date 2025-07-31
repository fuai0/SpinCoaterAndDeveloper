using MotionCardServiceInterface;
using Prism.Mvvm;
using SpinCoaterAndDeveloper.Shared.Models.MotionControlModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.App.Common.Models
{
    public class MovementPointPositionMonitorModel : BindableBase
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

        private MovementType _MovementPointType;
        public MovementType MovementPointType
        {
            get { return _MovementPointType; }
            set { SetProperty(ref _MovementPointType, value); }
        }

        private int _AxisInfoId;
        public int AxisInfoId
        {
            get { return _AxisInfoId; }
            set { SetProperty(ref _AxisInfoId, value); }
        }

        private double _AbsValue;
        public double AbsValue
        {
            get { return _AbsValue; }
            set { SetProperty(ref _AbsValue, value); }
        }

        private double _Vel;
        public double Vel
        {
            get { return _Vel; }
            set { SetProperty(ref _Vel, value); }
        }

        private double _Acc;
        public double Acc
        {
            get { return _Acc; }
            set { SetProperty(ref _Acc, value); }
        }

        private double _Dec;

        public double Dec
        {
            get { return _Dec; }
            set { SetProperty(ref _Dec, value); }
        }

        private double _Offset;
        public double Offset
        {
            get { return _Offset; }
            set { SetProperty(ref _Offset, value); }
        }

        private double _RelValue;
        public double RelValue
        {
            get { return _RelValue; }
            set { SetProperty(ref _RelValue, value); }
        }

        private AxisMonitorModel _AxisInfo;
        public AxisMonitorModel AxisInfo
        {
            get { return _AxisInfo; }
            set { SetProperty(ref _AxisInfo, value); }
        }

        private bool _InvolveAxis;
        public bool InvolveAxis
        {
            get { return _InvolveAxis; }
            set { SetProperty(ref _InvolveAxis, value); }
        }

        private int _JogIOInputId;
        public int JogIOInputId
        {
            get { return _JogIOInputId; }
            set { SetProperty(ref _JogIOInputId, value); }
        }

        private IOInputMonitorModel _JogIOInputInfo;
        public IOInputMonitorModel JogIOInputInfo
        {
            get { return _JogIOInputInfo; }
            set { SetProperty(ref _JogIOInputInfo, value); }
        }

        private JogArrivedType _JogArrivedCondition;
        public JogArrivedType JogArrivedCondition
        {
            get { return _JogArrivedCondition; }
            set { SetProperty(ref _JogArrivedCondition, value); }
        }

        private Direction _JogDirection;
        public Direction JogDirection
        {
            get { return _JogDirection; }
            set { SetProperty(ref _JogDirection, value); }
        }
    }
}
