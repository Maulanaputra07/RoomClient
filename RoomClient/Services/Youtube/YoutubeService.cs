using RoomClient.Core.Interfaces;
using RoomClient.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RoomClient.Services.Youtube
{
    public class YoutubeService : IYoutubeService
    {
        private static readonly Song[] DummyCatalog =
        [
            new()
            {
                VideoId = "M7lc1UVf-VE",
                Title = "YouTube IFrame Player API Demo",
                Artist = "YouTube Developers",
                Duration = TimeSpan.FromMinutes(1)
            },
            new()
            {
                VideoId = "dQw4w9WgXcQ",
                Title = "Never Gonna Give You Up",
                Artist = "Rick Astley",
                Duration = TimeSpan.FromMinutes(3) + TimeSpan.FromSeconds(33)
            },
            new()
            {
                VideoId = "LjhCEhWiKXk",
                Title = "Just The Way You Are (Dummy)",
                Artist = "Bruno Mars",
                Duration = TimeSpan.FromMinutes(3) + TimeSpan.FromSeconds(40)
            }
        ];

        public Task<List<Song>> SearchAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return Task.FromResult<List<Song>>([]);
            }

            var query = keyword.Trim();
            var results = DummyCatalog
                .Where(song =>
                    song.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    song.Artist.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (results.Count == 0)
            {
                results = [DummyCatalog[0]];
            }

            return Task.FromResult(results);
        }

        public string BuildPlayerHtml(Song song)
        {
            var videoId = EscapeJavaScriptString(song.VideoId);

            return $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='UTF-8'>
<meta name='referrer' content='strict-origin-when-cross-origin'>
<style>
  html,body{{margin:0;padding:0;width:100%;height:100%;overflow:hidden;background:black;}}
  #player{{width:100%;height:100%;}}
</style>
</head>
<body>
<div id='player'></div>
<script src='https://www.youtube.com/iframe_api'></script>
<script>
  var player;
  var videoId = '{videoId}';

  function onYouTubeIframeAPIReady() {{
    player = new YT.Player('player', {{
      videoId: videoId,
      playerVars: {{
        autoplay: 1,
        controls: 1,
        modestbranding: 1,
        rel: 0,
        iv_load_policy: 3,
        fs: 0,
        disablekb: 1,
        cc_load_policy: 0,
        playsinline: 1,
        origin: 'https://roomclient.local'
      }},
      events: {{
        'onStateChange': onPlayerStateChange,
        'onReady': onPlayerReady
      }}
    }});
  }}

  function onPlayerReady(event) {{
    window.chrome.webview.postMessage(JSON.stringify({{ type: 'ready' }}));
  }}

  function onPlayerStateChange(event) {{
    if (event.data === YT.PlayerState.ENDED) {{
      window.chrome.webview.postMessage(JSON.stringify({{ type: 'ended' }}));
    }}
  }}

  function pauseVideo() {{ if (player) player.pauseVideo(); }}
  function resumeVideo() {{ if (player) player.playVideo(); }}
  function stopVideo() {{ if (player) player.stopVideo(); }}
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
