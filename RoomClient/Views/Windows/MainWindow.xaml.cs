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

#if DEBUG
            // DEBUG ONLY: Ctrl+Alt+Shift+T untuk simulasi session aktif + websocket ready
            if (e.Key == Key.T &&
                (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
                (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt &&
                (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                e.Handled = true;
                SimulateReadyState();
            }

            // DEBUG ONLY: Ctrl+Alt+Shift+D untuk test putar file .DAT lokal via VLC
            if (e.Key == Key.D &&
                (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
                (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt &&
                (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                e.Handled = true;
                _ = SimulateDatPlaybackAsync();
            }

            // DEBUG ONLY: Ctrl+Alt+Shift+V untuk test putar valid online video stream (Big Buck Bunny) via VLC
            if (e.Key == Key.V &&
                (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
                (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt &&
                (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                e.Handled = true;
                _ = SimulateOnlineStreamPlaybackAsync();
            }
#endif
        }

#if DEBUG
        // ─── UBAH PATH INI sesuai lokasi file .DAT Anda ───────────────────────
        private const string _debugDatFilePath = @"D:\Downloads\DIAN FK - CIDRO.DAT";
        private const string _debugOnlineStreamUrl = "https://www.w3schools.com/html/mov_bbb.mp4";
        // ────────────────────────────────────────────────────────────────────────

        private void SimulateReadyState()
        {
            if (DataContext is not MainWindowViewModel vm) return;

            vm.Status.IsWebSocketConnecting = false;
            vm.Status.IsWebSocketReady = true;
            vm.Player.ActivateSession(DateTimeOffset.UtcNow.AddMinutes(02));

            vm.Status.AddLog("[DEBUG] Simulated: WebSocket ready + session active (2 min)");
        }

        private async Task SimulateOnlineStreamPlaybackAsync()
        {
            if (DataContext is not MainWindowViewModel vm) return;

            if (!vm.Player.IsSessionActive)
            {
                vm.Status.IsWebSocketConnecting = true;
                vm.Status.IsWebSocketReady = true;
                vm.Player.ActivateSession(DateTimeOffset.UtcNow.AddMinutes(30));
                vm.Status.AddLog("[DEBUG-VLC] Session diaktifkan otomatis (30 menit)");
            }

            // Tunggu UI render pass selesai agar HWND kontrol VLC terbentuk sebelum PlayAsync
            await Dispatcher.Yield(DispatcherPriority.Render);

            var testSong = new RoomClient.Core.Models.Song
            {
                Source = RoomClient.Core.Models.SongSource.Database,
                Title = "Big Buck Bunny (Sample Online Stream)",
                Artist = "Blender Foundation",
                VideoId = $"debug-online-{Guid.NewGuid():N}",
                DirectStreamUrl = _debugOnlineStreamUrl,
            };

            vm.Status.AddLog($"[DEBUG-VLC] Memutar Online Sample: {_debugOnlineStreamUrl}");
            await vm.Player.PlayAsync(testSong);
        }

        private async Task SimulateDatPlaybackAsync()
        {
            if (DataContext is not MainWindowViewModel vm) return;

            // 1. Pastikan session aktif (jika belum)
            if (!vm.Player.IsSessionActive)
            {
                vm.Status.IsWebSocketConnecting = false;
                vm.Status.IsWebSocketReady = true;
                vm.Player.ActivateSession(DateTimeOffset.UtcNow.AddMinutes(01));
                vm.Status.AddLog("[DEBUG-VLC] Session diaktifkan otomatis (30 menit)");
            }

            // Tunggu UI render pass selesai agar HWND kontrol VLC terbentuk sebelum PlayAsync
            await Dispatcher.Yield(DispatcherPriority.Render);

            // 2. Validasi file ada
            if (!System.IO.File.Exists(_debugDatFilePath))
            {
                System.Windows.MessageBox.Show(
                    $"File .DAT tidak ditemukan:\n{_debugDatFilePath}\n\n" +
                    "Ubah konstanta _debugDatFilePath di MainWindow.xaml.cs",
                    "[DEBUG] VLC Test — File Not Found");
                return;
            }

            // 3. Buat Song object dengan Source = Database
            var testSong = new RoomClient.Core.Models.Song
            {
                Source = RoomClient.Core.Models.SongSource.Database,
                Title = System.IO.Path.GetFileName(_debugDatFilePath),
                VideoId = $"debug-dat-{Guid.NewGuid():N}",   // ID unik agar history tracking tidak bentrok
                DirectStreamUrl = _debugDatFilePath,
            };

            vm.Status.AddLog($"[DEBUG-VLC] Memutar: {_debugDatFilePath}");

            // 4. Panggil PlayAsync — ini akan trigger VlcSourceUrl → VlcVideoView tampil
            await vm.Player.PlayAsync(testSong);
        }
#endif

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

        private const double FullscreenExitButtonRightMargin = 28;
        private const double FullscreenExitButtonBottomMargin = 28;
        private const double FullscreenOneMinuteWarningRightMargin = 28;
        private const double FullscreenOneMinuteWarningTopMargin = 24;

        private bool _isSidebarOpen = true;

        private void UpdateFullscreenPopupBounds()
        {
            var width = ActualWidth > 0 ? ActualWidth : PlayerContainer.ActualWidth;
            var height = ActualHeight > 0 ? ActualHeight : PlayerContainer.ActualHeight;

            if (width > 0 && height > 0)
            {
                FullscreenOverlayRootGrid.Width = width;
                FullscreenOverlayRootGrid.Height = height;
            }
        }


        private void UpdateFullscreenExitButtonPopupPosition()
        {
            if (ExitFullScreenButton is null || FullscreenExitButtonPopup is null)
            {
                return;
            }

            ExitFullScreenButton.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            var buttonWidth = ExitFullScreenButton.ActualWidth > 0
                ? ExitFullScreenButton.ActualWidth
                : ExitFullScreenButton.DesiredSize.Width;
            var buttonHeight = ExitFullScreenButton.ActualHeight > 0
                ? ExitFullScreenButton.ActualHeight
                : ExitFullScreenButton.DesiredSize.Height;

            var hostWidth = ActualWidth > 0 ? ActualWidth : Width;
            var hostHeight = ActualHeight > 0 ? ActualHeight : Height;

            if (hostWidth <= 0 || hostHeight <= 0 || buttonWidth <= 0 || buttonHeight <= 0)
            {
                return;
            }

            FullscreenExitButtonPopup.HorizontalOffset = Math.Max(0, hostWidth - buttonWidth - FullscreenExitButtonRightMargin);
            FullscreenExitButtonPopup.VerticalOffset = Math.Max(0, hostHeight - buttonHeight - FullscreenExitButtonBottomMargin);
        }

        private void UpdateFullscreenOneMinuteWarningPopupPosition()
        {
            if (FullscreenOneMinuteWarningContent is null || FullscreenOneMinuteWarningPopup is null)
            {
                return;
            }

            FullscreenOneMinuteWarningContent.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            var warningWidth = FullscreenOneMinuteWarningContent.ActualWidth > 0
                ? FullscreenOneMinuteWarningContent.ActualWidth
                : FullscreenOneMinuteWarningContent.DesiredSize.Width;

            var hostWidth = ActualWidth > 0 ? ActualWidth : Width;

            if (hostWidth <= 0 || warningWidth <= 0)
            {
                return;
            }

            FullscreenOneMinuteWarningPopup.HorizontalOffset = Math.Max(0, hostWidth - warningWidth - FullscreenOneMinuteWarningRightMargin);
            FullscreenOneMinuteWarningPopup.VerticalOffset = FullscreenOneMinuteWarningTopMargin;
        }

        private void UpdateFullscreenOneMinuteWarningVisibility()
        {
            if (DataContext is not MainWindowViewModel vm)
            {
                FullscreenOneMinuteWarningPopup.IsOpen = false;
                return;
            }

            var shouldShow = _isPlayerFullScreen && vm.Player.ShowOneMinuteWarning;
            FullscreenOneMinuteWarningPopup.IsOpen = shouldShow;

            if (shouldShow)
            {
                UpdateLayout();
                UpdateFullscreenOneMinuteWarningPopupPosition();
                FullscreenOneMinuteWarningHost.Opacity = 1;
            }
        }

        public void SetPlayerFullScreen(bool fullScreen)
        {
            if (fullScreen == _isPlayerFullScreen) return;
            _isPlayerFullScreen = fullScreen;

            if (fullScreen)
            {
                _isSidebarOpen = SidebarContainer.Visibility == Visibility.Visible;

                HeaderRowBorder.Visibility = Visibility.Collapsed;
                CategoryRowBorder.Visibility = Visibility.Collapsed;
                ContentRowGrid.Margin = new Thickness(0);

                SidebarContainer.Visibility = Visibility.Collapsed;
                SidebarColumnDefinition.Width = new GridLength(0);
                OpenSidebarButton.Visibility = Visibility.Collapsed;

                PlayerWrapperGrid.Margin = new Thickness(0);
                PlayerContainer.CornerRadius = new CornerRadius(0);

                FullscreenOverlayPopup.IsOpen = false;
                FullscreenExitButtonPopup.IsOpen = true;
                UpdateLayout();
                UpdateFullscreenExitButtonPopupPosition();
                UpdateFullscreenOneMinuteWarningVisibility();
                ShowFullscreenOverlay();
            }
            else
            {
                FullscreenOverlayPopup.IsOpen = false;
                _overlayHideTimer?.Stop();

                HeaderRowBorder.Visibility = Visibility.Visible;
                CategoryRowBorder.Visibility = Visibility.Visible;
                ContentRowGrid.Margin = new Thickness(12, 0, 12, 12);

                if (_isSidebarOpen)
                {
                    SidebarContainer.Visibility = Visibility.Visible;
                    SidebarColumnDefinition.Width = new GridLength(300);
                    OpenSidebarButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    SidebarContainer.Visibility = Visibility.Collapsed;
                    SidebarColumnDefinition.Width = new GridLength(0);
                    OpenSidebarButton.Visibility = Visibility.Visible;
                }

                PlayerWrapperGrid.ClearValue(FrameworkElement.MarginProperty);
                PlayerContainer.CornerRadius = new CornerRadius(16);

                FullscreenExitButtonPopup.IsOpen = false;
                FullscreenOneMinuteWarningPopup.IsOpen = false;
                BottomPlayerBar.Visibility = Visibility.Visible;
            }
        }

        private void FullscreenOverlay_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPlayerFullScreen) return;
            ShowFullscreenOverlay();
        }

        private void FullscreenOverlay_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isPlayerFullScreen) return;
            ShowFullscreenOverlay();
        }

        private void FullscreenOverlay_TouchDown(object? sender, TouchEventArgs e)
        {
            if (!_isPlayerFullScreen) return;
            ShowFullscreenOverlay();
        }

        private void ShowFullscreenOverlay()
        {
            if (!_isPlayerFullScreen) return;

            UpdateFullscreenPopupBounds();
            FullscreenOverlayPopup.IsOpen = true;

            FullscreenOverlayRootGrid.IsHitTestVisible = true;
            FullscreenOverlayRootGrid.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(1, TimeSpan.FromMilliseconds(200)));
            FullscreenExitButtonHost.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(1, TimeSpan.FromMilliseconds(200)));
            if (FullscreenOneMinuteWarningPopup.IsOpen)
            {
                FullscreenOneMinuteWarningHost.Opacity = 1;
            }

            _overlayHideTimer?.Stop();
            _overlayHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _overlayHideTimer.Tick += (s, e) =>
            {
                _overlayHideTimer?.Stop();
                if (_isPlayerFullScreen)
                {
                    // Fade overlay atas dan tombol exit agar tidak terlalu mengganggu video/lirik.
                    FullscreenOverlayRootGrid.BeginAnimation(UIElement.OpacityProperty,
                        new DoubleAnimation(0.35, TimeSpan.FromMilliseconds(500)));
                    FullscreenExitButtonHost.BeginAnimation(UIElement.OpacityProperty,
                        new DoubleAnimation(0.35, TimeSpan.FromMilliseconds(500)));
                    if (FullscreenOneMinuteWarningPopup.IsOpen)
                    {
                        FullscreenOneMinuteWarningHost.Opacity = 1;
                    }
                }
            };
            _overlayHideTimer.Start();
        }

        private void ExitFullScreenButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!_isPlayerFullScreen) return;

            _overlayHideTimer?.Stop();
            FullscreenExitButtonHost.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(1, TimeSpan.FromMilliseconds(120)));
        }

        private void ExitFullScreenButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isPlayerFullScreen) return;
            ShowFullscreenOverlay();
        }

        private void ExitFullScreenButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.Player.IsFullScreen = false;
            }
        }

        private void SongListTabButton_Click(object sender, RoutedEventArgs e)
        {
            SongListView.Visibility = Visibility.Visible;
            QueueView.Visibility = Visibility.Collapsed;
            SongListTabButton.IsChecked = true;
            QueueTabButton.IsChecked = false;
            SongListTabButton.Background = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB));
            SongListTabButton.Foreground = Brushes.White;
            QueueTabButton.Background = Brushes.Transparent;
        }

        private void QueueTabButton_Click(object sender, RoutedEventArgs e)
        {
            SongListView.Visibility = Visibility.Collapsed;
            QueueView.Visibility = Visibility.Visible;
            QueueTabButton.IsChecked = true;
            SongListTabButton.IsChecked = false;
            QueueTabButton.Background = new SolidColorBrush(Color.FromRgb(0x25, 0x63, 0xEB));
            QueueTabButton.Foreground = Brushes.White;
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
                vm.Player.IsFullScreen = !vm.Player.IsFullScreen;
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
            PlayerContainer.SizeChanged += (s, e) =>
            {
                if (_isPlayerFullScreen)
                {
                    UpdateFullscreenPopupBounds();
                }
            };
            SizeChanged += (s, e) =>
            {
                if (_isPlayerFullScreen)
                {
                    UpdateFullscreenPopupBounds();
                    UpdateFullscreenExitButtonPopupPosition();
                    UpdateFullscreenOneMinuteWarningPopupPosition();
                }
            };

            ExitFullScreenButton.SizeChanged += (s, e) =>
            {
                if (_isPlayerFullScreen)
                {
                    UpdateFullscreenExitButtonPopupPosition();
                }
            };

            FullscreenOneMinuteWarningContent.SizeChanged += (s, e) =>
            {
                if (_isPlayerFullScreen)
                {
                    UpdateFullscreenOneMinuteWarningPopupPosition();
                }
            };
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
                    {
                        SetPlayerFullScreen(vm.Player.IsFullScreen);
                    }
                    else if (args.PropertyName == nameof(PlayerViewModel.ShowOneMinuteWarning))
                    {
                        UpdateFullscreenOneMinuteWarningVisibility();
                    }
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
