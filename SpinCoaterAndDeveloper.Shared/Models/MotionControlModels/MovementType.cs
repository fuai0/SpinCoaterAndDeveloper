using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.Models.MotionControlModels
{
    public enum MovementType
    {
        Abs,
        Rel,
        Jog,
    }
    public enum JogArrivedType
    {
        Input,
        NoInput,
    }
}
