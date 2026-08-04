using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoomClient.Core.Interfaces;
using RoomClient.Core.Models;
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

        // Source Generator otomatis mendefinisikan properti PascalCase untuk setiap field di bawah
        [ObservableProperty]
        private string _nowPlaying = "Tidak ada lagu yang diputar";

        [ObservableProperty]
        private string? _playerHtml;

        [ObservableProperty]
        private bool _isSessionActive;

        [ObservableProperty]
        private string _remainingTimeText = string.Empty;

        [ObservableProperty]
        private bool _isFullScreen;

        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private Song? _currentSong;

        public PlayerViewModel(IPlayerService playerService, IYoutubeService youtubeService)
        {
            _playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
            _youtubeService = youtubeService ?? throw new ArgumentNullException(nameof(youtubeService));

            _playerService.CurrentSongChanged += OnCurrentSongChanged;
            _playerService.PlaybackStateChanged += OnPlaybackStateChanged;
        }

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
            IsSessionActive = true;
            _sessionEndTime = sessionEndTime;
            StartCountdown();
        }

        public void ExtendSession(DateTimeOffset newSessionEndTime)
        {
            _sessionEndTime = newSessionEndTime;
            if (_countdownTimer is null || !_countdownTimer.IsEnabled)
            {
                StartCountdown();
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
                return;
            }

            var remaining = _sessionEndTime.Value - DateTimeOffset.Now;

            if (remaining <= TimeSpan.Zero)
            {
                RemainingTimeText = "00:00";
                _countdownTimer?.Stop();
                IsSessionActive = false;
                NowPlaying = "Sesi telah berakhir";
                _ = StopAsync();
                return;
            }

            RemainingTimeText = remaining.Hours > 0
                ? remaining.ToString(@"hh\:mm\:ss")
                : remaining.ToString(@"mm\:ss");
        }

        public async Task PlayAsync(Song song)
        {
            if (!IsSessionActive)
            {
                NowPlaying = "Sesi belum dimulai";
                return;
            }

            if (string.IsNullOrWhiteSpace(song.VideoId)) return;

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
            IsSessionActive = false;

            if (_countdownTimer != null)
            {
                _countdownTimer.Stop();
            }

            await _playerService.StopAsync();
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
        }

        private void OnPlaybackStateChanged(object? sender, PlaybackState state)
        {
            IsPlaying = state == PlaybackState.Playing;
        }

        public void Dispose()
        {
            _playerService.CurrentSongChanged -= OnCurrentSongChanged;
            _playerService.PlaybackStateChanged -= OnPlaybackStateChanged;

            if (_countdownTimer != null)
            {
                _countdownTimer.Stop();
                _countdownTimer.Tick -= OnTimerTick;
            }
        }
    }
}