using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoomClient.Core.Interfaces;
using RoomClient.Core.Models;
using RoomClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.ViewModels
{
    public partial class CategoryViewModel : ObservableObject
    {
        private readonly IYoutubeService _youtubeService;
        public SongListViewModel? SongList { get; set; } // di-wire oleh MainWindowViewModel, sama seperti pola lain
        public SearchViewModel? Search { get; set; }

        public ObservableCollection<CategoryItem> Categories { get; } = new()
        {
            new CategoryItem { Name = "Semua Lagu", Slug = string.Empty, Icon = "\uE71D" },
            new CategoryItem { Name = "Pop Indonesia", Slug = "pop-indonesia", Icon = "\uE8D6" },
            new CategoryItem { Name = "Dangdut", Slug = "dangdut", Icon = "\uE8D6" },
            new CategoryItem { Name = "Pop Barat", Slug = "pop-barat", Icon = "\uE8D6" },
            new CategoryItem { Name = "Mandarin", Slug = "mandarin", Icon = "\uE8D6" },
            new CategoryItem { Name = "Korea (K-Pop)", Slug = "kpop", Icon = "\uE8D6" },
            new CategoryItem { Name = "Rohani", Slug = "rohani", Icon = "\uE8D6" },
            new CategoryItem { Name = "Anak-anak", Slug = "anak-anak", Icon = "\uE8D6" },
            new CategoryItem { Name = "Lawas / Nostalgia", Slug = "lawas-nostalgia", Icon = "\uE8D6" },
            new CategoryItem { Name = "Jazz & Akustik", Slug = "jazz-akustik", Icon = "\uE8D6" },
            new CategoryItem { Name = "Dj Remix", Slug = "dj-remix", Icon = "\uE8D6" },
        };

        [ObservableProperty]
        private CategoryItem? _selectedCategory;

        [ObservableProperty]
        private bool _isLoading;

        public CategoryViewModel(IYoutubeService youtubeService)
        {
            _youtubeService = youtubeService;
        }

        [RelayCommand]
        private async Task SelectCategoryAsync(CategoryItem category)
        {
            SelectedCategory = category;

            if (SongList is null)
            {
                return;
            }

            SongList.Results.Clear();
            if (Search is not null)
            {
                Search.StatusMessage = $"Memuat kategori '{category.Name}'...";
            }

            if (string.IsNullOrWhiteSpace(category.Slug))
            {
                if (Search is not null)
                {
                    Search.StatusMessage = "Mode semua lagu aktif. Gunakan pencarian untuk menampilkan hasil.";
                    Search.RefreshDisplayState();
                }
                return;
            }

            IsLoading = true;
            try
            {
                var results = await _youtubeService.GetByCategoryAsync(category.Slug);
                System.Diagnostics.Debug.WriteLine($"[CATEGORY] Slug='{category.Slug}' -> {results.Count} result(s)");
                foreach (var song in results)
                {
                    SongList.Results.Add(song);
                }
                if (Search is not null)
                {
                    Search.StatusMessage = results.Count > 0
                        ? $"Menampilkan {results.Count} lagu dari kategori '{category.Name}'."
                        : $"Tidak ada lagu pada kategori '{category.Name}'.";
                    Search.RefreshDisplayState();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Gagal load kategori {category.Slug}: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
