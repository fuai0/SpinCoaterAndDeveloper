using Prism.Mvvm;
using SpinCoaterAndDeveloper.Shared.DataEntity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.Models.InterpolationModels
{
    public class InterpolationPathCoordinateModel : BindableBase
    {
        private int id;

        public int Id
        {
            get { return id; }
            set { SetProperty(ref id, value); }
        }

        private string pathName;
        //路径名称
        public string PathName
        {
            get { return pathName; }
            set { SetProperty(ref pathName, value); }
        }
        private int interpolationCoordinateID;

        public int InterpolationCoordinateID
        {
            get { return interpolationCoordinateID; }
            set { SetProperty(ref interpolationCoordinateID, value); }
        }

        //X
        private bool enableAxisX = true;

        public bool EnableAxisX
        {
            get { return enableAxisX; }
            set { SetProperty(ref enableAxisX, value); }
        }

        private AxisInfoEntity axisX;

        public AxisInfoEntity AxisX
        {
            get { return axisX; }
            set { SetProperty(ref axisX, value); }
        }
        private double beginningX;

        public double BeginningX
        {
            get { return beginningX; }
            set { SetProperty(ref beginningX, value); }
        }

        private double beginningXVel;

        public double BeginningXVel
        {
            get { return beginningXVel; }
            set { SetProperty(ref beginningXVel, value); }
        }
        private double beginningXAcc;

        public double BeginningXAcc
        {
            get { return beginningXAcc; }
            set { SetProperty(ref beginningXAcc, value); }
        }
        private double beginningXDec;

        public double BeginningXDec
        {
            get { return beginningXDec; }
            set { SetProperty(ref beginningXDec, value); }
        }


        //Y
        private bool enableAxisY = true;

        public bool EnableAxisY
        {
            get { return enableAxisY; }
            set { SetProperty(ref enableAxisY, value); }
        }
        private AxisInfoEntity axisY;

        public AxisInfoEntity AxisY
        {
            get { return axisY; }
            set { SetProperty(ref axisY, value); }
        }
        private double beginningY;

        public double BeginningY
        {
            get { return beginningY; }
            set { SetProperty(ref beginningY, value); }
        }

        private double beginningYVel;

        public double BeginningYVel
        {
            get { return beginningYVel; }
            set { SetProperty(ref beginningYVel, value); }
        }
        private double beginningYAcc;

        public double BeginningYAcc
        {
            get { return beginningYAcc; }
            set { SetProperty(ref beginningYAcc, value); }
        }
        private double beginningYDec;

        public double BeginningYDec
        {
            get { return beginningYDec; }
            set { SetProperty(ref beginningYDec, value); }
        }
        //Z
        private bool enableAxisZ = true;

        public bool EnableAxisZ
        {
            get { return enableAxisZ; }
            set { SetProperty(ref enableAxisZ, value); }
        }
        private AxisInfoEntity axisZ;

        public AxisInfoEntity AxisZ
        {
            get { return axisZ; }
            set { SetProperty(ref axisZ, value); }
        }
        private double beginningZ;

        public double BeginningZ
        {
            get { return beginningZ; }
            set { SetProperty(ref beginningZ, value); }
        }
        private double beginningZVel;

        public double BeginningZVel
        {
            get { return beginningZVel; }
            set { SetProperty(ref beginningZVel, value); }
        }
        private double beginningZAcc;

        public double BeginningZAcc
        {
            get { return beginningZAcc; }
            set { SetProperty(ref beginningZAcc, value); }
        }
        private double beginningZDec;

        public double BeginningZDec
        {
            get { return beginningZDec; }
            set { SetProperty(ref beginningZDec, value); }
        }
        //R
        private bool enableAxisR = false;

        public bool EnableAxisR
        {
            get { return enableAxisR; }
            set { SetProperty(ref enableAxisR, value); }
        }
        private AxisInfoEntity axisR;

        public AxisInfoEntity AxisR
        {
            get { return axisR; }
            set { SetProperty(ref axisR, value); }
        }
        private double beginningR;

        public double BeginningR
        {
            get { return beginningR; }
            set { SetProperty(ref beginningR, value); }
        }
        private double beginningRVel;

        public double BeginningRVel
        {
            get { return beginningRVel; }
            set { SetProperty(ref beginningRVel, value); }
        }
        private double beginningRAcc;

        public double BeginningRAcc
        {
            get { return beginningRAcc; }
            set { SetProperty(ref beginningRAcc, value); }
        }
        private double beginningRDec;

        public double BeginningRDec
        {
            get { return beginningRDec; }
            set { SetProperty(ref beginningRDec, value); }
        }
        //A
        private bool enableAxisA = false;

        public bool EnableAxisA
        {
            get { return enableAxisA; }
            set { SetProperty(ref enableAxisA, value); }
        }
        private AxisInfoEntity axisA;

        public AxisInfoEntity AxisA
        {
            get { return axisA; }
            set { SetProperty(ref axisA, value); }
        }
        private double beginningA;

        public double BeginningA
        {
            get { return beginningA; }
            set { SetProperty(ref beginningA, value); }
        }
        private double beginningAVel;

        public double BeginningAVel
        {
            get { return beginningAVel; }
            set { SetProperty(ref beginningAVel, value); }
        }
        private double beginningAAcc;

        public double BeginningAAcc
        {
            get { return beginningAAcc; }
            set { SetProperty(ref beginningAAcc, value); }
        }
        private double beginningADec;

        public double BeginningADec
        {
            get { return beginningADec; }
            set { SetProperty(ref beginningADec, value); }
        }
        private int _ProductId;
        public int ProductId
        {
            get { return _ProductId; }
            set { SetProperty(ref _ProductId, value); }
        }
        //路径集合
        public ObservableCollection<InterpolationPathEditModel> InterpolationPaths { get; set; } = new ObservableCollection<InterpolationPathEditModel>();
    }
}
