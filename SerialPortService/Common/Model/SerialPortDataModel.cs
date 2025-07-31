using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialPortService.Common.Model
{
    public class SerialPortDataModel:BindableBase
    {
        private DateTime time;

        public DateTime Time
        {
            get { return time; }
            set { SetProperty(ref time, value); }
        }

        private Type type;

        public Type Type
        {
            get { return type; }
            set { SetProperty(ref type, value); }
        }

        private string data;

        public string Data
        {
            get { return data; }
            set { SetProperty(ref data, value); }
        }
    }
    public enum  Type
    {
        Write,
        Read,
        Error,
    }
}
