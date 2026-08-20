# VLC Player Integration — Context & Flow

> **Tujuan file ini:** Memberikan konteks lengkap tentang bagaimana VLC diintegrasikan ke dalam RoomClient sebagai media player engine tunggal.

---

## 1. Latar Belakang

RoomClient adalah aplikasi karaoke room berbasis WPF (C#). Aplikasi ini mendukung pemutaran lagu dari dua sumber yang **sepenuhnya diputar lewat LibVLCSharp (VLC)**:

| Source | Render | Keterangan |
|---|---|---|
| `SongSource.Youtube` | **LibVLCSharp (VLC)** | Lagu dicari dari YouTube API backend, direct stream URL di-extract via `IYoutubeService.GetStreamUrlAsync()`, lalu langsung diputar lewat VLC |
| `SongSource.Database` | **LibVLCSharp (VLC)** | Lagu dari database internal server karaoke, memiliki `DirectStreamUrl` (misalnya file URL atau RTSP stream), diputar langsung lewat VLC |

---

## 2. Arsitektur Layer

```
Server (SignalR / REST)
    │
    ▼
MainWindowViewModel
    │  (event: play, queue, session)
    ▼
PlayerViewModel           ← pusat logika player
    │  event: VlcCommandRequested
    │  property: VlcSourceUrl (Observable)
    ▼
PlayerView.xaml.cs        ← code-behind, penghubung ViewModel ↔ UI
    │
    └── VlcVideoView (VideoView)  ← VLC engine tunggal untuk semua playback
```

### Service Layer

```
IPlayerService (interface)
    └── PlayerService (implementasi)
            - PlaybackState, CurrentSong, Duration, Position
            - Dispatch VlcCommandRequested (Play, Pause, Resume, Stop, Seek, SetVolume)
```

---

## 3. Model-model Kunci

### `Song` (`Core/Models/Song.cs`)

```csharp
public enum SongSource { Youtube, Database }

public class Song
{
    public SongSource Source { get; set; }       // Sumber lagu
    public string? DirectStreamUrl { get; set; } // URL stream untuk VLC (Database source)
    public string? VideoId { get; set; }         // YouTube video ID (Youtube source)
    public string Title { get; set; }
    public string? Artist { get; set; }
    public TimeSpan? Duration { get; set; }
}
```

### `VlcCommand` (`Core/Models/VlcCommand.cs`)

```csharp
public enum VlcCommandType { Play, Pause, Resume, Stop, Seek, SetVolume }

public class VlcCommand
{
    public VlcCommandType Type { get; init; }
    public string? Source { get; init; }      // URL media (hanya untuk Play)
    public double? Volume { get; init; }      // 0–100 (hanya untuk SetVolume)
    public TimeSpan? Position { get; init; } // (hanya untuk Seek)
}
```

---

## 4. Flow Lengkap Pemutaran Lagu

```
User klik lagu (YouTube Search / Database)
        │
        ▼
MainWindowViewModel.EnqueueOrPlaySong(song)
        │
        ▼
PlayerViewModel.PlayAsync(song)
        │
        ├── Ambil stream URL:
        │       song.Source == SongSource.Database -> song.DirectStreamUrl
        │       song.Source == SongSource.Youtube  -> await _youtubeService.GetStreamUrlAsync(song.VideoId)
        │
        ├── VlcSourceUrl = streamUrl (trigger PropertyChanged)
        └── await _playerService.PlayAsync(song)
        │
        ▼
PlayerView.OnPlayerPropertyChanged()
  └── e.PropertyName == "VlcSourceUrl" → LoadVlcSourceAsync()
            │
            ├── EnsureVlcInitialized()
            │       ├── Core.Initialize()
            │       ├── new LibVLC()
            │       ├── new MediaPlayer(_libVlc)
            │       ├── VlcVideoView.MediaPlayer = _vlcMediaPlayer
            │       └── daftarkan event: EndReached, Playing, Paused, EncounteredError
            │
            └── _vlcMediaPlayer.Play(new Media(_libVlc, new Uri(streamUrl)))
```

---

## 5. Event Callbacks dari VLC ke ViewModel

```csharp
_vlcMediaPlayer.EndReached += (s, e) =>
    Dispatcher.Invoke(() => _playerViewModel?.NotifySongEnded());

_vlcMediaPlayer.Playing += (s, e) =>
    Dispatcher.Invoke(() => _playerViewModel?.NotifyPlaybackState(PlaybackState.Playing));

_vlcMediaPlayer.Paused += (s, e) =>
    Dispatcher.Invoke(() => _playerViewModel?.NotifyPlaybackState(PlaybackState.Paused));

_vlcMediaPlayer.EncounteredError += (s, e) =>
    System.Diagnostics.Debug.WriteLine("[VLC] Playback error encountered.");
```

`NotifySongEnded()` di `PlayerViewModel` akan:
1. Cek apakah ada grace period session expired → `FinalizeSessionExpiry()`
2. Jika ada lagu berikutnya di queue → `NextCommand.Execute()`
3. Jika tidak → `StopAsync()`

---

## 6. Dependency NuGet

| Package | Kegunaan |
|---|---|
| `LibVLCSharp` | Core VLC binding untuk .NET |
| `LibVLCSharp.WPF` | `VideoView` control untuk WPF |
| `VideoLAN.LibVLC.Windows` | Native VLC DLL untuk Windows (runtime) |
