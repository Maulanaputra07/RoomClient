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
        public bool IsRegistered { get; set; }
    }

    public class ApiSettings
    {
        public string ServerAPI { get; set; } = string.Empty;

        public string YoutubeAPI { get; set; } = string.Empty;

        public string WebSocket { get; set; } = string.Empty;
    }
}
