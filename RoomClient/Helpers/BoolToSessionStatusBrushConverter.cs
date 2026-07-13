using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace RoomClient.Helpers
{
    internal class BoolToSessionStatusBrushConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is true
                ? new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E))  // hijau - aktif
                : new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)); // abu-abu - menunggu
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
