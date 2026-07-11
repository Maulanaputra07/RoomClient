using RoomClient.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.Core.Interfaces
{
    public interface IQueueService
    {
        void Enqueue(Song song);

        void Remove(Song song);

        IReadOnlyList<QueueSong> GetQueue();
    }
}
