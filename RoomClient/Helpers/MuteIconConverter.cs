using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Wpf.Ui.Controls;

namespace RoomClient.Helpers
{
    public sealed class MuteIconConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            var volume = value is double v ? v : 100;

            var symbol = volume switch
            {
                <= 0 => SymbolRegular.SpeakerMute24,
                < 50 => SymbolRegular.Speaker124,
                _ => SymbolRegular.Speaker224,
            };

            return new SymbolIcon
            {
                Symbol = symbol
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
