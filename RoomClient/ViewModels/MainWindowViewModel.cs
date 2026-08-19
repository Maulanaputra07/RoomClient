using CommunityToolkit.Mvvm.ComponentModel;
using RoomClient.Core.Interfaces;
using RoomClient.Core.Models;
using System.Windows;

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
            CategoryViewModel categoryViewModel,
            ISignalRService signalRService)
        {
            Search = searchViewModel;
            Player = playerViewModel;
            SongList = songListViewModel;
            Queue = queueViewModel;
            Status = statusViewModel;
            Category = categoryViewModel;
            _signalRService = signalRService;

            Search.Results = SongList.Results;
            Search.Player = Player;
            SongList.Player = Player;
            SongList.Queue = Queue;
            Category.SongList = SongList;
            Queue.Player = Player;

            _signalRService.SessionStarted += OnSessionStarted;
            _signalRService.SessionExpired += OnSessionExpired;
            _signalRService.SessionExtended += OnSessionExtended;
            _signalRService.CurrentRoomReceived += OnCurrentRoomReceived;

            Player.SessionFullyExpired += OnPlayerSessionFullyExpired;
        }

        public SearchViewModel Search { get; }

        public PlayerViewModel Player { get; }

        public SongListViewModel SongList { get; }

        public QueueViewModel Queue { get; }

        public StatusViewModel Status { get; }

        public CategoryViewModel Category { get; }

        public async Task InitializeAsync()
        {
            await Status.LoadRoomsAsync();

            _signalRService.ConnectionStateChanged += connected =>
            {
                // event Socket.IO datang dari background thread, marshal ke UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Status.IsWebSocketConnecting = !connected && _signalRService.IsReconnecting;
                    Status.IsWebSocketReady = connected;
                });
            };

            var connected = await _signalRService.ConnectAsync();
            Status.MarkWebSocketEngineReady(connected);
        }

        private void OnSessionExpired(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Player.IsSessionActive)
                {
                    // Kalau sedang ada lagu diputar, ExpireSession akan menunggu lagu selesai dulu (grace period).
                    // Kalau tidak ada lagu diputar, ExpireSession akan langsung finalize.
                    Player.ExpireSession();
                }
                else
                {
                    // Aplikasi baru buka & belum ada sesi — bersihkan state awal tanpa tampilkan "expired"
                    Player.IsSessionActive = false;
                    Player.IsSessionExpired = false;
                    SongList.Results.Clear();
                    Queue.Items.Clear();
                    Search.Reset();
                }
            });
        }

        private void OnPlayerSessionFullyExpired(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                SongList.Results.Clear();
                Queue.Items.Clear();
                Search.Reset();
            });
        }

        private void OnSessionStarted(object? sender, SessionStartedPayload data)
        {
            Application.Current.Dispatcher.Invoke(() => Player.ActivateSession(data.EndTime));
        }

        private void OnSessionExtended(object? sender, SessionStartedPayload data)
        {
            Application.Current.Dispatcher.Invoke(() => Player.ExtendSession(data.EndTime));
        }

        private void OnCurrentRoomReceived(object? sender, CurrentRoomPayload data)
        {
            Application.Current.Dispatcher.Invoke(() => Status.UpdateCurrentRoom(data));
        }
    }
}
