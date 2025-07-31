using SpinCoaterAndDeveloper.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace SpinCoaterAndDeveloper.App.Common.Converters
{
    public class ShortToHomeMethodConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value)
            {
                case 0:
                    return "#0Null".TryFindResourceEx();
                case 1:
                    return "#1NegativeLimitSwitch+ZSignalFromTheEncoder".TryFindResourceEx();
                case 2:
                    return "#2PositiveLimitSwitch+ZSignalFromTheEncoder".TryFindResourceEx();
                case 3:
                    return "#3PositiveOriginSwitch+ZSignalFromTheEncoder".TryFindResourceEx();
                case 4:
                    return "#4PositiveOriginSwitch+ZSignalFromTheEncoder".TryFindResourceEx();
                case 5:
                    return "#5NegativeOriginSwitch+ZSignalFromTheEncoder".TryFindResourceEx();
                case 6:
                    return "#6NegativeOriginSwitch+ZSignalFromTheEncoder".TryFindResourceEx();
                case 7:
                    return "#7PositiveLimitSwitch+OriginSwitch+ZSignalFromTheEncoder".TryFindResourceEx();
                case 8:
                    return "#8PositiveLimitSwitch+OriginSwitch+ZSignalFromTheEncoder".TryFindResourceEx();
                case 9:
                    return "#9PositiveLimitSwitch+OriginSwitch+ZSignalFromTheEncoder".TryFindResourceEx();
                case 10:
                    return "#10PositiveLimitSwitch+OriginSwitch+ZSignalFromTheEncoder".TryFindResourceEx();
                case 11:
                    return "#11NegativeLimitSwitch+OriginSwitch+ZSignalFromTheEncoder".TryFindResourceEx();
                case 12:
                    return "#12NegativeLimitSwitch+OriginSwitch+ZSignalFromTheEncoder".TryFindResourceEx();
                case 13:
                    return "#13NegativeLimitSwitch+OriginSwitch+ZSignalFromTheEncoder".TryFindResourceEx();
                case 14:
                    return "#14NegativeLimitSwitch+OriginSwitch+ZSignalFromTheEncoder".TryFindResourceEx();
                case 15:
                    return $"#15{"Reserve".TryFindResourceEx()}";
                case 16:
                    return $"#16{"Reserve".TryFindResourceEx()}";
                case 17:
                    return "#17NegativeLimitSwitch".TryFindResourceEx();
                case 18:
                    return "#18PositiveLimitSwitch".TryFindResourceEx();
                case 19:
                    return "#19PositiveOriginSwitch".TryFindResourceEx();
                case 20:
                    return "#20PositiveOriginSwitch".TryFindResourceEx();
                case 21:
                    return "#21NegativeOriginSwitch".TryFindResourceEx();
                case 22:
                    return "#22NegativeOriginSwitch".TryFindResourceEx();
                case 23:
                    return "#23PositiveLimitSwitch+OriginSwitch".TryFindResourceEx();
                case 24:
                    return "#24PositiveLimitSwitch+OriginSwitch".TryFindResourceEx();
                case 25:
                    return "#25PositiveLimitSwitch+OriginSwitch".TryFindResourceEx();
                case 26:
                    return "#26PositiveLimitSwitch+OriginSwitch".TryFindResourceEx();
                case 27:
                    return "#27NegativeLimitSwitch+OriginSwitch".TryFindResourceEx();
                case 28:
                    return "#28NegativeLimitSwitch+OriginSwitch".TryFindResourceEx();
                case 29:
                    return "#29NegativeLimitSwitch+OriginSwitch".TryFindResourceEx();
                case 30:
                    return "#30NegativeLimitSwitch+OriginSwitch".TryFindResourceEx();
                case 31:
                    return $"#31{"Reserve".TryFindResourceEx()}";
                case 32:
                    return $"#32{"Reserve".TryFindResourceEx()}";
                case 33:
                    return "#33ZSignalFromTheEncoder".TryFindResourceEx();
                case 34:
                    return "#34ZSignalFromTheEncoder".TryFindResourceEx();
                case 35:
                    return "#35ZSignalFromTheEncoder(ServoDoesNotRequireOPStatus)".TryFindResourceEx();
                default:
                    return "未知".TryFindResourceEx();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (((string)value).ToCharArray()[2] >= '0' && ((string)value).ToCharArray()[2] <= '9')
            {
                value = ((string)value).Substring(0, 3);
            }
            else
            {
                value = ((string)value).Substring(0, 2);
            }

            switch (value)
            {
                case "#0":
                    return 0;
                case "#1":
                    return 1;
                case "#2":
                    return 2;
                case "#3":
                    return 3;
                case "#4":
                    return 4;
                case "#5":
                    return 5;
                case "#6":
                    return 6;
                case "#7":
                    return 7;
                case "#8":
                    return 8;
                case "#9":
                    return 9;
                case "#10":
                    return 10;
                case "#11":
                    return 11;
                case "#12":
                    return 12;
                case "#13":
                    return 13;
                case "#14":
                    return 14;
                case "#15":
                    return 15;
                case "#16":
                    return 16;
                case "#17":
                    return 17;
                case "#18":
                    return 18;
                case "#19":
                    return 19;
                case "#20":
                    return 20;
                case "#21":
                    return 21;
                case "#22":
                    return 22;
                case "#23":
                    return 23;
                case "#24":
                    return 24;
                case "#25":
                    return 25;
                case "#26":
                    return 26;
                case "#27":
                    return 27;
                case "#28":
                    return 28;
                case "#29":
                    return 29;
                case "#30":
                    return 30;
                case "#31":
                    return 31;
                case "#32":
                    return 32;
                case "#33":
                    return 33;
                case "#34":
                    return 34;
                case "#35":
                    return 35;
                default:
                    return 0;
            }
        }
    }
}
