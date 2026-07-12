using System.Net.Http;
using System.Net.Http.Json;
using RoomClient.Core.Interfaces;
using RoomClient.Core.Models;

namespace RoomClient.Services.Api
{
    public class ApiService : IApiService
    {
        private static readonly Uri BaseUri = new("http://192.168.201.220:3000/");
        private readonly HttpClient _httpClient;

        public ApiService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = BaseUri
            };
        }

        public async Task<IReadOnlyList<Room>> GetRoomsAsync(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetFromJsonAsync<RoomsResponse>("api/rooms", cancellationToken);

            if (response?.Data is null)
            {
                return Array.Empty<Room>();
            }

            return response.Data;
        }

        private sealed class RoomsResponse
        {
            public bool Success { get; set; }

            public string Message { get; set; } = string.Empty;

            public List<Room> Data { get; set; } = [];
        }
    }
}
