using RoomClient.Core.Interfaces;
using RoomClient.Core.Models;
using RoomClient.Helpers;
using RoomClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.Services.Player
{
    public class PlayerService : IPlayerService
    {
        private Song? _currentSong;
        private PlaybackState _state = PlaybackState.Stopped;
        private TimeSpan _currentPosition = TimeSpan.Zero;
        private TimeSpan _duration = TimeSpan.Zero;
        private double _volume = 100;
        public event EventHandler<string>? JavaScriptCommandRequested;

        public Song? CurrentSong
        {
            get => _currentSong;
            private set
            {
                if (_currentSong != value)
                {
                    _currentSong = value;
                    CurrentSongChanged?.Invoke(this, _currentSong);
                }
            }
        }

        public PlaybackState State
        {
            get => _state;
            private set
            {
                if (_state != value)
                {
                    _state = value;
                    PlaybackStateChanged?.Invoke(this, _state);
                }
            }
        }

        public TimeSpan CurrentPosition
        {
            get => _currentPosition;
            private set
            {
                if (_currentPosition != value)
                {
                    _currentPosition = value;
                    PositionChanged?.Invoke(this, _currentPosition);
                }
            }
        }

        public TimeSpan Duration
        {
            get => _duration;
            private set => _duration = value;
        }

        public event EventHandler<Song?>? CurrentSongChanged;
        public event EventHandler<PlaybackState>? PlaybackStateChanged;
        public event EventHandler<TimeSpan>? PositionChanged;

        // DIUBAH: constructor kosong (parameterless), tidak lagi minta WebViewPlayer dari DI
        public PlayerService()
        {
        }

        public void UpdatePlaybackStateFromWebView(PlaybackState state)
        {
            State = state;
        }

        public Task PlayAsync(Song song)
        {
            CurrentSong = song;
            State = PlaybackState.Playing;
            return Task.CompletedTask;
        }

        public Task ResumeAsync()
        {
            if (CurrentSong != null && State == PlaybackState.Paused)
            {
                State = PlaybackState.Playing;
                JavaScriptCommandRequested?.Invoke(this, "resumeVideo();");
            }
            return Task.CompletedTask;
        }

        public Task PauseAsync()
        {
            if (State == PlaybackState.Playing)
            {
                State = PlaybackState.Paused;
                JavaScriptCommandRequested?.Invoke(this, "pauseVideo();");
            }
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            State = PlaybackState.Stopped;
            CurrentSong = null;
            CurrentPosition = TimeSpan.Zero;
            JavaScriptCommandRequested?.Invoke(this, "stopVideo();");
            return Task.CompletedTask;
        }

        public Task SeekAsync(TimeSpan position)
        {
            CurrentPosition = position;
            JavaScriptCommandRequested?.Invoke(this, $"player.currentTime = {position.TotalSeconds};");
            return Task.CompletedTask;
        }

        public Task NextAsync()
        {
            return Task.CompletedTask;
        }

        public Task PreviousAsync()
        {
            return Task.CompletedTask;
        }

        // DIUBAH: guard null karena _webViewPlayer mungkin belum di-attach
        public Task SetVolumeAsync(double volume)
        {
            _volume = Math.Clamp(volume, 0, 100);
            var normalized = (_volume / 100.0).ToString(CultureInfo.InvariantCulture);

            JavaScriptCommandRequested?.Invoke(this,
                $"if(typeof player!=='undefined'){{player.volume={normalized};}}");

            return Task.CompletedTask;
        }

        public string GetApplyVolumeScript()
        {
            var normalized = (_volume / 100.0).ToString(CultureInfo.InvariantCulture);
            return $"if(typeof player!=='undefined'){{player.volume={normalized};}}";
        }
    }
}