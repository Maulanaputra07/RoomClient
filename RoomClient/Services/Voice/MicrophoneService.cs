using NAudio.CoreAudioApi;
using NAudio.Wave;
using RoomClient.Core.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RoomClient.Services.Voice
{
    public class MicrophoneService : IMicrophoneService
    {
        private const int SampleRate = 16000;
        private const int Channels = 1;
        private const int BitsPerSample = 16;

        private const double SilenceThreshold = 0.01;
        private const int SilenceDurationMs = 800;
        private const int MaxRecordingDurationMs = 10000;
        private const int MinRecordingDurationMs = 500;
        private const int MinimumSpeechDurationMs = 200;
        private const int NoSpeechTimeoutMs = 5000;

        bool speechStarted = false;
        DateTime? silenceStartedAt = null;


        public async Task<string> RecordAsync(
            int durationSeconds = 5,
            CancellationToken cancellationToken = default)
        {
            var tempFolder = Path.GetTempPath();
            var rawPath = Path.Combine(tempFolder, $"raw-{Guid.NewGuid():N}.wav");
            var outputPath = Path.Combine(tempFolder, $"roomclient-whisper-{Guid.NewGuid():N}.wav");

            try
            {
                // 1. Rekam audio native via WASAPI (Jernih & Bebas Noise Resampling MME)
                using (var capture = new WasapiCapture())
                {
                    using (var writer = new WaveFileWriter(rawPath, capture.WaveFormat))
                    {
                        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                        capture.DataAvailable += (_, e) =>
                        {
                            writer.Write(e.Buffer, 0, e.BytesRecorded);
                        };

                        capture.RecordingStopped += (_, e) =>
                        {
                            if (e.Exception != null)
                                tcs.TrySetException(e.Exception);
                            else
                                tcs.TrySetResult(true);
                        };

                        capture.StartRecording();

                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(durationSeconds), cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            // Dibatalkan secara normal via cancellation token
                        }
                        finally
                        {
                            capture.StopRecording();
                            await tcs.Task; // Tunggu writer selesai flushing buffer ke disk
                        }
                    }
                }

                // 2. Resample file mentah ke standar Whisper (16kHz 16-bit Mono) dengan kualitas tinggi
                ResampleToWhisperFormat(rawPath, outputPath);

                return outputPath;
            }
            finally
            {
                // Hapus file mentah (raw) temporary setelah konversi selesai
                if (File.Exists(rawPath))
                {
                    try { File.Delete(rawPath); } catch { }
                }
            }
        }

        public async Task<string> RecordUntilSilenceAsync(
    CancellationToken cancellationToken = default)
        {
            var outputPath = CreateTempFilePath();

            using var waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(
                    SampleRate,
                    BitsPerSample,
                    Channels),

                BufferMilliseconds = 100
            };

            using var writer =
                new WaveFileWriter(
                    outputPath,
                    waveIn.WaveFormat);

            var completionSource =
                new TaskCompletionSource<bool>(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);

            var recordingStartedAt = DateTime.UtcNow;

            DateTime? speechStartedAt = null;
            DateTime? silenceStartedAt = null;

            waveIn.DataAvailable += (_, e) =>
            {
                writer.Write(
                    e.Buffer,
                    0,
                    e.BytesRecorded);

                var rms = CalculateRms(
                    e.Buffer,
                    e.BytesRecorded);

                Debug.WriteLine(
                    $"RMS: {rms:F4}");

                var elapsed =
                    DateTime.UtcNow -
                    recordingStartedAt;

                // =========================
                // WAITING FOR SPEECH
                // =========================

                if (speechStartedAt is null)
                {
                    if (rms >= SilenceThreshold)
                    {
                        speechStartedAt =
                            DateTime.UtcNow;

                        Debug.WriteLine(
                            "Speech detected.");
                    }

                    // Tidak ada speech selama 5 detik
                    if (elapsed.TotalMilliseconds >=
                        NoSpeechTimeoutMs)
                    {
                        Debug.WriteLine(
                            "No speech detected.");

                        waveIn.StopRecording();
                    }

                    return;
                }

                // =========================
                // VERIFY SPEECH DURATION
                // =========================

                var speechDuration =
                    DateTime.UtcNow -
                    speechStartedAt.Value;

                if (speechDuration.TotalMilliseconds <
                    MinimumSpeechDurationMs)
                {
                    return;
                }

                // =========================
                // SILENCE DETECTION
                // =========================

                if (rms < SilenceThreshold)
                {
                    silenceStartedAt ??=
                        DateTime.UtcNow;

                    var silenceDuration =
                        DateTime.UtcNow -
                        silenceStartedAt.Value;

                    if (silenceDuration.TotalMilliseconds >=
                        SilenceDurationMs)
                    {
                        Debug.WriteLine(
                            "Silence detected. Stopping.");

                        waveIn.StopRecording();
                    }
                }
                else
                {
                    // User kembali berbicara
                    silenceStartedAt = null;
                }

                // =========================
                // MAX RECORDING
                // =========================

                if (elapsed.TotalMilliseconds >=
                    MaxRecordingDurationMs)
                {
                    Debug.WriteLine(
                        "Maximum recording duration reached.");

                    waveIn.StopRecording();
                }
            };

            waveIn.RecordingStopped += (_, _) =>
            {
                completionSource.TrySetResult(true);
            };

            waveIn.StartRecording();

            try
            {
                await completionSource.Task
                    .WaitAsync(cancellationToken);

                return outputPath;
            }
            catch
            {
                waveIn.StopRecording();

                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }

                throw;
            }
        }


        private static double CalculateRms(
        byte[] buffer,
        int bytesRecorded)
        {
            if (bytesRecorded < 2)
            {
                return 0;
            }

            double sumSquares = 0;

            var sampleCount =
                bytesRecorded / 2;

            for (var i = 0;
                 i < bytesRecorded;
                 i += 2)
            {
                short sample =
                    BitConverter.ToInt16(
                        buffer,
                        i);

                var normalized =
                    sample / 32768.0;

                sumSquares +=
                    normalized * normalized;
            }

            return Math.Sqrt(
                sumSquares / sampleCount);
        }

        private static string CreateTempFilePath()
        {
            return Path.Combine(
                Path.GetTempPath(),
                $"roomclient-voice-{Guid.NewGuid():N}.wav");
        }

        private static void ResampleToWhisperFormat(string inputPath, string outputPath)
        {
            using var reader = new AudioFileReader(inputPath);
            var targetFormat = new WaveFormat(16000, 16, 1); // Standar Whisper: 16kHz, 16-bit, Mono

            using var resampler = new MediaFoundationResampler(reader, targetFormat)
            {
                ResamplerQuality = 60 // Quality 60 = Kualitas resampling tertinggi di Windows Media Foundation
            };

            WaveFileWriter.CreateWaveFile(outputPath, resampler);
        }
    }
}