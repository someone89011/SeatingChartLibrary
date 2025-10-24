using System;
using System.Windows;
using System.Windows.Data;
using SeatingChartLibrary.ViewModels;

namespace SeatingChartLibrary.Converters
{
    public class ModeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is AppMode mode && parameter is string param)
            {
                return mode.ToString() == param ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}