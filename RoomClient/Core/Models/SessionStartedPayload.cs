using System.Text.Json.Serialization;

namespace RoomClient.Core.Models
{
    public class SessionStartedPayload
    {
        [JsonPropertyName("access")]
        public string Access { get; set; } = string.Empty;

        [JsonPropertyName("session_id")]
        public int SessionId { get; set; }

        [JsonPropertyName("room_id")]
        public int RoomId { get; set; }

        [JsonPropertyName("duration_minutes")]
        public int DurationMinutes { get; set; }

        [JsonPropertyName("start_time")]
        public DateTimeOffset StartTime { get; set; }

        [JsonPropertyName("end_time")]
        public DateTimeOffset EndTime { get; set; }
    }
}