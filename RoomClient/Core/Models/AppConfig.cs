using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RoomClient.Core.Models
{
    public class AppConfig
    {
        [JsonPropertyName("device_id")]
        public required string DeviceId { get; set; }

        [JsonPropertyName("is_registered")]
        public bool isRegistered { get; set; }

        [JsonPropertyName("api_settings")]
        public ApiSettings ApiSettings { get; set; } = new();
    }

    public class ApiSettings
    {
        [JsonPropertyName("server_api")]
        public string ServerAPI { get; set; } = "http://192.168.137.161:3000/api/";

        [JsonPropertyName("youtube_api")]
        public string YoutubeAPI { get; set; } = "http://127.0.0.1:8000/";

        [JsonPropertyName("websocket")]
        public string WebSocket { get; set; } = "http://192.168.137.161:3000";
    }
}
