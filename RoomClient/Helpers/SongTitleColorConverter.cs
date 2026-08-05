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
    public class SongTitleColorConverter : IMultiValueConverter
    {
        private static readonly SolidColorBrush HighlightBrush = new(Color.FromRgb(0x38, 0xBD, 0xF8)); // biru terang
        private static readonly SolidColorBrush DefaultBrush = Brushes.White;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] is not string itemVideoId || values[1] is not string currentVideoId)
                return DefaultBrush;

            return !string.IsNullOrEmpty(itemVideoId) && itemVideoId == currentVideoId
                ? HighlightBrush
                : DefaultBrush;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
