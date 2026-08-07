using Microsoft.Extensions.DependencyInjection;
using RoomClient.Core.Interfaces;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Wpf.Ui.Controls;

namespace RoomClient.Views.Windows
{
    public partial class MainWindow : FluentWindow
    {
        private bool _isPlayerFullScreen;

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern int ShowWindow(IntPtr hwnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        private DispatcherTimer? _overlayHideTimer;

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Escape && _isPlayerFullScreen)
            {
                e.Handled = true;
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.Player.IsFullScreen = false; // ini akan trigger PropertyChanged -> SetPlayerFullScreen(false) otomatis
                }
                return;
            }

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

        public void SetPlayerFullScreen(bool fullScreen)
        {
            if (fullScreen == _isPlayerFullScreen) return;
            _isPlayerFullScreen = fullScreen;

            if (fullScreen)
            {
                PlayerContainer.Child = null;
                PlayerFullScreenContent.Content = PlayerViewControl;
                PlayerFullScreenHost.Visibility = Visibility.Visible;
                MainContentGrid.Visibility = Visibility.Collapsed;
                ShowNowPlayingOverlay();
            }
            else
            {
                PlayerFullScreenContent.Content = null;
                PlayerContainer.Child = PlayerViewControl;
                PlayerFullScreenHost.Visibility = Visibility.Collapsed;
                MainContentGrid.Visibility = Visibility.Visible;
                _overlayHideTimer?.Stop();
            }
        }

        private void PlayerFullScreenHost_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPlayerFullScreen) return;
            ShowNowPlayingOverlay();
        }

        private void ExitFullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.Player.IsFullScreen = false;
            }
        }

        private void ShowNowPlayingOverlay()
        {
            NowPlayingOverlay.IsHitTestVisible = true;
            NowPlayingOverlay.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(1, TimeSpan.FromMilliseconds(150)));

            _overlayHideTimer?.Stop();
            _overlayHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _overlayHideTimer.Tick += (s, e) =>
            {
                _overlayHideTimer!.Stop();
                NowPlayingOverlay.BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(0, TimeSpan.FromMilliseconds(300)));
                NowPlayingOverlay.IsHitTestVisible = false;
            };
            _overlayHideTimer.Start();
        }

        private void SongListTabButton_Click(object sender, RoutedEventArgs e)
        {
            SongListView.Visibility = Visibility.Visible;
            QueueView.Visibility = Visibility.Collapsed;
            SongListTabButton.IsChecked = true;
            QueueTabButton.IsChecked = false;
            SongListTabButton.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B));
            QueueTabButton.Background = Brushes.Transparent;
        }

        private void QueueTabButton_Click(object sender, RoutedEventArgs e)
        {
            SongListView.Visibility = Visibility.Collapsed;
            QueueView.Visibility = Visibility.Visible;
            QueueTabButton.IsChecked = true;
            SongListTabButton.IsChecked = false;
            QueueTabButton.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B));
            SongListTabButton.Background = Brushes.Transparent;
        }

        private void CloseSidebarButton_Click(object sender, RoutedEventArgs e)
        {
            SidebarContainer.Visibility = Visibility.Collapsed;
            SidebarColumnDefinition.Width = new GridLength(0);
            OpenSidebarButton.Visibility = Visibility.Visible;
        }

        private void OpenSidebarButton_Click(object sender, RoutedEventArgs e)
        {
            SidebarContainer.Visibility = Visibility.Visible;
            SidebarColumnDefinition.Width = new GridLength(300);
            OpenSidebarButton.Visibility = Visibility.Collapsed;
        }

        private bool _isPlaying = true;


        private void BottomBarFullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.Player.IsFullScreen = true;
            }
        }

        private void SongListView_ToggleRequested(object? sender, bool collapsed)
        {
            SongListView.ContentGridControl.Visibility =
                collapsed ? Visibility.Collapsed : Visibility.Visible;
        }

        private void QueueView_ToggleRequested(object? sender, bool collapsed)
        {
            QueueView.ContentGridControl.Visibility =
                collapsed ? Visibility.Collapsed : Visibility.Visible;
        }

        public MainWindow(MainWindowViewModel vm)
        {
            InitializeComponent();

            DataContext = vm;

            SongListView.ToggleRequested += SongListView_ToggleRequested;
            QueueView.ToggleRequested += QueueView_ToggleRequested;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            HideTaskbar();
            WindowState = WindowState.Maximized;

            var configService = App.Services.GetRequiredService<IConfigService>();
            var config = configService.LoadCreate();

            if (DataContext is MainWindowViewModel vm && vm.Player is not null)
            {
                vm.Player.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(PlayerViewModel.IsFullScreen))
                        SetPlayerFullScreen(vm.Player.IsFullScreen);
                };
            }

            if (!config.IsRegistered)
            {
                var registerViewModel = App.Services.GetRequiredService<RegisterViewModel>();
                registerViewModel.RegisterSucceeded += (s, args) =>
                {
                    RegisterOverlay.Visibility = Visibility.Collapsed;
                    MainContentGrid.Visibility = Visibility.Visible;
                    _ = ProceedWithMainFlowAsync();
                };
                RegisterOverlay.DataContext = registerViewModel;
                RegisterOverlay.Visibility = Visibility.Visible;
                MainContentGrid.Visibility = Visibility.Collapsed;
                return;
            }

            await ProceedWithMainFlowAsync();
        }

        private async Task ProceedWithMainFlowAsync()
        {
            if (DataContext is ViewModels.MainWindowViewModel viewModel)
            {
                await viewModel.InitializeAsync();
            }
        }
    }
}
