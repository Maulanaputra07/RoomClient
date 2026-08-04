using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Whisper.net;

namespace RoomClient.Services.Voice
{
    public class WhisperService : IDisposable
    {
        private readonly WhisperFactory _whisperFactory;

        public WhisperService()
        {
            var modelPath = Path.Combine(AppContext.BaseDirectory, "Models", "ggml-small.bin");

            if (!File.Exists(modelPath))
            {
                throw new FileNotFoundException("Whisper model tidak ditemukan.", modelPath);
            }

            // Factory cukup diinisialisasi sekali karena berat
            _whisperFactory = WhisperFactory.FromPath(modelPath);
        }


        public async Task<string> TranscribeAsync(string audioPath, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(audioPath))
            {
                throw new FileNotFoundException("File audio tidak ditemukan.", audioPath);
            }

            var result = new List<string>();

            // Processor HARUS dibuat baru untuk setiap rekaman untuk mereset state.
            // Gunakan 'using' agar langsung dibuang dari memory setelah selesai.
            using var processor = _whisperFactory.CreateBuilder()
                .WithLanguage("id") // Tetap ID, tapi kita atasi kelemahan kosa kata asing di Prompt
                .Build();

            await using var audioStream = File.OpenRead(audioPath);

            await foreach (var segment in processor.ProcessAsync(audioStream).WithCancellation(cancellationToken))
            {
                if (!string.IsNullOrWhiteSpace(segment.Text))
                {
                    result.Add(segment.Text);
                }
            }

            return string.Join(" ", result).Trim();
        }

        public void Dispose()
        {
            // Buang factory saat aplikasi benar-benar mati
            _whisperFactory?.Dispose();
        }
    }
}