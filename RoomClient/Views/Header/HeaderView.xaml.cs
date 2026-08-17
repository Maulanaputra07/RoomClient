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
using System.Windows.Threading;

namespace RoomClient.Views.Header
{
    /// <summary>
    /// Interaction logic for HeaderView.xaml
    /// </summary>
    public partial class HeaderView : UserControl
    {
        private bool _isKeyboardInteraction;

        public HeaderView()
        {
            InitializeComponent();
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            KeyboardPopup.IsOpen = true;
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Tunda penutupan agar klik di dalam Popup keyboard tidak langsung menutupnya.
            // Saat user klik tombol di OnScreenKeyboard, focus pindah dari TextBox ke Button di Popup,
            // sehingga LostFocus terpicu. Dengan delay singkat, kita cek apakah focus masih
            // di dalam area Popup — jika ya, kembalikan focus ke TextBox dan biarkan popup tetap terbuka.
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                if (IsKeyboardFocusWithinPopup())
                {
                    // Focus masih di dalam popup keyboard — kembalikan focus ke TextBox
                    SearchTextBox.Focus();
                    return;
                }

                // Focus benar-benar pindah ke luar area search+keyboard — tutup popup
                KeyboardPopup.IsOpen = false;
            }));
        }

        /// <summary>
        /// Cek apakah focus keyboard saat ini berada di dalam elemen Popup keyboard.
        /// </summary>
        private bool IsKeyboardFocusWithinPopup()
        {
            var focused = Keyboard.FocusedElement as DependencyObject;
            if (focused is null) return false;

            // Telusuri visual tree ke atas untuk mencari apakah elemen yang di-focus
            // merupakan child dari KeyboardPopup
            var parent = focused;
            while (parent is not null)
            {
                if (parent == KeyboardPopup.Child)
                    return true;
                parent = VisualTreeHelper.GetParent(parent);
            }

            return false;
        }

        private void ToggleKeyboard_Click(object sender, RoutedEventArgs e)
        {
            KeyboardPopup.IsOpen = !KeyboardPopup.IsOpen;
            if (KeyboardPopup.IsOpen)
            {
                SearchTextBox.Focus();
            }
        }

        private void CariButton_Click(object sender, RoutedEventArgs e)
        {
            KeyboardPopup.IsOpen = false;
        }
    }
}

