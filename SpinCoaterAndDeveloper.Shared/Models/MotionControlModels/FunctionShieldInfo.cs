using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.Shared.Models.MotionControlModels
{
    public class FunctionShieldInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string CNName { get; set; }
        public string ENName { get; set; }
        public string VNName { get; set; }
        public string XXName { get; set; }
        public bool IsActive { get; set; }
        public bool EnableOnUI { get; set; }
        public string Group { get; set; }
        public string BackUp { get; set; }
        public int ProductId { get; set; }
    }
}
