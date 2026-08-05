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
    public class SearchResultsVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Pastikan XAML mengirimkan tepat 2 binding (Count dan IsBusy)
            if (values == null || values.Length < 2 || values.Any(v => v == DependencyProperty.UnsetValue))
                return Visibility.Collapsed;

            var count = values[0] is int c ? c : 0;
            var isBusy = values[1] is bool b && b;

            // List HANYA tampil jika ADA HASIL (> 0) dan TIDAK SEDANG SIBUK mencari
            if (count > 0 && !isBusy)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
