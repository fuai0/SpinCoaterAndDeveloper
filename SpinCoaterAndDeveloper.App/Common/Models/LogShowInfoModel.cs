using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpinCoaterAndDeveloper.App.Common.Models
{
    public class LogShowInfoModel : BindableBase
    {
        private int id;

        public int Id
        {
            get { return id; }
            set { SetProperty(ref id, value); }
        }
        private string level;

        public string Level
        {
            get { return level; }
            set { SetProperty(ref level, value); }
        }
        private string message;

        public string Message
        {
            get { return message; }
            set { SetProperty(ref message, value); }
        }
        private DateTime time;

        public DateTime Time
        {
            get { return time; }
            set { SetProperty(ref time, value); }
        }
    }
}
