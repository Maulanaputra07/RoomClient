using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.Core.Interfaces
{
    public interface IVoiceSearchService
    {
        Task<String?> ListenAsync(
            CancellationToken cancellationToken = default
        );
    }
}
