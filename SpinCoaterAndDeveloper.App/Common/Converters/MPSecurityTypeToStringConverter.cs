using SpinCoaterAndDeveloper.Shared.Models.MovementPointSecurityGraphModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace SpinCoaterAndDeveloper.App.Common.Converters
{
    public class MPSecurityTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return DependencyProperty.UnsetValue;

            var data = value is MovementPointSecurityTypes;
            return GetEnumDescription(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private string GetEnumDescription(object enumObj)
        {
            var fi = enumObj.GetType().GetField(enumObj.ToString());

            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0 && Thread.CurrentThread.CurrentUICulture.Name == "zh-CN")
                return attributes[0].Description;
            else
                return enumObj.ToString();
        }
    }
}
