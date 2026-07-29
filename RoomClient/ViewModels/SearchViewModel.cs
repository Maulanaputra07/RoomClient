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
        private string _searchQuery;
        private bool _isBusy;
        private string _statusMessage = "Ready";

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

        public void Reset()
        {
            SearchQuery = string.Empty;
            StatusMessage = "Ready.";
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            if (Player is null || !Player.IsSessionActive)
            {
                StatusMessage = "Sesi belum dimulai — tidak dapat mencari lagu.";
                Results.Clear();
                return;
            }

            if (IsBusy)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                StatusMessage = "Masukkan kata kunci pencarian.";
                return;
            }

            IsBusy = true;
            StatusMessage = $"Loading results for '{SearchQuery}'...";

            try
            {
                var results = await _youtubeService.SearchAsync(SearchQuery);

                Results.Clear();

                foreach (var song in results)
                {
                    Results.Add(song);
                }

                StatusMessage = Results.Count > 0
                    ? $"Loaded {Results.Count} result(s)."
                    : "No results found.";
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
        private void ToggleFullScreen()
        {
            if (Player is null) return;
            Player.IsFullScreen = !Player.IsFullScreen;
        }
    }
}
