using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RoomClient.Core.Models
{
    public class YoutubeStreamResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public YoutubeStreamData? Data { get; set; }
    }

    public class YoutubeStreamData
    {
        [JsonPropertyName("videoId")]
        public required string VideoId { get; set; }

        [JsonPropertyName("title")]
        public required string Title { get; set; }

        [JsonPropertyName("duration")]
        public int DurationSeconds { get; set; }

        [JsonPropertyName("thumbnail")]
        public string? Thumbnail { get; set; }

        [JsonPropertyName("streamUrl")]
        public required string StreamUrl { get; set; }
    }
}
