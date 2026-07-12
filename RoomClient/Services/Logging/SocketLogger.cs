using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace RoomClient.Services.Logging
{
    public static class SocketLogger
    {
        private static readonly string LogPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "logs", "socket.log");

        static SocketLogger()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
        }

        public static void Log(string category, string message)
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{category}] {message}";

            System.Diagnostics.Debug.WriteLine(line);

            try
            {
                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
            catch
            {
                // jangan sampai logging gagal menjatuhkan aplikasi
            }
        }

        public static void LogEvent(string eventName, object? payload)
        {
            var json = payload is null
                ? "null"
                : System.Text.Json.JsonSerializer.Serialize(payload);

            Log("SOCKET-EVENT", $"{eventName} => {json}");
        }
    }
}
