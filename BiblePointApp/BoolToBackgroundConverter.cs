using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace BiblePointApp.Converters
{
    public class BoolToBackgroundConverter : IValueConverter
    {
        // true -> highlight background, false -> white
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isCurrent = value is bool b && b;
            return isCurrent ? Color.FromArgb("#F3E5F5") : Colors.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}