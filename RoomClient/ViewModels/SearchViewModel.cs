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
        private readonly IVoiceSearchService _voiceSearchService;

        private string _searchQuery;
        private bool _isBusy;
        private bool _isListening;
        private string _statusMessage = "Ready";

        public SearchViewModel(
            IYoutubeService youtubeService,
            IVoiceSearchService voiceSearchService)
        {
            _youtubeService = youtubeService;
            _voiceSearchService = voiceSearchService;
        }

        public ObservableCollection<Song> Results { get; set; } = new();

        public PlayerViewModel? Player { get; set; }

        public bool IsListening
        {
            get => _isListening;
            set => SetProperty(ref _isListening, value);
        }

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

        private async Task ExecuteSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                StatusMessage = "Masukkan kata kunci pencarian.";
                return;
            }

            StatusMessage = $"Mencari hasil untuk '{SearchQuery}'...";

            try
            {
                var results = await _youtubeService.SearchAsync(SearchQuery);
                Results.Clear();
                foreach (var song in results)
                {
                    Results.Add(song);
                }
                StatusMessage = Results.Count > 0
                ? $"Menemukan {Results.Count} hasil pencarian."
                : "Tidak menemukan hasil.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Pencarian gagal: {ex.Message}";
            }   
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            if (Player is null)
            {
                StatusMessage = "Sesi belum dimulai — tidak dapat mencari lagu.";
                Results.Clear();
                return;
            }

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

            try
            {
                await ExecuteSearchAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SearchByVoiceAsync()
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
            IsBusy = true;
            IsListening = true;
            try
            {
                StatusMessage = "Mendegarkan pesan suara...";
                var query = await _voiceSearchService.ListenAsync();
                IsListening = false;
                if (string.IsNullOrWhiteSpace(query))
                {
                    StatusMessage = "Tidak ada suara atau kata kunci tidak ditemukan.";
                    return;
                }
                SearchQuery = query;
                await ExecuteSearchAsync();
            }
            catch(OperationCanceledException)
            {
                StatusMessage = "Voice search dibatalkan.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Voice search failed: {ex.Message}";
            }
            finally
            {
                IsListening = false;
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
