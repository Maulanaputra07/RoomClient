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
    public class SongHighlightConverter : IMultiValueConverter
    {
        private static readonly SolidColorBrush HighlightBrush = new(Color.FromRgb(0x1E, 0x40, 0xAF));
        private static readonly SolidColorBrush DefaultBrush = new(Color.FromRgb(0x11, 0x18, 0x27));

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
