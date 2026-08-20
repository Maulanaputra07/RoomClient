using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.Core.Models
{
    public enum VlcCommandType
    {
        Play,
        Pause,
        Resume,
        Stop,
        Seek,
        SetVolume
    }
    public class VlcCommand
    {
        public VlcCommandType Type { get; init; }
        public string? Source { get; init; }
        public double? Volume { get; init; }
        public TimeSpan? Position { get; init; }

    }
}
