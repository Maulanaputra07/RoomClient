using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.Core.Models
{
    public class Song
    {
        public string Title { get; set; } = "";

        public string Artist { get; set; } = "";

        public string VideoId { get; set; } = "";

        public string Thumbnail { get; set; } = "";

        public TimeSpan? Duration { get; set; }

        public string DurationDisplay => Duration.HasValue
            ? Duration.Value.ToString(@"hh\:mm\:ss")
            : "-";
    }
}
