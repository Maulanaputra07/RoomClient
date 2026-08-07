using System.Net.Http;
using System.Net.Http.Json;
using RoomClient.Config;
using RoomClient.Core.Interfaces;
using RoomClient.Core.Models;

namespace RoomClient.Services.Api
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;


        public ApiService(HttpClient httpClient, IConfigService configService)
        {
            _httpClient = httpClient;

            var baseUrl = ConfigurationProvider.ApiSettings.ServerAPI;
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        public async Task<IReadOnlyList<Room>> GetRoomsAsync(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetFromJsonAsync<RoomsResponse>("rooms", cancellationToken);

            if (response?.Data is null)
            {
                return Array.Empty<Room>();
            }

            return response.Data;
        }

        public async Task<bool> RegisterClientAsync(RegisterClientRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("devices/register", request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    throw new InvalidOperationException(
                        $"Server menolak registrasi ({(int)response.StatusCode} {response.StatusCode}): {errorBody}");
                }

                return true;
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Gagal menghubungi server: {ex.Message}", ex);
            }
        }

        private sealed class RoomsResponse
        {
            public bool Success { get; set; }

            public string Message { get; set; } = string.Empty;

            public List<Room> Data { get; set; } = [];
        }
    }
}
