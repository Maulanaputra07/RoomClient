using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RoomClient.Core.Models
{
    public class RegisterClientRequest
    {
        [JsonPropertyName("device_ip")]
        public required string DeviceIp { get; set; }

        [JsonPropertyName("device_id")]
        public required string DeviceId { get; set; }

        [JsonPropertyName("hostname")]
        public required string Hostname { get; set; }
    }
}
