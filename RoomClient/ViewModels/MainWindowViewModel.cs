using CommunityToolkit.Mvvm.ComponentModel;
using RoomClient.Core.Interfaces;

namespace RoomClient.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        private readonly ISignalRService _signalRService;

        public MainWindowViewModel(
            SearchViewModel searchViewModel,
            PlayerViewModel playerViewModel,
            SongListViewModel songListViewModel,
            QueueViewModel queueViewModel,
            StatusViewModel statusViewModel,
            ISignalRService signalRService)
        {
            Search = searchViewModel;
            Player = playerViewModel;
            SongList = songListViewModel;
            Queue = queueViewModel;
            Status = statusViewModel;
            _signalRService = signalRService;

            Search.Results = SongList.Results;
            Search.Player = Player;

            _signalRService.SessionExpired += OnSessionExpired;
        }

        public SearchViewModel Search { get; }

        public PlayerViewModel Player { get; }

        public SongListViewModel SongList { get; }

        public QueueViewModel Queue { get; }

        public StatusViewModel Status { get; }

        public async Task InitializeAsync()
        {
            await Status.LoadRoomsAsync();

            var connected = await _signalRService.ConnectAsync();
            Status.MarkWebSocketEngineReady(connected);
        }

        private void OnSessionExpired(object? sender, EventArgs e)
        {
            Player.Stop();
        }
    }
}
