using RoomClient.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.Core.Interfaces
{
    public interface ISignalRService
    {
        bool IsConnected { get; }
        bool IsReconnecting { get; }
        event EventHandler? SessionExpired;
        event EventHandler<SessionStartedPayload>? SessionStarted;
        event EventHandler<SessionStartedPayload>? SessionExtended;
        event EventHandler<CurrentRoomPayload>? CurrentRoomReceived;
        event Action<bool>? ConnectionStateChanged;

        Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

        Task<bool> SendSessionStartedAsync(
            RoomClient.Core.Models.SessionStartedPayload payload,
            CancellationToken cancellationToken = default);

        Task DisposeSocketAsync();

    }
}
