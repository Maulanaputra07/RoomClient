using RoomClient.ViewModels;
using RoomClient.Helpers;
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
            if (!_webViewInitialized || _playerViewModel?.PlayerHtml is not { Length: > 0 } html)
            {
                return;
            }

            _player?.LoadHtml(html);
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
                await PlayerWebView.EnsureCoreWebView2Async();

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
