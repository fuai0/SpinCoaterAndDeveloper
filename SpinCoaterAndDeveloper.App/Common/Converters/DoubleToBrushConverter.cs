using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace SpinCoaterAndDeveloper.App.Common.Converters
{
    public class DoubleToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var data = (int)value;
            switch (data)
            {
                case int x when (x >= 0 && x <= 25):
                    return new SolidColorBrush(Color.FromArgb(255, 83, 172, 122));
                case int x when (x > 25 && x <= 50):
                    return new SolidColorBrush(Color.FromArgb(255, 84, 151, 193));
                case int x when (x > 50 && x <= 75):
                    return new SolidColorBrush(Color.FromArgb(255, 243, 150, 91));
                case int x when (x > 75 && x <= 100):
                    return new SolidColorBrush(Color.FromArgb(255, 228, 94, 105));
                default:
                    break;
            }
            return new SolidColorBrush(Color.FromRgb(111, 121, 128));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
