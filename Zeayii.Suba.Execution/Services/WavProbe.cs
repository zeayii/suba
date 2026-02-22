using Zeayii.Suba.Core.Contexts;
using Zeayii.Suba.Core.Orchestration;

namespace Zeayii.Suba.Core.Services;

/// <summary>
/// Zeayii WAV 头探测器。
/// </summary>
internal sealed class WavProbe
{
    /// <summary>
    /// Zeayii 探测输入文件是否满足旁路提取条件。
    /// </summary>
    /// <param name="path">Zeayii 输入文件路径。</param>
    /// <returns>Zeayii WAV 探测结果。</returns>
    public WavProbeResult Probe(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return CreateNotWavResult();
        }

        try
        {
            using var stream = File.OpenRead(path);
            using var reader = new BinaryReader(stream);

            if (stream.Length < 12)
            {
                return CreateNotWavResult();
            }

            var riff = new string(reader.ReadChars(4));
            _ = reader.ReadInt32();
            var wave = new string(reader.ReadChars(4));
            if (!string.Equals(riff, "RIFF", StringComparison.Ordinal) ||
                !string.Equals(wave, "WAVE", StringComparison.Ordinal))
            {
                return CreateNotWavResult();
            }

            short audioFormat = 0;
            short channels = 0;
            int sampleRate = 0;
            short bitsPerSample = 0;
            var hasDataChunk = false;

            while (stream.Position + 8 <= stream.Length)
            {
                var chunkId = new string(reader.ReadChars(4));
                var chunkSize = reader.ReadInt32();
                if (chunkSize < 0)
                {
                    return CreateNotWavResult();
                }

                var nextPosition = stream.Position + chunkSize;
                if (nextPosition > stream.Length)
                {
                    return CreateNotWavResult();
                }

                if (string.Equals(chunkId, "fmt ", StringComparison.Ordinal))
                {
                    if (chunkSize < 16)
                    {
                        return CreateNotWavResult();
                    }

                    audioFormat = reader.ReadInt16();
                    channels = reader.ReadInt16();
                    sampleRate = reader.ReadInt32();
                    _ = reader.ReadInt32();
                    _ = reader.ReadInt16();
                    bitsPerSample = reader.ReadInt16();
                }
                else if (string.Equals(chunkId, "data", StringComparison.Ordinal))
                {
                    hasDataChunk = chunkSize > 0;
                }

                stream.Position = nextPosition;
            }

            return new WavProbeResult
            {
                IsWav = true,
                AudioFormat = audioFormat,
                Channels = channels,
                SampleRate = sampleRate,
                BitsPerSample = bitsPerSample,
                HasDataChunk = hasDataChunk,
                CanBypassExtraction = audioFormat == 1 && channels == 1 && sampleRate == GlobalContext.DefaultTargetSampleRateHz && bitsPerSample == 16 && hasDataChunk
            };
        }
        catch (IOException)
        {
            return CreateNotWavResult();
        }
        catch (UnauthorizedAccessException)
        {
            return CreateNotWavResult();
        }
    }

    /// <summary>
    /// Zeayii 构造非 WAV 默认探测结果。
    /// </summary>
    /// <returns>Zeayii 探测结果。</returns>
    private static WavProbeResult CreateNotWavResult()
    {
        return new WavProbeResult
        {
            IsWav = false,
            AudioFormat = 0,
            Channels = 0,
            SampleRate = 0,
            BitsPerSample = 0,
            HasDataChunk = false,
            CanBypassExtraction = false
        };
    }
}
