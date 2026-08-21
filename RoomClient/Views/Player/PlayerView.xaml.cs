using LibVLCSharp.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;
using RoomClient.Core.Interfaces;
using RoomClient.Core.Models;
using RoomClient.Helpers;
using RoomClient.ViewModels;
using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RoomClient.Views.Player
{
    public partial class PlayerView : UserControl
    {
        private enum PlaybackSurface
        {
            None,
            Vlc,
            WebView
        }

        private StatusViewModel? _statusViewModel;
        private PlayerViewModel? _playerViewModel;
        private LibVLC? _libVlc;
        private MediaPlayer? _vlcMediaPlayer;
        private bool _vlcInitialized;
        private WebViewPlayer? _webViewPlayer;
        private PlaybackSurface _activeSurface = PlaybackSurface.None;

        public PlayerView()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            DataContextChanged += OnDataContextChanged;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            EnsureVlcInitialized();
            _ = EnsureWebViewInitializedAsync();
            VlcVideoView.SizeChanged += (s, args) => UpdateVideoAspectRatio();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Jangan dispose di sini agar playback tidak berhenti saat reparenting ke FullScreen / sebaliknya
        }

        public void DisposeVlc()
        {
            _webViewPlayer?.Dispose();
            _webViewPlayer = null;
            _vlcMediaPlayer?.Stop();
            _vlcMediaPlayer?.Dispose();
            _libVlc?.Dispose();
            _vlcInitialized = false;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DetachPlayerViewModel();

            if (DataContext is MainWindowViewModel mainWindowViewModel)
            {
                _statusViewModel = mainWindowViewModel.Status;
                AttachPlayerViewModel(mainWindowViewModel.Player);
                _ = LoadPlaybackSourceAsync();
            }
        }

        private void AttachPlayerViewModel(PlayerViewModel playerViewModel)
        {
            _playerViewModel = playerViewModel;
            _playerViewModel.PropertyChanged += OnPlayerPropertyChanged;
            _playerViewModel.VlcCommandRequested += OnVlcCommandRequested;
        }

        private void DetachPlayerViewModel()
        {
            if (_playerViewModel is null)
            {
                return;
            }

            _playerViewModel.PropertyChanged -= OnPlayerPropertyChanged;
            _playerViewModel.VlcCommandRequested -= OnVlcCommandRequested;
            _playerViewModel = null;
            _statusViewModel = null;
        }

        private const string HttpUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
        private const string HttpReferrer = "https://www.youtube.com/";

        private async void OnVlcCommandRequested(object? sender, VlcCommand cmd)
        {
            try
            {
                switch (cmd.Type)
                {
                    case VlcCommandType.Play:
                        if (!string.IsNullOrWhiteSpace(cmd.Source) && _libVlc is not null)
                        {
                            EnsureActiveSurface(ResolveSurface(cmd.Source));
                            if (_activeSurface == PlaybackSurface.Vlc && _vlcMediaPlayer is not null)
                            {
                                using var media = CreateMedia(cmd.Source);
                                _vlcMediaPlayer.Play(media);
                            }
                            else if (_activeSurface == PlaybackSurface.WebView)
                            {
                                await PlayInWebViewAsync(cmd.Source);
                            }
                        }
                        break;
                    case VlcCommandType.Pause:
                        if (_activeSurface == PlaybackSurface.Vlc)
                        {
                            _vlcMediaPlayer?.Pause();
                        }
                        else if (_activeSurface == PlaybackSurface.WebView)
                        {
                            await _webViewPlayer?.ExecuteScriptAsync("pauseVideo();")!;
                        }
                        break;
                    case VlcCommandType.Resume:
                        if (_activeSurface == PlaybackSurface.Vlc)
                        {
                            _vlcMediaPlayer?.Play();
                        }
                        else if (_activeSurface == PlaybackSurface.WebView)
                        {
                            await _webViewPlayer?.ExecuteScriptAsync("resumeVideo();")!;
                        }
                        break;
                    case VlcCommandType.Stop:
                        if (_activeSurface == PlaybackSurface.Vlc)
                        {
                            _vlcMediaPlayer?.Stop();
                        }
                        else if (_activeSurface == PlaybackSurface.WebView)
                        {
                            _webViewPlayer?.Clear();
                        }
                        break;
                    case VlcCommandType.Seek:
                        if (_activeSurface == PlaybackSurface.Vlc && cmd.Position.HasValue && _vlcMediaPlayer is not null)
                        {
                            _vlcMediaPlayer.Time = (long)cmd.Position.Value.TotalMilliseconds;
                        }
                        break;
                    case VlcCommandType.SetVolume:
                        if (cmd.Volume.HasValue)
                        {
                            if (_activeSurface == PlaybackSurface.Vlc && _vlcMediaPlayer is not null)
                            {
                                _vlcMediaPlayer.Volume = (int)cmd.Volume.Value;
                            }
                            else if (_activeSurface == PlaybackSurface.WebView)
                            {
                                var normalized = Math.Clamp(cmd.Volume.Value / 100.0, 0, 1);
                                await _webViewPlayer?.ExecuteScriptAsync($"document.getElementById('player').volume = {normalized.ToString(System.Globalization.CultureInfo.InvariantCulture)};")!;
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                var errorLog = $"[PLAYER] Command handling error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(errorLog);
                _statusViewModel?.AddLog(errorLog);
            }
        }

        private async void OnPlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlayerViewModel.PlaybackSourceUrl))
            {
                await LoadPlaybackSourceAsync();
            }
            else if (e.PropertyName == nameof(PlayerViewModel.IsFullScreen))
            {
                Dispatcher.BeginInvoke(UpdateVideoAspectRatio);
            }
        }

        public void UpdateVideoAspectRatio()
        {
            if (!_vlcInitialized || _vlcMediaPlayer is null) return;

            try
            {
                if (VlcVideoView.ActualWidth > 0 && VlcVideoView.ActualHeight > 0)
                {
                    _vlcMediaPlayer.AspectRatio = $"{(int)VlcVideoView.ActualWidth}:{(int)VlcVideoView.ActualHeight}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VLC] UpdateVideoAspectRatio error: {ex.Message}");
            }
        }

        private async Task LoadPlaybackSourceAsync()
        {
            var url = _playerViewModel?.PlaybackSourceUrl;

            if (string.IsNullOrEmpty(url))
            {
                _vlcMediaPlayer?.Stop();
                _webViewPlayer?.Clear();
                _activeSurface = PlaybackSurface.None;
                return;
            }

            try
            {
                var surface = ResolveSurface(url);
                var engineLog = $"[PLAYBACK] Engine={surface} Source={url}";
                var detailLog = $"[PLAYBACK] Source detail: {DescribeSource(url)}";
                System.Diagnostics.Debug.WriteLine(engineLog);
                System.Diagnostics.Debug.WriteLine(detailLog);
                _statusViewModel?.AddLog(engineLog);
                EnsureActiveSurface(surface);

                if (surface == PlaybackSurface.Vlc)
                {
                    EnsureVlcInitialized();

                    if (_libVlc is not null && _vlcMediaPlayer is not null)
                    {
                        using var media = CreateMedia(url);

                        if (_playerViewModel is not null)
                        {
                            _vlcMediaPlayer.Volume = (int)_playerViewModel.Volume;
                        }

                        _vlcMediaPlayer.Play(media);
                    }
                }
                else
                {
                    await PlayInWebViewAsync(url);
                }
            }
            catch (Exception ex)
            {
                var errorLog = $"[PLAYER] Load source error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(errorLog);
                _statusViewModel?.AddLog(errorLog);
            }

            await Task.CompletedTask;
        }

        private PlaybackSurface ResolveSurface(string source)
        {
            if (Uri.TryCreate(source, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return PlaybackSurface.WebView;
            }

            var extension = Path.GetExtension(source);
            if (string.Equals(extension, ".dat", StringComparison.OrdinalIgnoreCase))
            {
                return PlaybackSurface.Vlc;
            }

            return PlaybackSurface.Vlc;
        }

        private static string DescribeSource(string source)
        {
            var extension = Path.GetExtension(source);

            if (Uri.TryCreate(source, UriKind.Absolute, out var uri))
            {
                return $"AbsoluteUri scheme={uri.Scheme} host={uri.Host} ext={extension}";
            }

            return $"LocalPath ext={extension}";
        }

        private void EnsureActiveSurface(PlaybackSurface surface)
        {
            _activeSurface = surface;
            var surfaceLog = $"[PLAYBACK] Switching active surface -> {surface}";
            System.Diagnostics.Debug.WriteLine(surfaceLog);
            _statusViewModel?.AddLog(surfaceLog);

            if (surface == PlaybackSurface.Vlc)
            {
                StreamWebView.Visibility = Visibility.Collapsed;
                VlcVideoView.Visibility = Visibility.Visible;
                _webViewPlayer?.Clear();
            }
            else if (surface == PlaybackSurface.WebView)
            {
                VlcVideoView.Visibility = Visibility.Collapsed;
                StreamWebView.Visibility = Visibility.Visible;
                _vlcMediaPlayer?.Stop();
            }
            else
            {
                VlcVideoView.Visibility = Visibility.Collapsed;
                StreamWebView.Visibility = Visibility.Collapsed;
            }
        }

        private async Task EnsureWebViewInitializedAsync()
        {
            if (StreamWebView.CoreWebView2 is null)
            {
                System.Diagnostics.Debug.WriteLine("[WEBVIEW] Initializing CoreWebView2...");
                _statusViewModel?.AddLog("[WEBVIEW] Initializing CoreWebView2...");
                await StreamWebView.EnsureCoreWebView2Async();
                System.Diagnostics.Debug.WriteLine("[WEBVIEW] CoreWebView2 initialized.");
                _statusViewModel?.AddLog("[WEBVIEW] CoreWebView2 initialized.");
            }

            _webViewPlayer ??= new WebViewPlayer(StreamWebView);
            StreamWebView.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
            StreamWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
        }

        private async Task PlayInWebViewAsync(string source)
        {
            await EnsureWebViewInitializedAsync();
            if (_webViewPlayer is null || _playerViewModel is null)
            {
                return;
            }

            var webViewLoadLog = $"[WEBVIEW] Loading stream URL: {source}";
            System.Diagnostics.Debug.WriteLine(webViewLoadLog);
            _statusViewModel?.AddLog(webViewLoadLog);
            var html = App.Services.GetRequiredService<IYoutubeService>().BuildPlayerHtml(source);
            _webViewPlayer.LoadHtml(html);
            await _webViewPlayer.SetVolumeAsync(_playerViewModel.Volume);
            var webViewLoadedLog = $"[WEBVIEW] Stream loaded. Volume={_playerViewModel.Volume}";
            System.Diagnostics.Debug.WriteLine(webViewLoadedLog);
            _statusViewModel?.AddLog(webViewLoadedLog);
        }

        private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var messageJson = e.TryGetWebMessageAsString();
                using var document = JsonDocument.Parse(messageJson);
                var type = document.RootElement.TryGetProperty("type", out var typeElement)
                    ? typeElement.GetString()
                    : null;

                Dispatcher.BeginInvoke(() =>
                {
                    switch (type)
                    {
                        case "play":
                            System.Diagnostics.Debug.WriteLine("[WEBVIEW] Event: play");
                            _statusViewModel?.AddLog("[WEBVIEW] Event: play");
                            _playerViewModel?.NotifyPlaybackState(PlaybackState.Playing);
                            break;
                        case "pause":
                            System.Diagnostics.Debug.WriteLine("[WEBVIEW] Event: pause");
                            _statusViewModel?.AddLog("[WEBVIEW] Event: pause");
                            _playerViewModel?.NotifyPlaybackState(PlaybackState.Paused);
                            break;
                        case "ended":
                            System.Diagnostics.Debug.WriteLine("[WEBVIEW] Event: ended");
                            _statusViewModel?.AddLog("[WEBVIEW] Event: ended");
                            _playerViewModel?.NotifySongEnded();
                            break;
                        case "error":
                            System.Diagnostics.Debug.WriteLine("[WEBVIEW] Event: error");
                            _statusViewModel?.AddLog("[WEBVIEW] Event: error");
                            break;
                    }
                });
            }
            catch (Exception ex)
            {
                var errorLog = $"[WEBVIEW] Message parse error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(errorLog);
                _statusViewModel?.AddLog(errorLog);
            }
        }

        private Media CreateMedia(string source)
        {
            Media media;
            if (Uri.TryCreate(source, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                media = new Media(_libVlc!, uri);
                media.AddOption($":http-user-agent={HttpUserAgent}");
                media.AddOption($":http-referrer={HttpReferrer}");
            }
            else
            {
                media = new Media(_libVlc!, source, FromType.FromPath);
            }

            return media;
        }

        private void EnsureVlcInitialized()
        {
            if (_vlcInitialized) return;

            try
            {
                LibVLCSharp.Shared.Core.Initialize();

                _libVlc = new LibVLC(
                    "--no-video-title-show"
                );

                _libVlc.Log += (sender, e) =>
                {
                    System.Diagnostics.Debug.WriteLine($"[VLC Log][{e.Level}] {e.Module}: {e.Message}");
                };

                _vlcMediaPlayer = new MediaPlayer(_libVlc);
                VlcVideoView.MediaPlayer = _vlcMediaPlayer;
                _statusViewModel?.AddLog("[VLC] Engine initialized.");

                _vlcMediaPlayer.EndReached += (s, e) =>
                {
                    try
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            _statusViewModel?.AddLog("[VLC] Event: ended");
                            _playerViewModel?.NotifySongEnded();
                        });
                    }
                    catch { }
                };

                _vlcMediaPlayer.Playing += (s, e) =>
                {
                    try
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            _statusViewModel?.AddLog("[VLC] Event: play");
                            _playerViewModel?.NotifyPlaybackState(PlaybackState.Playing);
                            UpdateVideoAspectRatio();
                        });
                    }
                    catch { }
                };

                _vlcMediaPlayer.Paused += (s, e) =>
                {
                    try
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            _statusViewModel?.AddLog("[VLC] Event: pause");
                            _playerViewModel?.NotifyPlaybackState(PlaybackState.Paused);
                        });
                    }
                    catch { }
                };

                _vlcMediaPlayer.EncounteredError += (s, e) =>
                {
                    const string errorLog = "[VLC] Playback error encountered.";
                    System.Diagnostics.Debug.WriteLine(errorLog);
                    Dispatcher.BeginInvoke(() => _statusViewModel?.AddLog(errorLog));
                };

                _vlcInitialized = true;
            }
            catch (Exception ex)
            {
                var errorLog = $"[VLC] EnsureVlcInitialized failed: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(errorLog);
                _statusViewModel?.AddLog(errorLog);
            }
        }
    }
}
