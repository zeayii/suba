namespace Zeayii.Suba.Core.Services;

/// <summary>
/// Zeayii WAV 文件读取器。
/// </summary>
internal static class WavReader
{
    /// <summary>
    /// Zeayii 读取 16-bit PCM 单声道 WAV 并输出归一化浮点样本。
    /// </summary>
    /// <param name="path">Zeayii WAV 文件路径。</param>
    /// <param name="sampleRate">Zeayii 输出采样率。</param>
    /// <returns>Zeayii 归一化音频样本。</returns>
    public static float[] ReadMono16Pcm(string path, out int sampleRate)
    {
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);

        var riff = new string(reader.ReadChars(4));
        if (!string.Equals(riff, "RIFF", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Invalid wav header: RIFF not found.");
        }

        _ = reader.ReadInt32();
        var wave = new string(reader.ReadChars(4));
        if (!string.Equals(wave, "WAVE", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Invalid wav header: WAVE not found.");
        }

        short channels = 0;
        short bitsPerSample = 0;
        sampleRate = 0;
        byte[]? pcmData = null;

        while (stream.Position < stream.Length)
        {
            var chunkId = new string(reader.ReadChars(4));
            var chunkSize = reader.ReadInt32();

            if (string.Equals(chunkId, "fmt ", StringComparison.Ordinal))
            {
                var audioFormat = reader.ReadInt16();
                channels = reader.ReadInt16();
                sampleRate = reader.ReadInt32();
                _ = reader.ReadInt32();
                _ = reader.ReadInt16();
                bitsPerSample = reader.ReadInt16();
                if (chunkSize > 16)
                {
                    _ = reader.ReadBytes(chunkSize - 16);
                }

                if (audioFormat != 1)
                {
                    throw new NotSupportedException("Only PCM wav is supported.");
                }
            }
            else if (string.Equals(chunkId, "data", StringComparison.Ordinal))
            {
                pcmData = reader.ReadBytes(chunkSize);
            }
            else
            {
                _ = reader.ReadBytes(chunkSize);
            }
        }

        if (sampleRate <= 0 || channels != 1 || bitsPerSample != 16 || pcmData is null)
        {
            throw new InvalidDataException("Unsupported wav format. Require mono/16bit PCM.");
        }

        var sampleCount = pcmData.Length / 2;
        var output = new float[sampleCount];
        for (var i = 0; i < sampleCount; i++)
        {
            var value = BitConverter.ToInt16(pcmData, i * 2);
            output[i] = value / 32768f;
        }

        return output;
    }
}
