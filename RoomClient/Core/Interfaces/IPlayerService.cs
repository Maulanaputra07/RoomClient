using RoomClient.Core.Models;
using RoomClient.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        event EventHandler<string>? JavaScriptCommandRequested;

        void UpdatePlaybackStateFromWebView(PlaybackState state);

        Task PlayAsync(Song song);
        Task ResumeAsync();
        Task PauseAsync();
        Task StopAsync();
        Task SeekAsync(TimeSpan position);
        Task NextAsync();
        Task PreviousAsync();
        Task SetVolumeAsync(double volume);
        string GetApplyVolumeScript();
    }
}
