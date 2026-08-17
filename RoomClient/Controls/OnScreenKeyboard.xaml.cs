using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RoomClient.Controls
{
    /// <summary>
    /// Interaction logic for OnScreenKeyboard.xaml
    /// </summary>
    public partial class OnScreenKeyboard : UserControl
    {
        public static readonly DependencyProperty InputTextProperty =
            DependencyProperty.Register(
                nameof(InputText),
                typeof(string),
                typeof(OnScreenKeyboard),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string InputText
        {
            get => (string)GetValue(InputTextProperty);
            set => SetValue(InputTextProperty, value);
        }

        private static readonly string[] Row1Keys = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" };
        private static readonly string[] Row2Keys = { "K", "L", "M", "N", "O", "P", "Q", "R", "S" };
        private static readonly string[] Row3Keys = { "T", "U", "V", "W", "X", "Y", "Z" };

        public OnScreenKeyboard()
        {
            InitializeComponent();
            BuildRow(Row1, Row1Keys);
            BuildRow(Row2, Row2Keys);
            BuildRow(Row3, Row3Keys);
        }

        private void BuildRow(StackPanel target, string[] keys)
        {
            foreach (var key in keys)
            {
                var btn = new Button
                {
                    Content = key,
                    Style = (Style)FindResource("KeyButtonStyle"),
                    Tag = key
                };
                btn.Click += (s, e) => InputText += ((Button)s).Tag.ToString();
                target.Children.Add(btn);
            }
        }

        private void Space_Click(object sender, RoutedEventArgs e) => InputText += " ";

        private void Backspace_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(InputText))
                InputText = InputText.Substring(0, InputText.Length - 1);
        }

        private void Clear_Click(object sender, RoutedEventArgs e) => InputText = string.Empty;
    }
}
