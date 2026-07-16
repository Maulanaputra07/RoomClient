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
                var options = new CoreWebView2EnvironmentOptions(
                    "--autoplay-policy=no-user-gesture-required");

                var environment = await CoreWebView2Environment.CreateAsync(
                    browserExecutableFolder: null,
                    userDataFolder: null,
                    options: options);

                await PlayerWebView.EnsureCoreWebView2Async(environment);

                if (PlayerWebView.CoreWebView2 is null)
                {
                    return;
                }

                _player ??= new WebViewPlayer(PlayerWebView);
                _webViewInitialized = true;
            }
            catch
            {
                _webViewInitialized = false;
            }
        }
    }
}
