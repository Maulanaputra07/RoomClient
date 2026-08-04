using RoomClient.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoomClient.Services.Voice
{
    public class VoiceSearchService : IVoiceSearchService
    {
        private readonly IMicrophoneService _microphoneService;
        private readonly WhisperService _whisperService;
        public VoiceSearchService(
            IMicrophoneService microphoneService,
            WhisperService whisperService)
        {
            _microphoneService = microphoneService;
            _whisperService = whisperService;
        }
        public async Task<string?> ListenAsync(
            CancellationToken cancellationToken = default)
        {
            var audioPath =
                await _microphoneService.RecordUntilSilenceAsync(
                cancellationToken);

            try
            {
                var transcript =
                    await _whisperService.TranscribeAsync(
                        audioPath,
                        cancellationToken);

                if (string.IsNullOrWhiteSpace(transcript))
                {
                    return null;
                }

                var query =
                    VoiceQueryProcessor.Process(transcript);

                return string.IsNullOrWhiteSpace(query)
                    ? null
                    : query;
            }
            finally
            {
                if (File.Exists(audioPath))
                {
                    File.Delete(audioPath);
                }
            }
        }
    }
}