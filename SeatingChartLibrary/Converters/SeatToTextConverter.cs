using SeatingChartLibrary.ViewModels;
using System.Globalization;
using System.Windows.Data;

namespace SeatingChartLibrary.Converters
{
    public class SeatToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Seat seat)
            {
                string row = seat.RowName ?? "";
                string num = seat.Number ?? "未設定";
                string person = seat.Person?.Name ?? "空位";
                string device = seat.Person?.Device != null ? $"{seat.Person.Device.TypeUuid}/{seat.Person.Device.DeviceUuid}" : "無";
                return $"{row}{num}\n{person}\n{device}";
            }
            return "未設定";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}