using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.App.Common.Models
{
    public class ProductCTShowModel : BindableBase
    {
        private string time;

        public string Time
        {
            get { return time; }
            set { SetProperty(ref time, value); }
        }
        private int nums;

        public int Nums
        {
            get { return nums; }
            set { SetProperty(ref nums, value); }
        }

        private double avg;

        public double Avg
        {
            get { return avg; }
            set { SetProperty(ref avg, value); }
        }
    }
}
