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
        public QueueViewModel? Queue { get; set; }

        [RelayCommand]
        private async Task PlayAsync(Song? song)
        {
            if (song is null || Player is null)
            {
                return;
            }

            //if (!Player.IsSessionActive)
            //{
            //    return;
            //}

            if (song.VideoId == Player.CurrentSong?.VideoId)
            {
                // Lagu yang sama sedang aktif -> toggle pause/resume, bukan restart
                await Player.TogglePlayPauseCommand.ExecuteAsync(null);
            }
            else
            {
                // Lagu berbeda -> mainkan dari awal
                await Player.PlayAsync(song);
                Player.IsFullScreen = true;
            }
        }

        [RelayCommand]
        private void AddToQueue(Song? song)
        {
            if (song is null || Queue is null)
            {
                return;
            }

            Queue.Add(song);
        }
    }
}
