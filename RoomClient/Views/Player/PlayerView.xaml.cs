using Microsoft.Web.WebView2.Core;
using RoomClient.Helpers;
using RoomClient.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace RoomClient.Views.Player
{
    public partial class PlayerView : UserControl
    {
        private PlayerViewModel? _playerViewModel;
        private WebViewPlayer? _player;
        private bool _webViewInitialized;

        public PlayerView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            DataContextChanged += OnDataContextChanged;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            await EnsureWebViewAsync();
            await LoadCurrentPlayerHtmlAsync();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _player?.Dispose();
            _player = null;
            _webViewInitialized = false;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DetachPlayerViewModel();

            if (DataContext is MainWindowViewModel mainWindowViewModel)
            {
                AttachPlayerViewModel(mainWindowViewModel.Player);
                _ = LoadCurrentPlayerHtmlAsync();
            }
        }

        private void AttachPlayerViewModel(PlayerViewModel playerViewModel)
        {
            _playerViewModel = playerViewModel;
            _playerViewModel.PropertyChanged += OnPlayerPropertyChanged;
        }

        private void DetachPlayerViewModel()
        {
            if (_playerViewModel is null)
            {
                return;
            }

            _playerViewModel.PropertyChanged -= OnPlayerPropertyChanged;
            _playerViewModel = null;
        }

        private async void OnPlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlayerViewModel.PlayerHtml))
            {
                await LoadCurrentPlayerHtmlAsync();
            }
        }

        private async Task LoadCurrentPlayerHtmlAsync()
        {
            if (!_webViewInitialized)
            {
                return;
            }

            if (_playerViewModel?.PlayerHtml is { Length: > 0 } html)
            {
                _player?.LoadHtml(html);
            }
            else
            {
                _player?.Clear();
            }

                await Task.CompletedTask;
        }

        private async Task EnsureWebViewAsync()
        {
            if (_webViewInitialized)
            {
                return;
            }
            try
            {
                // Folder yang PASTI writable di semua kondisi (admin ataupun standard user),
                // terlepas dari lokasi instalasi app (mis. Program Files).
                var userDataFolder = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RoomClient", "WebView2");

                var options = new CoreWebView2EnvironmentOptions(
                    "--autoplay-policy=no-user-gesture-required");

                var environment = await CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: userDataFolder,
                    options: options);

                await PlayerWebView.EnsureCoreWebView2Async(environment);

                if (PlayerWebView.CoreWebView2 is null)
                {
                    return;
                }

                _player ??= new WebViewPlayer(PlayerWebView);
                _webViewInitialized = true;
            }
            catch (Exception ex)
            {
                _webViewInitialized = false;

                // JANGAN biarkan catch block kosong — minimal log ke file/debug output
                // supaya kalau gagal lagi di PC lain, kamu tahu penyebabnya (missing runtime,
                // access denied, dsb) alih-alih cuma lihat blank window tanpa petunjuk.
                System.Diagnostics.Debug.WriteLine($"WebView2 init failed: {ex}");

                // Opsional: tulis ke file log supaya bisa dicek user non-developer juga
                try
                {
                    var logPath = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "RoomClient", "webview2-error.log");
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath)!);
                    System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] {ex}\n\n");
                }
                catch
                {
                    // Kalau logging pun gagal, biarkan saja — jangan sampai crash di sini
                }
            }
        }
    }
}
