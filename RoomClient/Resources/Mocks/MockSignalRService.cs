using RoomClient.Core.Interfaces;
using RoomClient.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.Resources.Mocks
{
    public class MockSignalRService : ISignalRService
    {
        public event EventHandler<SessionStartedPayload>? SessionStarted;
        public event EventHandler? SessionExpired;
        public event EventHandler<SessionStartedPayload>? SessionExtended;
        public event EventHandler<CurrentRoomPayload>? CurrentRoomReceived;
        public bool IsConnected => true;

        public Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            // Bypass proses jaringan, langsung kembalikan sukses secara instan
            return Task.FromResult(true);
        }
        public Task<bool> SendSessionStartedAsync(SessionStartedPayload payload, CancellationToken cancellationToken = default)
        {
            // Bypass pengiriman data, asumsikan server menerimanya
            return Task.FromResult(true);
        }

        public Task DisposeSocketAsync()
        {
            return Task.CompletedTask;
        }

        // ==========================================================
        // ALAT DEBUGGING (Hanya ada di Mock)
        // Panggil method ini dari UI/ViewModel untuk mengetes behavior
        // tanpa perlu koneksi server asli.
        // ==========================================================

        public void SimulateSessionStarted()
        {
            var dummyPayload = new SessionStartedPayload
            {
                SessionId = 123,
                RoomId = 1,
                DurationMinutes = 60,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddHours(1)
            };
            SessionStarted?.Invoke(this, dummyPayload);
        }

        public void SimulateSessionExpired()
        {
            SessionExpired?.Invoke(this, EventArgs.Empty);
        }

        public void SimulateCurrentRoomReceived()
        {
            var dummyRoom = new CurrentRoomPayload
            {
                // Isi properti dummy sesuai model Anda
            };
            CurrentRoomReceived?.Invoke(this, dummyRoom);
        }
    }
}
