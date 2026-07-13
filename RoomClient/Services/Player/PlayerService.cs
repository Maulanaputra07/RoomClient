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
        private readonly PlayerViewModel _playerViewModel;

        public PlayerService(PlayerViewModel playerViewModel)
        {
            _playerViewModel = playerViewModel;
        }

        public Task PlayAsync(Song song)
        {
            _playerViewModel.PlayAsync(song);
            return Task.CompletedTask;
        }

        public Task PauseAsync()
        {
            throw new NotImplementedException();
        }

        public Task StopAsync()
        {
            _playerViewModel.Stop();
            return Task.CompletedTask;
        }
    }
}
