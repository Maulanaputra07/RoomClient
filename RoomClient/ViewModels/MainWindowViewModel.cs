using CommunityToolkit.Mvvm.ComponentModel;
using RoomClient.Core.Interfaces;
using RoomClient.Core.Models;

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
            SongList.Player = Player;
            SongList.Queue = Queue;
            Queue.Player = Player;

            _signalRService.SessionStarted += OnSessionStarted;
            _signalRService.SessionExpired += OnSessionExpired;
            _signalRService.SessionExtended += OnSessionExtended;
            _signalRService.CurrentRoomReceived += OnCurrentRoomReceived;
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
            Player.StopAsync();
            SongList.Results.Clear();
            Queue.Items.Clear();
            Search.Reset();
        }

        private void OnSessionStarted(object? sender, SessionStartedPayload data)
        {
            Player.ActivateSession(data.EndTime);
        }

        private void OnSessionExtended(object? sender, SessionStartedPayload data)
        {
            Player.ExtendSession(data.EndTime);
        }

        private void OnCurrentRoomReceived(object? sender, CurrentRoomPayload data)
        {
            Status.UpdateCurrentRoom(data);
        }
    }
}
