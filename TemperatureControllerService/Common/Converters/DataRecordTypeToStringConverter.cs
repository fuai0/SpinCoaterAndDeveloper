using TemperatureControllerService.Common.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace TemperatureControllerService.Converters
{
    public class DataRecordTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DataRecordType type)
            {
                switch (type)
                {
                    case DataRecordType.Read:
                        return "读取";
                    case DataRecordType.Write:
                        return "设置";
                    case DataRecordType.Status:
                        return "状态";
                    case DataRecordType.Error:
                        return "错误";
                    default:
                        return "";
                }
            }
            return "";
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
