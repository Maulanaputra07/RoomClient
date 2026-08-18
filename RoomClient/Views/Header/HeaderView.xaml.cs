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
            Loaded += HeaderView_Loaded;
            Unloaded += HeaderView_Unloaded;
        }

        private void HeaderView_Loaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.PreviewMouseDown += Window_PreviewMouseDown;
            }
        }

        private void HeaderView_Unloaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.PreviewMouseDown -= Window_PreviewMouseDown;
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                KeyboardPopup.IsOpen = false;
                Keyboard.ClearFocus(); // Hilangkan fokus dari SearchTextBox
            }
        }

        private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!KeyboardPopup.IsOpen) return;

            var hitElement = e.OriginalSource as DependencyObject;

            // Jika klik terjadi di dalam SearchTextBox, isi Popup, atau tombol Toggle Keyboard, jangan tutup
            if (IsElementInside(hitElement, SearchTextBox) ||
                IsElementInside(hitElement, KeyboardPopup.Child))
            {
                return;
            }

            // Klik berada di luar area yang diizinkan -> tutup popup dan lepaskan fokus
            KeyboardPopup.IsOpen = false;
            Keyboard.ClearFocus();
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

            return IsElementInside(focused, KeyboardPopup.Child);
        }

        private bool IsElementInside(DependencyObject element, DependencyObject container)
        {
            if (element == null || container == null) return false;

            var parent = element;
            while (parent != null)
            {
                if (parent == container) return true;
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
            Keyboard.ClearFocus();
        }
    }
}

