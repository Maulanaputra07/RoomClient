using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace RoomClient.Helpers
{
    public class PlayButtonContentConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3 || values[0] is not string itemVideoId || values[1] is not string currentVideoId)
                return "Play";

            bool isCurrent = !string.IsNullOrEmpty(itemVideoId) && itemVideoId == currentVideoId;
            bool isPlaying = values[2] is true;

            return isCurrent
                ? (isPlaying ? "⏸ Pause" : "▶ Lanjut")
                : "Play";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
