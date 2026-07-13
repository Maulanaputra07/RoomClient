using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.Core.Models
{
    public class QueueSong
    {
        public required Song Song { get; set; }
        public string RequestedBy { get; set; } = "";
        public DateTime RequestedAt { get; set; } = DateTime.Now;
    }
}
