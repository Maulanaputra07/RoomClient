using Microsoft.Web.WebView2.Core;
using RoomClient.Core.Interfaces;
using RoomClient.Helpers;
using RoomClient.ViewModels;
using System.ComponentModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace RoomClient.Views.Player
{
    public partial class PlayerView : UserControl
    {
        private static CoreWebView2Environment? _sharedEnvironment; // <-- tambahkan ini
        private static readonly SemaphoreSlim _envLock = new(1, 1);
        private PlayerViewModel? _playerViewModel;
        private WebViewPlayer? _player;
        private bool _webViewInitialized;
        private bool _webMessageSubscribed;
        private string? _lastLoadedHtml;

        public PlayerView()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            DataContextChanged += OnDataContextChanged;
        }


        private void SubscribeWebMessages()
        {
            if (_webMessageSubscribed || PlayerWebView.CoreWebView2 is null) return;

            PlayerWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            _webMessageSubscribed = true;
            LogWebViewIssue("WebMessageReceived subscribed.");
        }

        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var json = e.TryGetWebMessageAsString();
                LogWebViewIssue($"WebMessage diterima: {json}");

                using var doc = JsonDocument.Parse(json);
                var type = doc.RootElement.GetProperty("type").GetString();

                switch (type)
                {
                    case "play":
                        _playerViewModel?.NotifyWebViewPlaybackState(PlaybackState.Playing);
                        break;
                    case "pause":
                        _playerViewModel?.NotifyWebViewPlaybackState(PlaybackState.Paused);
                        break;
                    case "ended":
                        LogWebViewIssue("Event ended diterima, memanggil NotifySongEnded.");
                        _playerViewModel?.NotifySongEnded();
                        break;
                    case "exitFullscreen":
                        if (_playerViewModel is not null)
                        {
                            _playerViewModel.IsFullScreen = false;
                        }
                        break;
                    case "error":
                        LogWebViewIssue("Video playback error dari WebView.");
                        break;
                }
            }
            catch (Exception ex)
            {
                LogWebViewIssue($"Gagal parse WebMessage: {ex}");
            }
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            await LoadCurrentPlayerHtmlAsync();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Pastikan pemutaran dihentikan dulu sebelum dispose agar audio lama tidak menggantung
            //_player?.Dispose();
            //_player = null;
            //_webViewInitialized = false;
            if (_webMessageSubscribed && PlayerWebView.CoreWebView2 is not null)
            {
                PlayerWebView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
                _webMessageSubscribed = false;
            }
            //_player?.Clear();
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
            _playerViewModel.JavaScriptCommandRequested += OnJavaScriptCommandRequested;
        }

        private void DetachPlayerViewModel()
        {
            if (_playerViewModel is null)
            {
                return;
            }

            _playerViewModel.PropertyChanged -= OnPlayerPropertyChanged;
            _playerViewModel.JavaScriptCommandRequested -= OnJavaScriptCommandRequested;
            _playerViewModel = null;
        }

        private async void OnJavaScriptCommandRequested(object? sender, string js)
        {
            if (!_webViewInitialized || PlayerWebView.CoreWebView2 is null)
            {
                LogWebViewIssue($"Skip JS command, WebView belum siap: {js}");
                return;
            }

            try
            {
                await PlayerWebView.CoreWebView2.ExecuteScriptAsync(js);
            }
            catch (Exception ex)
            {
                LogWebViewIssue($"ExecuteScriptAsync gagal untuk '{js}': {ex}");
            }
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
            // PERBAIKAN: Tunggu WebView2 siap terlebih dahulu, jangan di-return/diabaikan!
            await EnsureWebViewAsync();

            if (!_webViewInitialized)
            {
                return;
            }

            var html = _playerViewModel?.PlayerHtml;

            if (string.IsNullOrEmpty(html))
            {
                _player?.Clear();
                _lastLoadedHtml = null;
                return;
            }

            if (html == _lastLoadedHtml)
            {
                // HTML sama seperti yang sudah dimuat (misal Loaded terpicu ulang karena
                // reparenting saat toggle fullscreen) — skip supaya video tidak restart.
                return;
            }

            _player?.LoadHtml(html);
            _lastLoadedHtml = html;

            if (_playerViewModel is not null && PlayerWebView.CoreWebView2 is not null)
            {
                try
                {
                    var applyVolumeJs = _playerViewModel.GetApplyVolumeScript();
                    await PlayerWebView.CoreWebView2.ExecuteScriptAsync(applyVolumeJs);
                }
                catch (Exception ex)
                {
                    LogWebViewIssue($"Gagal reapply volume: {ex}");
                }
            }
        }

        private async Task EnsureWebViewAsync()
        {
            LogWebViewIssue($"EnsureWebViewAsync called. _webViewInitialized={_webViewInitialized}");

            if (_webViewInitialized)
            {
                SubscribeWebMessages();
                return;
            }

            if (PlayerWebView.CoreWebView2 is not null)
            {
                _player ??= new WebViewPlayer(PlayerWebView);
                #if DEBUG
                PlayerWebView.CoreWebView2.OpenDevToolsWindow();
                #endif
                SubscribeWebMessages();
                _webViewInitialized = true;
                LogWebViewIssue("CoreWebView2 already existed on control, reused it.");
                return;
            }

            try
            {
                await _envLock.WaitAsync();
                try
                {
                    if (_sharedEnvironment is null)
                    {
                        var userDataFolder = System.IO.Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "RoomClient", "WebView2");

                        var options = new CoreWebView2EnvironmentOptions(
                            "--autoplay-policy=no-user-gesture-required");

                        _sharedEnvironment = await CoreWebView2Environment.CreateAsync(
                            browserExecutableFolder: null,
                            userDataFolder: userDataFolder,
                            options: options);
                    }
                }
                finally
                {
                    _envLock.Release();
                }

                await PlayerWebView.EnsureCoreWebView2Async(_sharedEnvironment);

                if (PlayerWebView.CoreWebView2 is null)
                {
                    LogWebViewIssue("CoreWebView2 is null setelah EnsureCoreWebView2Async");
                    return;
                }

                LogWebViewIssue($"WebView2 initialized OK. Runtime version: {_sharedEnvironment.BrowserVersionString}");

                _player ??= new WebViewPlayer(PlayerWebView);
                SubscribeWebMessages();
                _webViewInitialized = true;
            }
            catch (Exception ex)
            {
                _webViewInitialized = false;
                LogWebViewIssue($"Exception: {ex}");
            }
        }

        private static void LogWebViewIssue(string message)
        {
            System.Diagnostics.Debug.WriteLine($"WebView2 issue: {message}");
            try
            {
                var logPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RoomClient", "webview2-error.log");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath)!);
                System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] {message}\n\n");
            }
            catch
            {
                // Ignore logging failures
            }
        }
    }
}