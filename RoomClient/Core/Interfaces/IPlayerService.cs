using RoomClient.Core.Models;
using System;
using System.Threading.Tasks;

namespace RoomClient.Core.Interfaces
{
    public enum PlaybackState
    {
        Stopped,
        Playing,
        Paused,
        Buffering
    }

    public interface IPlayerService
    {
        Song? CurrentSong { get; }
        PlaybackState State { get; }
        TimeSpan CurrentPosition { get; }
        TimeSpan Duration { get; }

        event EventHandler<Song?>? CurrentSongChanged;
        event EventHandler<PlaybackState>? PlaybackStateChanged;
        event EventHandler<TimeSpan>? PositionChanged;
        event EventHandler<VlcCommand>? VlcCommandRequested;

        void UpdatePlaybackState(PlaybackState state);

        Task PlayAsync(Song song);
        Task ResumeAsync();
        Task PauseAsync();
        Task StopAsync();
        Task SeekAsync(TimeSpan position);
        Task NextAsync();
        Task PreviousAsync();
        Task SetVolumeAsync(double volume);
    }
}
