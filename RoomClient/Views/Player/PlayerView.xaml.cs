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
            // Log TANPA SYARAT di paling awal — kalau baris ini saja tidak muncul di log,
            // berarti method ini memang tidak pernah dipanggil sama sekali dari caller-nya.
            LogWebViewIssue($"EnsureWebViewAsync called. _webViewInitialized={_webViewInitialized}");

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
                    // Jalur ini SEBELUMNYA tidak di-log sama sekali — inilah kenapa
                    // webview2-error.log tidak muncul di beberapa laptop meskipun
                    // WebView2 gagal init. EnsureCoreWebView2Async bisa "berhasil"
                    // (tidak throw) tapi CoreWebView2 tetap null di beberapa environment.
                    LogWebViewIssue("CoreWebView2 is null setelah EnsureCoreWebView2Async " +
                        "(tidak ada exception, tapi init gagal secara silent)");
                    return;
                }

                // Log versi runtime yang benar-benar dipakai — berguna untuk
                // membandingkan versi WebView2 antar laptop yang bermasalah vs tidak.
                LogWebViewIssue($"WebView2 initialized OK. Runtime version: {environment.BrowserVersionString}");

                _player ??= new WebViewPlayer(PlayerWebView);
                _webViewInitialized = true;
            }
            catch (Exception ex)
            {
                _webViewInitialized = false;
                LogWebViewIssue($"Exception: {ex}");
            }
        }

        // Helper terpusat supaya semua jalur (exception maupun silent-null) pasti ke-log.
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
                // Kalau logging pun gagal, tidak ada lagi yang bisa dilakukan di sini
            }
        }
    }
}
