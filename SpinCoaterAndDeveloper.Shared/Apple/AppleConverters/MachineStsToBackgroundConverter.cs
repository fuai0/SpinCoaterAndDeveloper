using SpinCoaterAndDeveloper.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace SpinCoaterAndDeveloper.Shared.Apple.AppleConverters
{
    public class MachineStsToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value.ToString() == FSMStateCode.GlobleResetting.TryFindResourceEx())
            {
                return new SolidColorBrush(Color.FromRgb(204, 171, 216));
            }
            else if (value.ToString() == FSMStateCode.Idling.TryFindResourceEx())
            {
                return new SolidColorBrush(Color.FromRgb(255, 215, 212));
            }
            else if (value.ToString() == FSMStateCode.Alarming.TryFindResourceEx())
            {
                return new SolidColorBrush(Color.FromRgb(235, 115, 115));
            }
            return new SolidColorBrush(Color.FromRgb(236, 236, 236));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
