using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RoomClient.Core.Models
{
    public class CurrentRoomPayload
    {
        [JsonPropertyName("room_id")]
        public int? RoomId { get; set; }

        [JsonPropertyName("room_number")]
        public string? RoomNumber { get; set; }

        [JsonPropertyName("room_status")]
        public string? RoomStatus { get; set; }
    }
}
