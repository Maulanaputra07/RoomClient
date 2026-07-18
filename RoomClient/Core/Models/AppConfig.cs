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
    }
}
