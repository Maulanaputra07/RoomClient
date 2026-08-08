using System.Diagnostics;
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
using RoomClient.Config;

namespace RoomClient.ViewModels
{
    public enum SearchDisplayState
    {
        Initial,
        Loading,
        Empty,
        HasResults
    }

    public partial class SearchViewModel : ObservableObject
    {
        private readonly IYoutubeService _youtubeService;

#if VOICE_SEARCH
        private readonly IVoiceSearchService _voiceSearchService;
#endif

        public bool IsVoiceSearchEnabled => FeatureFlags.VoiceSearchEnabled;

        private string _searchQuery;
        private bool _isBusy;
        private bool _isListening;
        private bool _hasSearched;
        private string _statusMessage = "Ready";

#if VOICE_SEARCH
        public SearchViewModel(
            IYoutubeService youtubeService,
            IVoiceSearchService voiceSearchService)
        {
            _youtubeService = youtubeService;
            _voiceSearchService = voiceSearchService;
            Results.CollectionChanged += (s, e) => OnPropertyChanged(nameof(DisplayState));
        }
#else
        public SearchViewModel(
            IYoutubeService youtubeService)
        {
            _youtubeService = youtubeService;
            Results.CollectionChanged += (s, e) => OnPropertyChanged(nameof(DisplayState));
        }
#endif
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
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    _hasSearched = false;
                    OnPropertyChanged(nameof(DisplayState));
                }
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                    OnPropertyChanged(nameof(DisplayState));
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public SearchDisplayState DisplayState
        {
            get
            {
                if (IsBusy) return SearchDisplayState.Loading;
                if (string.IsNullOrWhiteSpace(SearchQuery) || !_hasSearched) return SearchDisplayState.Initial;
                return Results.Count == 0 ? SearchDisplayState.Empty : SearchDisplayState.HasResults;
            }
        }

        public void Reset()
        {
            SearchQuery = string.Empty;
            _hasSearched = false;
            StatusMessage = "Ready.";
            OnPropertyChanged(nameof(DisplayState));
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
                _hasSearched = true;
                StatusMessage = Results.Count > 0
                ? $"Menemukan {Results.Count} hasil pencarian."
                : "Tidak menemukan hasil.";
            }
            catch (Exception ex)
            {
                _hasSearched = true;
                StatusMessage = $"Pencarian gagal: {ex.Message}";
            }
            finally
            {
                OnPropertyChanged(nameof(DisplayState));
            }  
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

            try
            {
                await ExecuteSearchAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

#if VOICE_SEARCH
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
#endif


        [RelayCommand]
        private void ToggleFullScreen()
        {
            if (Player is null) return;
            Player.IsFullScreen = !Player.IsFullScreen;
        }
    }
}
