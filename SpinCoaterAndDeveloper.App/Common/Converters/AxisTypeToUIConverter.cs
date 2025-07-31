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
    public class AxisTypeToUIConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                string[] data = new string[2];
                if (ConfigurationManager.AppSettings["Language"] == "CN")
                {
                    var temp = value as AxisType[];
                    for (int i = 0; i < temp.Count(); i++)
                    {
                        if (temp[i] == AxisType.LineAxis)
                        {
                            data[i] = "直线轴";
                        }
                        else if (temp[i] == AxisType.RotateAxis)
                        {
                            data[i] = "旋转轴";
                        }
                        else
                        {
                            data[i] = temp[i].ToString();
                        }
                    }
                    return data;
                }
                else
                {
                    var temp = value as AxisType[];
                    for (int i = 0; i < temp.Count(); i++)
                    {
                        if (temp[i] == AxisType.LineAxis)
                        {
                            data[i] = "Line Axis";
                        }
                        else if (temp[i] == AxisType.RotateAxis)
                        {
                            data[i] = "Rotate Axis";
                        }
                        else
                        {
                            data[i] = temp[i].ToString();
                        }
                    }
                    return data;
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
