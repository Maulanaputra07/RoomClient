using CommunityToolkit.Mvvm.ComponentModel;
using RoomClient.Core.Interfaces;
using RoomClient.Core.Models;

namespace RoomClient.ViewModels
{
    public class PlayerViewModel : ObservableObject
    {
        private readonly IYoutubeService _youtubeService;
        private string _nowPlaying = "waiting";
        private string? _playerHtml;

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

        public void Play(Song song)
        {
            if (string.IsNullOrWhiteSpace(song.VideoId))
            {
                return;
            }

            NowPlaying = string.IsNullOrWhiteSpace(song.Artist)
                ? song.Title
                : $"{song.Title} - {song.Artist}";

            PlayerHtml = _youtubeService.BuildPlayerHtml(song);
        }
    }
}
