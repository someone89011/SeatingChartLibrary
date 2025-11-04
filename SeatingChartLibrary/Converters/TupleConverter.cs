using System;
using System.Globalization;
using System.Windows.Data;
using SeatingChartLibrary.ViewModels;

namespace SeatingChartLibrary.Converters
{
    public class TupleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Seat seat && parameter is string deltaStr && double.TryParse(deltaStr, out double delta))
            {
                return (seat, delta);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}