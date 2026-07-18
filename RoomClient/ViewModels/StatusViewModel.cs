using CommunityToolkit.Mvvm.ComponentModel;
using RoomClient.Core.Interfaces;
using RoomClient.Core.Models;
using System.Collections.ObjectModel;

namespace RoomClient.ViewModels
{
    public class StatusViewModel : ObservableObject
    {
        private readonly IApiService _apiService;
        private bool _isLoading;
        private string _statusMessage = "Room data not loaded yet.";
        private string? _errorMessage;
        private Room? _selectedRoom;

        public StatusViewModel(IApiService apiService)
        {
            _apiService = apiService;
        }

        public ObservableCollection<Room> Rooms { get; } = new();

        public ObservableCollection<string> EventLogs { get; } = new();

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public Room? SelectedRoom
        {
            get => _selectedRoom;
            private set => SetProperty(ref _selectedRoom, value);
        }

        public async Task LoadRoomsAsync(CancellationToken cancellationToken = default)
        {
            if (IsLoading)
            {
                return;
            }

            IsLoading = true;
            ErrorMessage = null;
            StatusMessage = "Fetching rooms from local API...";

            try
            {
                var rooms = await _apiService.GetRoomsAsync(cancellationToken);

                Rooms.Clear();

                foreach (var room in rooms)
                {
                    Rooms.Add(room);
                }

                SelectedRoom = Rooms.FirstOrDefault();
                StatusMessage = Rooms.Count > 0
                    ? $"Loaded {Rooms.Count} room(s) from local API."
                    : "No rooms returned by local API.";

                AddLog(StatusMessage);
            }
            catch (Exception ex)
            {
                Rooms.Clear();
                SelectedRoom = null;
                ErrorMessage = ex.Message;
                StatusMessage = "Failed to load rooms from local API.";
                AddLog(StatusMessage);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void UpdateCurrentRoom(CurrentRoomPayload payload)
        {
            if (payload.RoomId is null)
            {
                SelectedRoom = null;
                StatusMessage = "Belum ada room aktif untuk device ini.";
                AddLog(StatusMessage);
                return;
            }

            SelectedRoom = new Room
            {
                RoomId = payload.RoomId.Value,
                RoomNumber = payload.RoomNumber ?? "",
                RoomStatus = payload.RoomStatus ?? ""
            };

            StatusMessage = $"Room {SelectedRoom.RoomNumber} aktif.";
            AddLog(StatusMessage);
        }

        public void MarkWebSocketEngineReady(bool connected)
        {
            AddLog(connected
                ? "WebSocket engine ready on app launch."
                : "WebSocket engine failed to start on app launch.");
        }

        public void MarkWebSocketSessionOutcome(bool granted)
        {
            AddLog(granted
                ? "WebSocket session_started sent: GRANTED."
                : "WebSocket session_started sent: DENIED.");
        }

        public void AddLog(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            EventLogs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
        }
    }
}
