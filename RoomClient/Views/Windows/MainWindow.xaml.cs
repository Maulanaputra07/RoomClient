using RoomClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Wpf.Ui.Controls;

namespace RoomClient.Views.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern int ShowWindow(IntPtr hwnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        private int _exitSequenceStep = 0;

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Blokir Alt+F4
            if (e.SystemKey == Key.F4 && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                e.Handled = true;
                return;
            }

            // Blokir Alt+Tab (opsional, tidak selalu bisa di-intercept penuh karena Windows menangkapnya duluan)
            if (e.SystemKey == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                e.Handled = true;
                return;
            }

            // Kombinasi tersembunyi untuk keluar: Ctrl+Alt+Shift+Q
            if (e.Key == Key.Q &&
                (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
                (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt &&
                (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                e.Handled = true;
                ExitApplication();
            }
        }

        private void ExitApplication()
        {
            ShowTaskbar();
            Application.Current.Shutdown();
        }

        private void HideTaskbar()
        {
            var taskbarHandle = FindWindow("Shell_TrayWnd", null);
            if (taskbarHandle != IntPtr.Zero)
            {
                ShowWindow(taskbarHandle, SW_HIDE);
            }
        }

        private void ShowTaskbar()
        {
            var taskbarHandle = FindWindow("Shell_TrayWnd", null);
            if (taskbarHandle != IntPtr.Zero)
            {
                ShowWindow(taskbarHandle, SW_SHOW);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            ShowTaskbar();
            base.OnClosed(e);
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(MainWindowViewModel vm)
        {
            InitializeComponent();

            DataContext = vm;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            HideTaskbar();
            WindowState = WindowState.Maximized;

            if (DataContext is MainWindowViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }
    }
}
