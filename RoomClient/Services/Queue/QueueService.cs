using RoomClient.Core.Interfaces;
using RoomClient.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.Services.Queue
{
    public class QueueService : IQueueService
    {
        private readonly List<QueueSong> _queue = new();

        public void Enqueue(Song song)
        {
            _queue.Add(new QueueSong { Song = song });
        }

        public void Remove(Song song)
        {
            _queue.RemoveAll(item =>
                ReferenceEquals(item.Song, song) ||
                (item.Song.VideoId == song.VideoId &&
                 item.Song.Title == song.Title &&
                 item.Song.Artist == song.Artist));
        }

        public IReadOnlyList<QueueSong> GetQueue()
        {
            return _queue.AsReadOnly();
        }
    }
}
