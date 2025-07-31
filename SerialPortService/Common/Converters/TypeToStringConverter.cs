using SerialPortService.Common.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SerialPortService.Converters
{
    public class TypeToStringConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            string str="";
            switch ((Common.Model.Type)value)
            {
                case Common.Model.Type.Write:
                    str = "Write";
                    break;
                case Common.Model.Type.Read:
                    str = "Read";
                    break;
                case Common.Model.Type.Error:
                    str = "Error";
                    break;
            }

            return str;

        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
