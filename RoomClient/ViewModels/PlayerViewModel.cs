using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoomClient.Core.Interfaces;
using RoomClient.Core.Models;
using System.Collections.Generic;
using System;
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
        public event EventHandler<string>? JavaScriptCommandRequested;

        private readonly Stack<Song> _history = new();
        public Func<bool>? HasNextSong { get; set; }
        public Func<Song?>? DequeueNextSong { get; set; }

        // Source Generator otomatis mendefinisikan properti PascalCase untuk setiap field di bawah
        [ObservableProperty]
        private string _nowPlaying = "Tidak ada lagu yang diputar";

        [ObservableProperty]
        private string? _playerHtml;

        [ObservableProperty]
        private bool _isSessionActive;

        [ObservableProperty]
        private bool _isSessionExpired;

        [ObservableProperty]
        private string _remainingTimeText = string.Empty;

        [ObservableProperty]
        private bool _isFullScreen;

        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private Song? _currentSong;

        [ObservableProperty]
        private bool _showOneMinuteWarning;

        public PlayerViewModel(IPlayerService playerService, IYoutubeService youtubeService)
        {
            _playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
            _youtubeService = youtubeService ?? throw new ArgumentNullException(nameof(youtubeService));

            _playerService.CurrentSongChanged += OnCurrentSongChanged;
            _playerService.PlaybackStateChanged += OnPlaybackStateChanged;
            _playerService.JavaScriptCommandRequested += OnJavaScriptCommandRequested;
        }
        private void OnJavaScriptCommandRequested(object? sender, string js) => JavaScriptCommandRequested?.Invoke(this, js);


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
            _sessionEndTime = sessionEndTime;

            IsSessionActive = true;
            IsSessionExpired = false;
            ShowOneMinuteWarning = false;

            StartCountdown();
        }

        public void ExtendSession(DateTimeOffset newSessionEndTime)
        {
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
                RemainingTimeText = string.Empty;
                ShowOneMinuteWarning = false;
                return;
            }

            var remaining = _sessionEndTime.Value.ToUniversalTime() - DateTimeOffset.UtcNow;

            if (remaining < TimeSpan.FromSeconds(0.5))
            {
                RemainingTimeText = "00:00";
                _countdownTimer?.Stop();

                if (IsSessionActive)
                {
                    IsSessionExpired = true;
                }

                IsSessionActive = false;
                ShowOneMinuteWarning = false;

                NowPlaying = "Sesi telah berakhir";
                _ = StopPlayerAfterSessionExpiredAsync();
                return;
            }

            IsSessionActive = true;
            IsSessionExpired = false;

            ShowOneMinuteWarning = remaining.TotalSeconds <= 60;

            RemainingTimeText = remaining.Hours > 0
                ? remaining.ToString(@"hh\:mm\:ss")
                : remaining.ToString(@"mm\:ss");
        }

        private async Task StopPlayerAfterSessionExpiredAsync()
        {
            PlayerHtml = null;

            try
            {
                await _playerService.StopAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Failed to stop player after session expired: {ex.Message}");
            }
        }

        public async Task PlayAsync(Song song)
        {
            if (!IsSessionActive)
            {
                NowPlaying = "Sesi belum dimulai";
                return;
            }

            if (string.IsNullOrWhiteSpace(song.VideoId)) return;

            if (CurrentSong is not null && CurrentSong.VideoId != song.VideoId)
            {
                _history.Push(CurrentSong);
                PreviousCommand.NotifyCanExecuteChanged();
            }

            NowPlaying = "Memuat...";

            try
            {
                PlayerHtml = null;

                var streamUrl = await _youtubeService.GetStreamUrlAsync(song.VideoId);

                if (string.IsNullOrWhiteSpace(streamUrl))
                {
                    NowPlaying = "Gagal memuat stream";
                    return;
                }

                PlayerHtml = _youtubeService.BuildPlayerHtml(streamUrl);
                await _playerService.PlayAsync(song);
            }
            catch (Exception ex)
            {
                NowPlaying = $"Gagal memutar: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task StopAsync()
        {
            NowPlaying = "waiting";
            PlayerHtml = null;
            if (IsSessionActive)
            {
                IsSessionExpired = true;
            }
            IsSessionActive = false;
            ShowOneMinuteWarning = false;

            if (_countdownTimer != null)
            {
                _countdownTimer.Stop();
            }

            await _playerService.StopAsync();
        }

        private bool CanGoNext() => HasNextSong?.Invoke() ?? false;
        private bool CanGoPrevious() => _history.Count > 0;

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
            _playerService.JavaScriptCommandRequested -= OnJavaScriptCommandRequested;

            if (_countdownTimer != null)
            {
                _countdownTimer.Stop();
                _countdownTimer.Tick -= OnTimerTick;
            }
        }

        public void NotifyWebViewPlaybackState(PlaybackState state) => _playerService.UpdatePlaybackStateFromWebView(state);

        public void NotifySongEnded()
        {
            if (NextCommand.CanExecute(null))
                NextCommand.Execute(null);
            else
                _ = StopAsync();
        }
    }
}