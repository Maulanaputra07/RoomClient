using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoomClient.Core.Interfaces;
using RoomClient.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace RoomClient.ViewModels
{
    public partial class PlayerViewModel : ObservableObject, IDisposable
    {
        private readonly IPlayerService _playerService;
        private readonly IYoutubeService _youtubeService;
        private DateTimeOffset? _sessionEndTime;
        private DispatcherTimer? _countdownTimer;
        public event EventHandler? SessionFullyExpired;
        public event EventHandler<VlcCommand>? VlcCommandRequested;

        private readonly Stack<Song> _history = new();
        public Func<bool>? HasNextSong { get; set; }
        public Func<Song?>? DequeueNextSong { get; set; }

        [ObservableProperty]
        private string _nowPlaying = "Tidak ada lagu yang diputar";

        [ObservableProperty]
        private bool _isSessionActive;

        [ObservableProperty]
        private bool _isSessionExpired;

        [ObservableProperty]
        private string _remainingTimeText = "00:00";

        [ObservableProperty]
        private bool _isFullScreen;

        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private Song? _currentSong;

        [ObservableProperty]
        private bool _showOneMinuteWarning;

        [ObservableProperty]
        private double _volume = 100;

        [ObservableProperty]
        private bool _isMuted = false;

        [ObservableProperty]
        private string? _vlcSourceUrl;

        private double _previousVolume = 100;
        private bool _isSessionExpiredPending;

        partial void OnVolumeChanged(double value)
        {
            _ = _playerService.SetVolumeAsync(value);
            IsMuted = value == 0;
        }

        public PlayerViewModel(IPlayerService playerService, IYoutubeService youtubeService)
        {
            _playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
            _youtubeService = youtubeService ?? throw new ArgumentNullException(nameof(youtubeService));

            _playerService.CurrentSongChanged += OnCurrentSongChanged;
            _playerService.PlaybackStateChanged += OnPlaybackStateChanged;
            _playerService.VlcCommandRequested += OnVlcCommandRequested;
        }

        private void OnVlcCommandRequested(object? sender, VlcCommand cmd) => VlcCommandRequested?.Invoke(this, cmd);

        [RelayCommand]
        private async Task TogglePlayPauseAsync()
        {
            if (CurrentSong == null) return;

            if (IsPlaying)
            {
                await _playerService.PauseAsync();
            }
            else
            {
                await _playerService.ResumeAsync();
            }
        }

        public void ActivateSession(DateTimeOffset sessionEndTime)
        {
            _isSessionExpiredPending = false;
            _sessionEndTime = sessionEndTime;

            IsSessionActive = true;
            IsSessionExpired = false;
            ShowOneMinuteWarning = false;

            StartCountdown();
        }

        public void ExtendSession(DateTimeOffset newSessionEndTime)
        {
            _isSessionExpiredPending = false;
            _sessionEndTime = newSessionEndTime;
            IsSessionActive = true;
            IsSessionExpired = false;
            ShowOneMinuteWarning = false;
            if (_countdownTimer is null || !_countdownTimer.IsEnabled)
            {
                StartCountdown();
            }
            else
            {
                UpdateRemainingTime();
            }
        }

        private void StartCountdown()
        {
            if (_countdownTimer is null)
            {
                _countdownTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                _countdownTimer.Tick += OnTimerTick;
            }

            _countdownTimer.Stop();
            _countdownTimer.Start();
            UpdateRemainingTime();
        }

        private void OnTimerTick(object? sender, EventArgs e) => UpdateRemainingTime();

        private void UpdateRemainingTime()
        {
            if (_sessionEndTime is null)
            {
                RemainingTimeText = "00:00";
                ShowOneMinuteWarning = false;
                return;
            }

            var remaining = _sessionEndTime.Value.ToUniversalTime() - DateTimeOffset.UtcNow;

            if (remaining < TimeSpan.FromSeconds(0.5))
            {
                RemainingTimeText = "00:00";
                _countdownTimer?.Stop();
                ExpireSession();
                return;
            }

            IsSessionActive = true;
            IsSessionExpired = false;

            ShowOneMinuteWarning = remaining.TotalSeconds <= 60;

            RemainingTimeText = remaining.Hours > 0
                ? remaining.ToString(@"hh\:mm\:ss")
                : remaining.ToString(@"mm\:ss");
        }

        public void ExpireSession()
        {
            if (IsSessionExpired) return;

            var isCurrentlyPlaying = CurrentSong is not null && !string.IsNullOrEmpty(VlcSourceUrl);

            if (isCurrentlyPlaying)
            {
                // Grace period: biarkan lagu selesai diputar dulu
                _isSessionExpiredPending = true;
                NextCommand.NotifyCanExecuteChanged();
                PreviousCommand.NotifyCanExecuteChanged();
            }
            else
            {
                FinalizeSessionExpiry();
            }
        }

        private void FinalizeSessionExpiry()
        {
            _isSessionExpiredPending = false;

            if (IsSessionActive)
            {
                IsSessionExpired = true;
            }
            IsSessionActive = false;
            ShowOneMinuteWarning = false;
            NowPlaying = "Sesi telah berakhir";

            _ = StopPlayerAfterSessionExpiredAsync();
            SessionFullyExpired?.Invoke(this, EventArgs.Empty);
        }

        private async Task StopPlayerAfterSessionExpiredAsync()
        {
            try
            {
                await StopPlaybackCoreAsync(resetSessionClock: false, keepSessionState: false, nowPlayingText: "Sesi telah berakhir");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Failed to stop player after session expired: {ex.Message}");
            }
        }

        private async Task StopPlaybackCoreAsync(bool resetSessionClock, bool keepSessionState, string nowPlayingText)
        {
            _isSessionExpiredPending = false;
            VlcSourceUrl = null;
            NowPlaying = nowPlayingText;
            ShowOneMinuteWarning = false;

            if (resetSessionClock)
            {
                RemainingTimeText = "00:00";
            }

            if (!keepSessionState)
            {
                if (IsSessionActive)
                {
                    IsSessionExpired = true;
                }

                IsSessionActive = false;

                if (_countdownTimer != null)
                {
                    _countdownTimer.Stop();
                }
            }

            await _playerService.StopAsync();
        }

        public async Task StopPlaybackAsync()
        {
            await StopPlaybackCoreAsync(
                resetSessionClock: false,
                keepSessionState: true,
                nowPlayingText: "Tidak ada lagu yang diputar");
        }

        public async Task PlayAsync(Song song)
        {
            if (!IsSessionActive) { NowPlaying = "Sesi belum dimulai"; return; }
            if (_isSessionExpiredPending) return;

            var hasValidIdentifier = song.Source == SongSource.Database
                ? !string.IsNullOrWhiteSpace(song.DirectStreamUrl)
                : !string.IsNullOrWhiteSpace(song.VideoId);

            if (!hasValidIdentifier) return;

            if (CurrentSong is not null && CurrentSong.VideoId != song.VideoId)
            {
                _history.Push(CurrentSong);
                PreviousCommand.NotifyCanExecuteChanged();
            }

            NowPlaying = "Memuat...";

            try
            {
                string? streamUrl = song.Source == SongSource.Database
                    ? song.DirectStreamUrl
                    : await _youtubeService.GetStreamUrlAsync(song.VideoId);

                if (!IsSessionActive) { NowPlaying = "Sesi telah berakhir"; return; }

                System.Diagnostics.Debug.WriteLine($"[DEBUG-STREAM] Song: '{song.Title}' | Source: {song.Source} | URL: {streamUrl}");

                if (string.IsNullOrWhiteSpace(streamUrl))
                {
                    NowPlaying = "Gagal memuat stream";
                    return;
                }

                VlcSourceUrl = streamUrl;
                await _playerService.PlayAsync(song);
            }
            catch (Exception ex)
            {
                NowPlaying = $"Gagal memutar: {ex.Message}";
            }
        }

        [RelayCommand]
        private void ToggleMute()
        {
            if (IsMuted)
            {
                Volume = _previousVolume == 0 ? 50 : _previousVolume;
            }
            else
            {
                _previousVolume = Volume;
                Volume = 0;
            }
        }

        [RelayCommand]
        public async Task StopAsync()
        {
            await StopPlaybackAsync();
        }

        private bool CanGoNext() => !_isSessionExpiredPending && (HasNextSong?.Invoke() ?? false);
        private bool CanGoPrevious() => !_isSessionExpiredPending && _history.Count > 0;

        [RelayCommand(CanExecute = nameof(CanGoNext))]
        private async Task NextAsync()
        {
            var nextSong = DequeueNextSong?.Invoke();
            if (nextSong is not null)
            {
                await PlayAsync(nextSong);
            }
        }

        [RelayCommand(CanExecute = nameof(CanGoPrevious))]
        private async Task PreviousAsync()
        {
            if (_history.Count == 0) return;
            var prevSong = _history.Pop();
            await PlayAsync(prevSong);
        }

        private void OnCurrentSongChanged(object? sender, Song? song)
        {
            CurrentSong = song;
            if (song != null)
            {
                NowPlaying = string.IsNullOrWhiteSpace(song.Artist)
                    ? song.Title
                    : $"{song.Title} - {song.Artist}";
            }
            else if (!_isSessionExpiredPending && IsSessionActive)
            {
                NowPlaying = "Tidak ada lagu yang diputar";
            }

            NextCommand.NotifyCanExecuteChanged();
            PreviousCommand.NotifyCanExecuteChanged();
        }

        private void OnPlaybackStateChanged(object? sender, PlaybackState state)
        {
            IsPlaying = state == PlaybackState.Playing;
        }

        public void Dispose()
        {
            _playerService.CurrentSongChanged -= OnCurrentSongChanged;
            _playerService.PlaybackStateChanged -= OnPlaybackStateChanged;
            _playerService.VlcCommandRequested -= OnVlcCommandRequested;

            if (_countdownTimer != null)
            {
                _countdownTimer.Stop();
                _countdownTimer.Tick -= OnTimerTick;
            }
        }

        public void NotifyPlaybackState(PlaybackState state) => _playerService.UpdatePlaybackState(state);

        public void NotifySongEnded()
        {
            if (_isSessionExpiredPending)
            {
                FinalizeSessionExpiry();
                return;
            }

            if (NextCommand.CanExecute(null))
            {
                NextCommand.Execute(null);
            }
            else
            {
                _ = StopPlaybackAsync();
            }
        }
    }
}
