using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace RoomClient.Helpers
{
    public class InitialSearchStateConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2 || values.Any(v => v == DependencyProperty.UnsetValue))
                return Visibility.Collapsed;

            var query = values[0] as string;
            var isBusy = values[1] is bool b && b;

            // Initial state HANYA tampil jika:
            // 1. User belum mengetik apapun (query kosong)
            // 2. Tidak sedang loading
            if (string.IsNullOrWhiteSpace(query) && !isBusy)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
