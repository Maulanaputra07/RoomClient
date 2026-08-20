using RoomClient.Core.Interfaces;
using RoomClient.Core.Models;
using System;
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

        public event EventHandler<VlcCommand>? VlcCommandRequested;

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

        public PlayerService()
        {
        }

        public void UpdatePlaybackState(PlaybackState state)
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
                VlcCommandRequested?.Invoke(this, new VlcCommand { Type = VlcCommandType.Resume });
            }
            return Task.CompletedTask;
        }

        public Task PauseAsync()
        {
            if (State == PlaybackState.Playing)
            {
                State = PlaybackState.Paused;
                VlcCommandRequested?.Invoke(this, new VlcCommand { Type = VlcCommandType.Pause });
            }
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            State = PlaybackState.Stopped;
            CurrentSong = null;
            CurrentPosition = TimeSpan.Zero;
            VlcCommandRequested?.Invoke(this, new VlcCommand { Type = VlcCommandType.Stop });
            return Task.CompletedTask;
        }

        public Task SeekAsync(TimeSpan position)
        {
            CurrentPosition = position;
            VlcCommandRequested?.Invoke(this, new VlcCommand { Type = VlcCommandType.Seek, Position = position });
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

        public Task SetVolumeAsync(double volume)
        {
            _volume = Math.Clamp(volume, 0, 100);
            VlcCommandRequested?.Invoke(this, new VlcCommand { Type = VlcCommandType.SetVolume, Volume = _volume });
            return Task.CompletedTask;
        }
    }
}