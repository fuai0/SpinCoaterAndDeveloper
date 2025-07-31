using MotionControlActuation;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SpinCoaterAndDeveloper.App.Common.Converters
{
    public class AxisTypeToVMConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if (ConfigurationManager.AppSettings["Language"] == "CN")
                {
                    switch ((AxisType)value)
                    {
                        case AxisType.LineAxis:
                            return "直线轴";
                        case AxisType.RotateAxis:
                            return "旋转轴";
                        default:
                            break;
                    }
                }
                else
                {
                    switch ((AxisType)value)
                    {
                        case AxisType.LineAxis:
                            return "Line Axis";
                        case AxisType.RotateAxis:
                            return "Rotate Axis";
                        default:
                            break;
                    }
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if (ConfigurationManager.AppSettings["Language"] == "CN")
                {
                    switch (value.ToString())
                    {
                        case "直线轴":
                            return AxisType.LineAxis;
                        case "旋转轴":
                            return AxisType.RotateAxis;
                        default:
                            break;
                    }
                }
                else
                {
                    switch (value.ToString())
                    {
                        case "Line Axis":
                            return AxisType.LineAxis;
                        case "Rotate Axis":
                            return AxisType.RotateAxis;
                        default:
                            break;
                    }
                }
            }
            return value;
        }
    }
}
