using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.Models.InterpolationModels
{
    public class InterpolationPathEditModel : BindableBase
    {
        private int id;

        public int Id
        {
            get { return id; }
            set { SetProperty(ref id, value); }
        }

        private int coordinateId;

        public int CoordinateId
        {
            get { return coordinateId; }
            set { SetProperty(ref coordinateId, value); }
        }

        private int sequence;

        public int Sequence
        {
            get { return sequence; }
            set { SetProperty(ref sequence, value); }
        }

        private InterpolationPathMode pathMode;

        public InterpolationPathMode PathMode
        {
            get { return pathMode; }
            set { SetProperty(ref pathMode, value); }
        }

        private double mx;

        public double MX
        {
            get { return mx; }
            set { SetProperty(ref mx, value); }
        }
        private double my;

        public double MY
        {
            get { return my; }
            set { SetProperty(ref my, value); }
        }
        private double mz;

        public double MZ
        {
            get { return mz; }
            set { SetProperty(ref mz, value); }
        }

        private double tx;

        public double TX
        {
            get { return tx; }
            set { SetProperty(ref tx, value); }
        }

        private double ty;

        public double TY
        {
            get { return ty; }
            set { SetProperty(ref ty, value); }
        }

        private double tz;

        public double TZ
        {
            get { return tz; }
            set { SetProperty(ref tz, value); }
        }

        private double tr;

        public double TR
        {
            get { return tr; }
            set { SetProperty(ref tr, value); }
        }

        private double ta;

        public double TA
        {
            get { return ta; }
            set { SetProperty(ref ta, value); }
        }

        private double speed;

        public double Speed
        {
            get { return speed; }
            set { SetProperty(ref speed, value); }
        }

        private double accSpeed;

        public double AccSpeed
        {
            get { return accSpeed; }
            set { SetProperty(ref accSpeed, value); }
        }

        private bool ioEnable;

        public bool IOEnable
        {
            get { return ioEnable; }
            set { SetProperty(ref ioEnable, value); }
        }

        private double startDelayTime;

        public double StartDelayTime
        {
            get { return startDelayTime; }
            set { SetProperty(ref startDelayTime, value); }
        }

        private bool startDelayIOEnable;

        public bool StartDelayIOEnable
        {
            get { return startDelayIOEnable; }
            set { SetProperty(ref startDelayIOEnable, value); }
        }

        private double endDelayTime;

        public double EndDelayTime
        {
            get { return endDelayTime; }
            set { SetProperty(ref endDelayTime, value); }
        }

        private bool endDelayIOEnable;

        public bool EndDelayIOEnable
        {
            get { return endDelayIOEnable; }
            set { SetProperty(ref endDelayIOEnable, value); }
        }

    }
}
