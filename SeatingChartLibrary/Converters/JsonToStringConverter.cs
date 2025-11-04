using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SeatingChartLibrary.Converters
{
    public class JsonToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string json && !string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var sb = new StringBuilder();
                    foreach (var prop in doc.RootElement.EnumerateObject())
                        sb.Append($"{prop.Name}: {prop.Value} ");
                    return sb.ToString().Trim();
                }
                catch { }
            }
            return "-";
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
