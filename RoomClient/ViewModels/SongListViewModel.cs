using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoomClient.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.ViewModels
{
    public partial class SongListViewModel : ObservableObject
    {
        public ObservableCollection<Song> Results { get; set; } = new();
        public PlayerViewModel? Player { get; set; }

        [RelayCommand]
        private async Task PlayAsync(Song? song)
        {
            if (song is null || Player is null)
            {
                return;
            }

            if (!Player.IsSessionActive)
            {
                // opsional: tampilkan status kalau ada StatusMessage di sini
                return;
            }

            await Player.PlayAsync(song);
        }
    }
}
