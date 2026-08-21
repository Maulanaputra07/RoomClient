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

        private void SearchTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!SearchTextBox.IsKeyboardFocused)
            {
                SearchTextBox.Focus();
                e.Handled = true;
            }

            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                KeyboardPopup.IsOpen = true;
            }));
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

            // Jika klik terjadi di dalam SearchTextBox atau isi Popup, jangan tutup
            if (IsElementInside(hitElement, SearchTextBox) ||
                IsElementInside(hitElement, KeyboardPopup.Child))
            {
                return;
            }

            // Klik di luar -> cukup tutup popup. Biarkan focus alami pindah sendiri
            KeyboardPopup.IsOpen = false;
        }

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                KeyboardPopup.IsOpen = true;
            }));
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                if (IsKeyboardFocusWithinPopup())
                {
                    SearchTextBox.Focus();
                    KeyboardPopup.IsOpen = true;
                    return;
                }

                if (!SearchTextBox.IsKeyboardFocused)
                {
                    KeyboardPopup.IsOpen = false;
                }
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
