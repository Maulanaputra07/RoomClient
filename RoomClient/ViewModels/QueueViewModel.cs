using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using RoomClient.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.ViewModels
{
    public partial class QueueViewModel : ObservableObject
    {
        public ObservableCollection<QueueSong> Items { get; } = new();

        public PlayerViewModel? Player { get; set; }

        public void Add(Song song, string requestedBy = "Guest")
        {
            Items.Add(new QueueSong
            {
                Song = song,
                RequestedBy = requestedBy,
                RequestedAt = DateTime.Now
            });
        }

        [RelayCommand]
        private async Task PlayAsync(QueueSong? queueSong)
        {
            if (queueSong is null || Player is null)
            {
                return;
            }

            //if (!Player.IsSessionActive)
            //{
            //    return;
            //}

            await Player.PlayAsync(queueSong.Song);
            Items.Remove(queueSong);
        }

        [RelayCommand]
        private void Remove(QueueSong? queueSong)
        {
            if (queueSong is null)
            {
                return;
            }

            Items.Remove(queueSong);
        }
    
    }
}
