using System;
using System.Text.Json.Serialization;

namespace RoomClient.Core.Models
{
    public class Room
    {
        [JsonPropertyName("room_id")]
        public int RoomId { get; set; }

        [JsonPropertyName("room_number")]
        public string RoomNumber { get; set; } = string.Empty;

        [JsonPropertyName("client_ip")]
        public string ClientIp { get; set; } = string.Empty;

        [JsonPropertyName("room_status")]
        public string RoomStatus { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
