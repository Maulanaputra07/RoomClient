using RoomClient.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.Core.Interfaces
{
    public interface IPlayerService
    {
        Task PlayAsync(Song song);

        Task PauseAsync();

        Task StopAsync();
    }
}
