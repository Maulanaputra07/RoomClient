using CommunityToolkit.Mvvm.ComponentModel;
using RoomClient.Core.Interfaces;
using RoomClient.Core.Models;
using System.Windows.Threading;

namespace RoomClient.ViewModels
{
    public partial class PlayerViewModel : ObservableObject
    {
        private readonly IYoutubeService _youtubeService;
        private string _nowPlaying = "waiting";
        private string? _playerHtml;
        private bool _isSessionActive;
        private string _remainingTimeText = string.Empty;

        private DateTimeOffset? _sessionEndTime;
        private DispatcherTimer? _countdownTimer;

        private bool _isFullScreen;

        public bool IsFullScreen
        {
            get => _isFullScreen;
            set => SetProperty(ref _isFullScreen, value);
        }

        public PlayerViewModel(IYoutubeService youtubeService)
        {
            _youtubeService = youtubeService;
        }

        public string NowPlaying
        {
            get => _nowPlaying;
            set => SetProperty(ref _nowPlaying, value);
        }

        public string? PlayerHtml
        {
            get => _playerHtml;
            private set => SetProperty(ref _playerHtml, value);
        }

        public bool IsSessionActive
        {
            get => _isSessionActive;
            private set => SetProperty(ref _isSessionActive, value);
        }

        public string RemainingTimeText
        {
            get => _remainingTimeText;
            private set => SetProperty(ref _remainingTimeText, value);
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
            _countdownTimer?.Stop();
            _countdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _countdownTimer.Tick += (s, e) => UpdateRemainingTime();
            _countdownTimer.Start();

            UpdateRemainingTime();
        }

        private void UpdateRemainingTime()
        {
            if (_sessionEndTime is null)
            {
                RemainingTimeText = string.Empty;
                return;
            }

            var remaining = _sessionEndTime.Value - DateTime.Now;

            if (remaining <= TimeSpan.Zero)
            {
                RemainingTimeText = "00:00";
                _countdownTimer?.Stop();
                IsSessionActive = false;
                NowPlaying = "Sesi telah berakhir";
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

            if (string.IsNullOrWhiteSpace(song.VideoId))
            {
                return;
            }

            NowPlaying = "Memuat...";

            try
            {
                var streamUrl = await _youtubeService.GetStreamUrlAsync(song.VideoId);

                if (string.IsNullOrWhiteSpace(streamUrl))
                {
                    NowPlaying = "Gagal memuat stream";
                    return;
                }

                NowPlaying = string.IsNullOrWhiteSpace(song.Artist)
                    ? song.Title
                    : $"{song.Title} - {song.Artist}";
                PlayerHtml = _youtubeService.BuildPlayerHtml(streamUrl);
            }
            catch (Exception ex)
            {
                NowPlaying = $"Gagal memutar: {ex.Message}";
            }
        }

        public void Stop()
        {
            NowPlaying = "waiting";
            PlayerHtml = null;
            IsSessionActive = false;
        }
    }
}
