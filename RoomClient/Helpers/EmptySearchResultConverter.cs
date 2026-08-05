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
    public class EmptySearchResultConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3 || values.Any(v => v == DependencyProperty.UnsetValue))
                return Visibility.Collapsed;

            var query = values[0] as string;
            var count = values[1] is int c ? c : 0;
            var isBusy = values[2] is bool b && b;

            // Empty state HANYA tampil jika:
            // 1. User sudah mengetik sesuatu (query tidak kosong)
            // 2. Tidak sedang loading (!isBusy)
            // 3. Hasilnya benar-benar kosong (count == 0)
            if (!string.IsNullOrWhiteSpace(query) && !isBusy && count == 0)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
