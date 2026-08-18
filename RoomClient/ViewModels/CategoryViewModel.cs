using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoomClient.Core.Interfaces;
using RoomClient.Core.Models;
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

        public ObservableCollection<CategoryItem> Categories { get; } = new()
        {
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
            IsLoading = true;
            try
            {
                var results = await _youtubeService.GetByCategoryAsync(category.Slug); // DIUBAH dari SearchAsync
                if (SongList is not null)
                {
                    SongList.Results = new ObservableCollection<Song>(results);
                }
            }
            catch (Exception ex)
            {
                // TAMBAHAN: tampilkan error ke user, misal via NowPlaying atau status text
                System.Diagnostics.Debug.WriteLine($"Gagal load kategori {category.Slug}: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
