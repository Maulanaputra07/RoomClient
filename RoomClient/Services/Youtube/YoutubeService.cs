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

            var baseUrl = ConfigurationProvider.ApiSettings.YoutubeAPI;
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        public async Task<List<Song>> GetByCategoryAsync(string categorySlug)
        {
            if (string.IsNullOrWhiteSpace(categorySlug))
            {
                return [];
            }

            var url = $"/category/{Uri.EscapeDataString(categorySlug)}";

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
                throw new InvalidOperationException($"Gagal memuat kategori: {ex.Message}", ex);
            }
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
  video::-webkit-media-controls-fullscreen-button {{ display: none !important; }}
  video::-webkit-media-controls-overflow-button {{ display: none !important; }}
  video::-webkit-media-controls-mute-button,
  video::-webkit-media-controls-volume-slider,
  video::-webkit-media-controls-volume-slider-container {{ display: none !important; }}
  video::-webkit-media-controls-toggle-closed-captions-button {{ display: none !important; }}
  video::-webkit-media-controls-picture-in-picture-button {{ display: none !important; }}

  #exitFullscreenBtn {{
    position: fixed;
    bottom: 28px;
    right: 40px;
    width: 40px;
    height: 40px;
    border: none;
    background: transparent;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    z-index: 999;
    padding: 0;
    opacity: 0.9;
    transition: opacity 0.15s ease, transform 0.15s ease;
  }}
  #exitFullscreenBtn:hover {{
    opacity: 1;
    transform: scale(1.1);
  }}
  #exitFullscreenBtn svg {{
    width: 24px;
    height: 24px;
    filter: drop-shadow(0 1px 2px rgba(0,0,0,0.8));
  }}
</style>
</head>
<body>
<video id='player' autoplay controls controlsList=""nodownload noremoteplayback nofullscreen"" playsinline src='{escapedUrl}'></video>

<button id='exitFullscreenBtn' title='Keluar dari Fullscreen'>
  <svg viewBox=""0 0 24 24"" fill=""white"" xmlns=""http://www.w3.org/2000/svg"">
    <path d=""M14 14h6v2h-4v4h-2v-6zm-4-4H4V8h4V4h2v6zm4-6h2v4h4v2h-6V4zM4 16h6v6H8v-4H4v-2z""/>
  </svg>
</button>

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

  document.getElementById('exitFullscreenBtn').addEventListener('click', function() {{
    window.chrome.webview.postMessage(JSON.stringify({{ type: 'exitFullscreen' }}));
  }});
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
