using Prism.Mvvm;
using SpinCoaterAndDeveloper.Shared.Models.CylinderModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.App.Common.Models
{
    public class CylinderMonitorModel : BindableBase
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
        private double _OriginPointTimeout;
        public double OriginPointTimeout
        {
            get { return _OriginPointTimeout; }
            set { SetProperty(ref _OriginPointTimeout, value); }
        }
        private double _MovingPointTimeout;
        public double MovingPointTimeout
        {
            get { return _MovingPointTimeout; }
            set { SetProperty(ref _MovingPointTimeout, value); }
        }

        private ValveType _ValveType;
        public ValveType ValveType
        {
            get { return _ValveType; }
            set { SetProperty(ref _ValveType, value); }
        }
        private int _SingleValveOutputId;
        public int SingleValveOutputId
        {
            get { return _SingleValveOutputId; }
            set { SetProperty(ref _SingleValveOutputId, value); }
        }
        private IOOutputMonitorModel _SingleValveOutputInfo;
        public IOOutputMonitorModel SingleValveOutputInfo
        {
            get { return _SingleValveOutputInfo; }
            set { SetProperty(ref _SingleValveOutputInfo, value); }
        }

        private int _DualValveOriginOutputId;
        public int DualValveOriginOutputId
        {
            get { return _DualValveOriginOutputId; }
            set { SetProperty(ref _DualValveOriginOutputId, value); }
        }
        private IOOutputMonitorModel _DualValveOriginOutputInfo;
        public IOOutputMonitorModel DualValveOriginOutputInfo
        {
            get { return _DualValveOriginOutputInfo; }
            set { SetProperty(ref _DualValveOriginOutputInfo, value); }
        }
        private int _DualValveMovingOutputId;
        public int DualValveMovingOutputId
        {
            get { return _DualValveMovingOutputId; }
            set { SetProperty(ref _DualValveMovingOutputId, value); }
        }
        private IOOutputMonitorModel _DualValveMovingOutputInfo;
        public IOOutputMonitorModel DualValveMovingOutputInfo
        {
            get { return _DualValveMovingOutputInfo; }
            set { SetProperty(ref _DualValveMovingOutputInfo, value); }
        }

        private SensorType _SensorType;
        public SensorType SensorType
        {
            get { return _SensorType; }
            set { SetProperty(ref _SensorType, value); }
        }
        private double _DelayTime;
        public double DelayTime
        {
            get { return _DelayTime; }
            set { SetProperty(ref _DelayTime, value); }
        }

        private int _SensorOriginInputId;
        public int SensorOriginInputId
        {
            get { return _SensorOriginInputId; }
            set { SetProperty(ref _SensorOriginInputId, value); }
        }
        private IOInputMonitorModel _SensorOriginInputInfo;
        public IOInputMonitorModel SensorOriginInputInfo
        {
            get { return _SensorOriginInputInfo; }
            set { SetProperty(ref _SensorOriginInputInfo, value); }
        }
        private int _SensorMovingInputId;
        public int SensorMovingInputId
        {
            get { return _SensorMovingInputId; }
            set { SetProperty(ref _SensorMovingInputId, value); }
        }
        private IOInputMonitorModel _SensorMovingInputInfo;
        public IOInputMonitorModel SensorMovingInputInfo
        {
            get { return _SensorMovingInputInfo; }
            set { SetProperty(ref _SensorMovingInputInfo, value); }
        }
        private bool _ShiedSensorOriginInput;
        public bool ShiedSensorOriginInput
        {
            get { return _ShiedSensorOriginInput; }
            set { SetProperty(ref _ShiedSensorOriginInput, value); }
        }
        private double _ShiedSensorOriginInputDelayTime;
        public double ShiedSensorOriginInputDelayTime
        {
            get { return _ShiedSensorOriginInputDelayTime; }
            set { SetProperty(ref _ShiedSensorOriginInputDelayTime, value); }
        }
        private bool _ShiedSensorMovingInput;
        public bool ShiedSensorMovingInput
        {
            get { return _ShiedSensorMovingInput; }
            set { SetProperty(ref _ShiedSensorMovingInput, value); }
        }
        private double _ShiedSensorMovingInputDelayTime;
        public double ShiedSensorMovingInputDelayTime
        {
            get { return _ShiedSensorMovingInputDelayTime; }
            set { SetProperty(ref _ShiedSensorMovingInputDelayTime, value); }
        }
    }
}
