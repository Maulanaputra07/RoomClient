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
        public event EventHandler? SessionExpired;
        public event EventHandler<SessionStartedPayload>? SessionStarted;
        public event EventHandler<SessionStartedPayload>? SessionExtended;
        public event EventHandler<CurrentRoomPayload>? CurrentRoomReceived;

        private static readonly Uri ServerUri = new("http://100.114.192.55:3000");
        private SocketIOClient.SocketIO? _client;
        private readonly IConfigService _configService;

        public bool IsConnected => _client?.Connected ?? false;

        public SignalRService(IConfigService configService)
        {
            _configService = configService;
        }

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

            _client.OnConnected += async (s, e) =>
            {
                SocketLogger.Log("LIFECYCLE", $"OnConnected fired. Id={_client!.Id}");
                try
                {
                    var config = _configService.LoadCreate();

                    await _client.EmitAsync("register_device", new object[] { new { device_id = config.DeviceId } });
                    SocketLogger.Log("EMIT", $"register_device sent: {config.DeviceId}");

                    await _client.EmitAsync("current_room");
                    SocketLogger.Log("EMIT", "current_room sent");

                    await _client.EmitAsync("request_sync");
                    SocketLogger.Log("EMIT", "request_sync sent");
                }
                catch (Exception ex)
                {
                    SocketLogger.Log("EMIT-ERROR", $"request_sync failed: {ex.Message}");
                }
            };

            _client.OnDisconnected += (s, reason) =>
                SocketLogger.Log("LIFECYCLE", $"OnDisconnected. Reason={reason}");

            _client.OnReconnectAttempt += (s, attempt) =>
                SocketLogger.Log("LIFECYCLE", $"Reconnect attempt #{attempt}");

            _client.OnReconnectError += (s, ex) =>
                SocketLogger.Log("LIFECYCLE", $"Reconnect error: {ex.Message}");

            _client.OnError += (s, err) =>
                SocketLogger.Log("ERROR", err);

            _client.OnAny(async (eventName, ctx) =>
            {
                SocketLogger.Log("RAW-EVENT", $"{eventName} => {ctx.RawText}");
                await Task.CompletedTask;
            });

           

            _client.On("session_started", async ctx =>
            {
                var data = ctx.GetValue<SessionStartedPayload>(0);
                SocketLogger.LogEvent("session_started", data);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    SessionStarted?.Invoke(this, data);
                });

                await Task.CompletedTask;
            });

            _client.On("session_expired", async ctx =>
            {
                SocketLogger.LogEvent("session_expired", null);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    SessionExpired?.Invoke(this, EventArgs.Empty);
                });

                await Task.CompletedTask;
            });

            _client.On("session_extended", async ctx =>
            {
                var data = ctx.GetValue<SessionStartedPayload>(0);
                SocketLogger.LogEvent("session_extended", data);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    SessionExtended?.Invoke(this, data);
                });

                await Task.CompletedTask;
            });

            _client.On("current_room", async ctx =>
            {
                var data = ctx.GetValue<CurrentRoomPayload>(0);
                SocketLogger.LogEvent("current_room", data);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentRoomReceived?.Invoke(this, data);
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
