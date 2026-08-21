using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RoomClient.Core.Models
{
    public class YoutubeSearchResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public List<YoutubeSearchItem> Data { get; set; } = [];
    }

    public class YoutubeSearchItem
    {
        [JsonPropertyName("videoId")]
        public string? VideoId { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("channel")]
        public string? Channel { get; set; }

        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        [JsonPropertyName("duration")]
        public int DurationSeconds { get; set; }

        [JsonPropertyName("viewCount")]
        public long ViewCount { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; } = "youtube";

        [JsonPropertyName("streamUrl")]
        public string? StreamUrl { get; set; }
    }
}
