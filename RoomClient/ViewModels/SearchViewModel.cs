using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoomClient.Core.Interfaces;
using RoomClient.Core.Models;
using System.Collections.ObjectModel;

namespace RoomClient.ViewModels
{
    public partial class SearchViewModel : ObservableObject
    {
        private readonly IYoutubeService _youtubeService;
        private string _searchQuery = "bruno mars";
        private bool _isBusy;
        private string _statusMessage = "Ready — using dummy YouTube data for local test.";

        public SearchViewModel(IYoutubeService youtubeService)
        {
            _youtubeService = youtubeService;
        }

        public ObservableCollection<Song> Results { get; set; } = new();

        public PlayerViewModel? Player { get; set; }

        public string SearchQuery
        {
            get => _searchQuery;
            set => SetProperty(ref _searchQuery, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            StatusMessage = $"Loading dummy results for '{SearchQuery}'...";

            try
            {
                var results = await _youtubeService.SearchAsync(SearchQuery);

                Results.Clear();
                foreach (var song in results)
                {
                    Results.Add(song);
                }

                StatusMessage = Results.Count > 0
                    ? $"Loaded {Results.Count} dummy result(s)."
                    : "No dummy results found.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Search failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task TestBrunoMarsAsync()
        {
            SearchQuery = "bruno mars";
            await SearchAsync();

            if (Results.Count > 0 && Player is not null)
            {
                Player.Play(Results[0]);
                StatusMessage = $"Playing {Results[0].Title} in WebView2.";
            }
        }
    }
}
