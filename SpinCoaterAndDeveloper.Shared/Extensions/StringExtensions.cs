using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SpinCoaterAndDeveloper.Shared.Extensions
{
    public static class StringExtensions
    {
        public static string TryFindResourceEx(this string str)
        {
            return Application.Current.TryFindResource(str)?.ToString() ?? str;
        }
    }
}
