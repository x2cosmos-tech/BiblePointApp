using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace BiblePointApp.Converters
{
    public class BoolToStrokeConverter : IValueConverter
    {
        // true -> accent stroke, false -> light gray
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isCurrent = value is bool b && b;
            var color = isCurrent ? Color.FromArgb("#512BD4") : Color.FromArgb("#E0E0E0");
            return new SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}