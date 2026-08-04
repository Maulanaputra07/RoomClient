using RoomClient.Core.Interfaces;
using RoomClient.Core.Models;
using RoomClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoomClient.ViewModels;

namespace RoomClient.Services.Player
{
    public class PlayerService : IPlayerService
    {
        private Song? _currentSong;
        private PlaybackState _state = PlaybackState.Stopped;
        private TimeSpan _currentPosition = TimeSpan.Zero;
        private TimeSpan _duration = TimeSpan.Zero;

        private readonly PlayerViewModel _playerViewModel;

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

        public Task PlayAsync(Song song)
        {
            CurrentSong = song;
            State = PlaybackState.Playing;

            // Masukkan logika pemutaran engine media Anda di sini
            return Task.CompletedTask;
        }

        public Task ResumeAsync()
        {
            if (CurrentSong != null && State == PlaybackState.Paused)
            {
                State = PlaybackState.Playing;
                // Masukkan logika resume media di sini
            }
            return Task.CompletedTask;
        }

        public Task PauseAsync()
        {
            if (State == PlaybackState.Playing)
            {
                State = PlaybackState.Paused;
                // Masukkan logika pause media di sini
            }
            return Task.CompletedTask;
        }

        public Task StopAsync()
        {
            State = PlaybackState.Stopped;
            CurrentSong = null;
            CurrentPosition = TimeSpan.Zero;
            // Masukkan logika stop media di sini
            return Task.CompletedTask;
        }

        public Task SeekAsync(TimeSpan position)
        {
            CurrentPosition = position;
            // Masukkan logika seek media di sini
            return Task.CompletedTask;
        }

        public Task NextAsync()
        {
            // Ambil lagu berikutnya dari antrean lalu panggil PlayAsync
            return Task.CompletedTask;
        }

        public Task PreviousAsync()
        {
            // Ambil lagu sebelumnya dari antrean lalu panggil PlayAsync
            return Task.CompletedTask;
        }
    }
}
