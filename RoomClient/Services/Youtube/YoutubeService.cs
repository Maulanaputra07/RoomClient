using RoomClient.Config;
using RoomClient.Core.Interfaces;
using RoomClient.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace RoomClient.Services.Youtube
{
    public class YoutubeService : IYoutubeService
    {
        private readonly HttpClient _httpClient;

        public YoutubeService(HttpClient httpClient, IConfigService configService)
        {
            _httpClient = httpClient;

            var baseUrl = configService.Config.ApiSettings.YoutubeAPI;
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        public async Task<List<Song>> SearchAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return [];
            }
            var url = $"/youtube/search?q={Uri.EscapeDataString(keyword.Trim())}";
            try
            {
                var response = await _httpClient.GetFromJsonAsync<YoutubeSearchResponse>(url);

                if (response is not { Success: true } || response.Data.Count == 0)
                {
                    return [];
                }

                return response.Data.Select(item => new Song
                {
                    VideoId = item.VideoId,
                    Title = item.Title,
                    Artist = item.Channel,
                    Duration = TimeSpan.FromSeconds(item.DurationSeconds),
                    Thumbnail = item.Thumbnail,
                    ViewCount = item.ViewCount
                }).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Gagal mencari lagu: {ex.Message}", ex);
            }
        }

        public async Task<string?> GetStreamUrlAsync(string videoId)
        {
            if (string.IsNullOrWhiteSpace(videoId))
            {
                return null;
            }

            var url = $"/youtube/stream/{Uri.EscapeDataString(videoId)}";

            try
            {
                var response = await _httpClient.GetFromJsonAsync<YoutubeStreamResponse>(url);
                return response is { Success: true, Data: not null }
                    ? response.Data.StreamUrl
                    : null;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Gagal memuat stream: {ex.Message}", ex);
            }
        }



        public string BuildPlayerHtml(string streamUrl)
        {
            var escapedUrl = EscapeJavaScriptString(streamUrl);

            return $@"
        <!DOCTYPE html>
        <html>
        <head>
        <meta charset='UTF-8'>
        <style>
          html,body{{margin:0;padding:0;width:100%;height:100%;overflow:hidden;background:black;}}
          video{{width:100%;height:100%;object-fit:contain;}}
        </style>
        </head>
        <body>
        <video id='player' autoplay controls playsinline src='{escapedUrl}'></video>
        <script>
          var player = document.getElementById('player');
          player.addEventListener('ended', function() {{
            window.chrome.webview.postMessage(JSON.stringify({{ type: 'ended' }}));
          }});
          player.addEventListener('error', function() {{
            window.chrome.webview.postMessage(JSON.stringify({{ type: 'error' }}));
          }});
          player.addEventListener('loadeddata', function() {{
            window.chrome.webview.postMessage(JSON.stringify({{ type: 'ready' }}));
          }});
        player.addEventListener('play', function() {{
          window.chrome.webview.postMessage(JSON.stringify({{ type: 'play' }}));
        }});
        player.addEventListener('pause', function() {{
          window.chrome.webview.postMessage(JSON.stringify({{ type: 'pause' }}));
        }});
          function pauseVideo() {{ player.pause(); }}
          function resumeVideo() {{ player.play(); }}
          function stopVideo() {{ player.pause(); player.currentTime = 0; }}
        </script>
        </body>
        </html>";
        }

        private static string EscapeJavaScriptString(string value)
        {
            return value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("'", "\\'", StringComparison.Ordinal);
        }
    }
}
