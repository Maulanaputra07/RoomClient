using LibVLCSharp.Shared;
using RoomClient.Core.Interfaces;
using RoomClient.Core.Models;
using RoomClient.ViewModels;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RoomClient.Views.Player
{
    public partial class PlayerView : UserControl
    {
        private PlayerViewModel? _playerViewModel;
        private LibVLC? _libVlc;
        private MediaPlayer? _vlcMediaPlayer;
        private bool _vlcInitialized;

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
            VlcVideoView.SizeChanged += (s, args) => UpdateVideoAspectRatio();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // Jangan dispose di sini agar playback tidak berhenti saat reparenting ke FullScreen / sebaliknya
        }

        public void DisposeVlc()
        {
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
                AttachPlayerViewModel(mainWindowViewModel.Player);
                _ = LoadVlcSourceAsync();
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
        }

        private const string HttpUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";
        private const string HttpReferrer = "https://www.youtube.com/";

        private void OnVlcCommandRequested(object? sender, VlcCommand cmd)
        {
            if (!_vlcInitialized || _vlcMediaPlayer is null) return;

            try
            {
                switch (cmd.Type)
                {
                    case VlcCommandType.Play:
                        if (!string.IsNullOrWhiteSpace(cmd.Source) && _libVlc is not null)
                        {
                            using var media = CreateMedia(cmd.Source);
                            _vlcMediaPlayer.Play(media);
                        }
                        break;
                    case VlcCommandType.Pause:
                        _vlcMediaPlayer.Pause();
                        break;
                    case VlcCommandType.Resume:
                        _vlcMediaPlayer.Play();
                        break;
                    case VlcCommandType.Stop:
                        _vlcMediaPlayer.Stop();
                        break;
                    case VlcCommandType.Seek:
                        if (cmd.Position.HasValue)
                        {
                            _vlcMediaPlayer.Time = (long)cmd.Position.Value.TotalMilliseconds;
                        }
                        break;
                    case VlcCommandType.SetVolume:
                        if (cmd.Volume.HasValue)
                        {
                            _vlcMediaPlayer.Volume = (int)cmd.Volume.Value;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VLC] OnVlcCommandRequested error: {ex.Message}");
            }
        }

        private async void OnPlayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlayerViewModel.VlcSourceUrl))
            {
                await LoadVlcSourceAsync();
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

        private async Task LoadVlcSourceAsync()
        {
            var url = _playerViewModel?.VlcSourceUrl;

            if (string.IsNullOrEmpty(url))
            {
                _vlcMediaPlayer?.Stop();
                return;
            }

            try
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VLC] LoadVlcSourceAsync error: {ex.Message}");
            }

            await Task.CompletedTask;
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

                _vlcMediaPlayer.EndReached += (s, e) =>
                {
                    try
                    {
                        Dispatcher.BeginInvoke(() => _playerViewModel?.NotifySongEnded());
                    }
                    catch { }
                };

                _vlcMediaPlayer.Playing += (s, e) =>
                {
                    try
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
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
                        Dispatcher.BeginInvoke(() => _playerViewModel?.NotifyPlaybackState(PlaybackState.Paused));
                    }
                    catch { }
                };

                _vlcMediaPlayer.EncounteredError += (s, e) =>
                    System.Diagnostics.Debug.WriteLine("[VLC] Playback error encountered.");

                _vlcInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VLC] EnsureVlcInitialized failed: {ex.Message}");
            }
        }
    }
}