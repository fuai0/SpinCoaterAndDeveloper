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

namespace TemperatureControllerService.Converters
{
    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = "";
            switch (parameter)
            {
                case "1":
                    if ((bool)value)
                    {
                        str = Application.Current.TryFindResource("SerialPort_ConnectionSuccessful").ToString();
                    }
                    else
                    {
                        //使用FindResource，当找不到资源时，会引发异常
                        //Application.Current.FindResource("ConnectionFail").ToString();
                        //使用TryFindResource，当找不到资源时，会返回null，并不会引发异常
                        str = Application.Current.TryFindResource("SerialPort_ConnectionFail").ToString();
                    }
                    break;
                case "2":
                    if ((bool)value)
                    {
                        str = Application.Current.TryFindResource("SerialPort_CodeScanSuccessful").ToString();
                    }
                    else
                    {
                        str = Application.Current.TryFindResource("SerialPort_CodeScanFail").ToString();
                    }
                    break;
            }

            return str;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
