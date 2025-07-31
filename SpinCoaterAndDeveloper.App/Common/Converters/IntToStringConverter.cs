using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SpinCoaterAndDeveloper.App.Common.Converters
{
    public class IntToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!string.IsNullOrWhiteSpace(value.ToString()))
            {
                int temp = 0;
                bool result = int.TryParse(value.ToString(), out temp);
                if (result)
                    return temp;
                else
                    return 99;
            }
            else
                return 99;
        }
    }
}
