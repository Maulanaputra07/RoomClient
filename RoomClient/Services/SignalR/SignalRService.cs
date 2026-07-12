using RoomClient.Core.Interfaces;
using RoomClient.Core.Models;
using RoomClient.Services.Logging;
using SocketIOClient;
using SocketIOClient.Common;
using System.Windows;

namespace RoomClient.Services.SignalR
{
    public class SignalRService : ISignalRService
    {
        private static readonly Uri ServerUri = new("http://192.168.201.220:3000");
        private SocketIOClient.SocketIO? _client;

        public bool IsConnected => _client?.Connected ?? false;

        //public SignalRService()
        //{
        //    _socket = new SocketIO(ServerUri);
        //}

        public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (IsConnected) return true;

            _client = new SocketIOClient.SocketIO(ServerUri, new SocketIOOptions
            {
                Transport = TransportProtocol.WebSocket,
                Reconnection = true,
                ReconnectionAttempts = 50,
                ConnectionTimeout = TimeSpan.FromSeconds(10),
            });

            SetupListeners();

            try
            {
                SocketLogger.Log("CONNECT", $"Attempting connect to {ServerUri}...");
                await _client.ConnectAsync(cancellationToken);
                SocketLogger.Log("CONNECT", $"Connected. SocketId={_client.Id}");
                return true;
            }
            catch (Exception ex)
            {
                SocketLogger.Log("CONNECT-ERROR", ex.Message);
                MessageBox.Show(
                    $"Socket.IO connection failed: {ex.Message}",
                    "RoomClient Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }
        }

        private void SetupListeners()
        {
            if (_client == null) return;

            // --- Lifecycle events (signature berubah di v4) ---

            _client.OnConnected += (s, e) =>
                SocketLogger.Log("LIFECYCLE", $"OnConnected fired. Id={_client!.Id}");

            _client.OnDisconnected += (s, reason) =>
                SocketLogger.Log("LIFECYCLE", $"OnDisconnected. Reason={reason}");

            _client.OnReconnectAttempt += (s, attempt) =>
                SocketLogger.Log("LIFECYCLE", $"Reconnect attempt #{attempt}");

            _client.OnReconnectError += (s, ex) =>
                SocketLogger.Log("LIFECYCLE", $"Reconnect error: {ex.Message}");

            _client.OnError += (s, err) =>
                SocketLogger.Log("ERROR", err);

            // Catch-all semua event dari server (handler sekarang async di v4)
            _client.OnAny(async (eventName, ctx) =>
            {
                SocketLogger.Log("RAW-EVENT", $"{eventName} => {ctx.RawText}");
                await Task.CompletedTask;
            });

            // --- Event spesifik ---

            _client.On("session_started", async ctx =>
            {
                var data = ctx.GetValue<SessionStartedPayload>(0);
                SocketLogger.LogEvent("session_started", data);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        $"Session Started! Room: {data.RoomId}, Duration: {data.DurationMinutes}m",
                        "RoomClient",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Logic buka layar aplikasi ada di sini
                });

                await Task.CompletedTask;
            });

            _client.On("session_expired", async ctx =>
            {
                SocketLogger.LogEvent("session_expired", null);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        "Waktu Habis! Layar akan dikunci.",
                        "RoomClient",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    // Logic kunci layar aplikasi ada di sini
                });

                await Task.CompletedTask;
            });
        }

        public async Task<bool> SendSessionStartedAsync(
            SessionStartedPayload payload,
            CancellationToken cancellationToken = default)
        {
            if (!IsConnected && !await ConnectAsync(cancellationToken))
            {
                SocketLogger.Log("EMIT-FAIL", "SendSessionStartedAsync aborted: not connected");
                return false;
            }

            try
            {
                SocketLogger.Log("EMIT", $"session_started => {System.Text.Json.JsonSerializer.Serialize(payload)}");
                await _client!.EmitAsync("session_started", new object[]
                {
                    new
                    {
                        access = payload.Access,
                        session_id = payload.SessionId,
                        room_id = payload.RoomId,
                        duration_minutes = payload.DurationMinutes,
                        start_time = payload.StartTime,
                        end_time = payload.EndTime
                    }
                }, cancellationToken);

                SocketLogger.Log("EMIT-ACK", "session_started sent successfully");

                MessageBox.Show(
                    "Socket.IO session_started: GRANTED",
                    "RoomClient Socket.IO",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return true;
            }
            catch (Exception ex)
            {
                SocketLogger.Log("EMIT-ERROR", ex.Message);
                MessageBox.Show(
                    "Socket.IO session_started: DENIED",
                    "RoomClient Socket.IO",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return false;
            }
        }

        public async Task DisposeSocketAsync()
        {
            if (_client != null)
            {
                SocketLogger.Log("LIFECYCLE", "Disposing socket connection");
                await _client.DisconnectAsync();
                _client.Dispose();
                _client = null;
            }
        }
    }
}
