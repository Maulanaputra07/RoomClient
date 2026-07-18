using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoomClient.Core.Interfaces;
using RoomClient.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.ViewModels
{
    public partial class RegisterViewModel : ObservableObject
    {
        private readonly IApiService _apiService;
        private readonly IConfigService _configService;
        private readonly AppConfig _config;

        private string _hostname = Environment.MachineName;
        private string _clientIp = GetLocalIpAddress();
        private bool _isBusy;
        private string _statusMessage = "";

        public event EventHandler? RegisterSucceeded;

        public RegisterViewModel(IApiService apiService, IConfigService configService)
        {
            _apiService = apiService;
            _configService = configService;
            _config = _configService.LoadCreate();
        }

        public string Hostname
        {
            get => _hostname;
            set => SetProperty(ref _hostname, value);
        }

        public string ClientIp
        {
            get => _clientIp;
            set => SetProperty(ref _clientIp, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        [RelayCommand]
        private async Task RegisterAsync()
        {
            if (IsBusy)
                return;

            if (string.IsNullOrEmpty(Hostname) || string.IsNullOrEmpty(ClientIp))
            {
                StatusMessage = "Hostname dan Client IP wajib diisi.";
                return;
            }

            IsBusy = true;
            StatusMessage = "Mendaftarkan client...";

            try
            {
                var success = await _apiService.RegisterClientAsync(new RegisterClientRequest
                {
                    DeviceId = _config.DeviceId,
                    DeviceIp = ClientIp,
                    Hostname = Hostname
                });

                if (success)
                {
                    _config.isRegistered = true;
                    _configService.Save(_config);
                    StatusMessage = "Register berhasil.";
                    RegisterSucceeded?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    StatusMessage = "Registrasi gagal. Silakan coba lagi.";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("===== REGISTER ERROR =====");
                Debug.WriteLine(ex.ToString());
                StatusMessage = $"Terjadi kesalahan: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static string GetLocalIpAddress()
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Idp);
                socket.Connect("8.8.8.8", 65530);
                return (socket.LocalEndPoint as IPEndPoint)?.Address.ToString() ?? "";
            }catch
            {
                return "";
            }
        }
    }
}
