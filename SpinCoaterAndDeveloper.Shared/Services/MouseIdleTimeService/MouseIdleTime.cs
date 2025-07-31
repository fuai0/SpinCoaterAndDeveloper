using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SpinCoaterAndDeveloper.Shared.Services.MouseIdelTimeService
{
    public class MouseIdleTime : IMouseIdleTime
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool GetCursorPos(out Point point);

        private DateTime latestMoveTime;
        private Point latestPoint;
        private readonly Timer timer;
        public MouseIdleTime()
        {
            latestMoveTime = DateTime.Now;
            latestPoint = new Point();
            timer = new Timer(new TimerCallback(obj =>
            {
                if (GetCursorPos(out Point point))
                {
                    if (latestPoint.X != point.X || latestPoint.Y != point.Y)
                    {
                        latestMoveTime = DateTime.Now;
                    }
                    latestPoint = point;
                }
            }), null, 0, 1000);
        }

        public double GetMouseIdleTimeSecondes()
        {
            return (DateTime.Now - latestMoveTime).TotalSeconds;
        }

        public void ResetIdleTime()
        {
            latestMoveTime = DateTime.Now;
        }

        ~MouseIdleTime()
        {
            timer.Dispose();
        }
    }
}
