using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.Core.Interfaces
{
    public interface IMicrophoneService
    {
        Task<string> RecordAsync(
        int durationSeconds = 5,
        CancellationToken cancellationToken = default);

        Task<string> RecordUntilSilenceAsync(
        CancellationToken cancellationToken = default);
    }
}
