using CommunityToolkit.Mvvm.ComponentModel;
using RoomClient.Core.Models;
using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.ViewModels
{
    public class SongListViewModel : ObservableObject
    {
        public ObservableCollection<Song> Results { get; } = new();
    }
}
