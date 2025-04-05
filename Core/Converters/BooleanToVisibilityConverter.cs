using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RealScraper.Core.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var visible = (bool)value;

            // Negate visibility if a parameter is provided
            visible = (parameter != null) ? !visible : visible;

            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}