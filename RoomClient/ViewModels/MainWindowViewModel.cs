using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RoomClient.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        public MainWindowViewModel(
            SearchViewModel searchViewModel,
            PlayerViewModel playerViewModel,
            SongListViewModel songListViewModel,
            QueueViewModel queueViewModel,
            StatusViewModel statusViewModel)
        {
            Search = searchViewModel;
            Player = playerViewModel;
            SongList = songListViewModel;
            Queue = queueViewModel;
            Status = statusViewModel;

            Search.Results = SongList.Results;
            Search.Player = Player;
        }

        public SearchViewModel Search { get; }

        public PlayerViewModel Player { get; }

        public SongListViewModel SongList { get; }

        public QueueViewModel Queue { get; }

        public StatusViewModel Status { get; }
    }
}
