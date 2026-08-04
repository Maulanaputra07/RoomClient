using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.Core.Models
{
    public class WhisperOptions
    {
        public string ModelPath { get; set; } = string.Empty;
        public string Language { get; set; } = "id";
        public int SampleRate { get; set; } = 16000;
    }
}
