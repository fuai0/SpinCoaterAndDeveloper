using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.App.Common.Models
{
    public class ProductivityShowModel : BindableBase
    {
        private string time;

        public string Time
        {
            get { return time; }
            set { SetProperty(ref time, value); }
        }
        private int totalNums;

        public int TotalNums
        {
            get { return totalNums; }
            set { SetProperty(ref totalNums, value); }
        }
        private int failNums;

        public int FailNums
        {
            get { return failNums; }
            set { SetProperty(ref failNums, value); }
        }
        private string percentage;

        public string Percentage
        {
            get { return percentage; }
            set { SetProperty(ref percentage, value); }
        }
    }
}
