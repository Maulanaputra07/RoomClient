using RoomClient.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.Core.Interfaces
{
    public interface IApiService
    {
        Task<IReadOnlyList<RoomClient.Core.Models.Room>> GetRoomsAsync(CancellationToken cancellationToken = default);
        Task<bool> RegisterClientAsync(RegisterClientRequest request);
    }
}
