namespace Zeayii.Suba.Execution.Tests.TestSupport;

/// <summary>
/// Zeayii 测试用 WAV 文件构造器。
/// </summary>
internal static class TestWaveFileBuilder
{
    /// <summary>
    /// Zeayii 写入 PCM16 单声道 WAV 文件。
    /// </summary>
    /// <param name="path">Zeayii 输出路径。</param>
    /// <param name="sampleRate">Zeayii 采样率。</param>
    /// <param name="samples">Zeayii 采样数据。</param>
    public static void WriteMonoPcm16(string path, int sampleRate, IReadOnlyList<short> samples)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        const short channels = 1;
        const short bitsPerSample = 16;
        var blockAlign = (short)(channels * bitsPerSample / 8);
        var byteRate = sampleRate * blockAlign;
        var dataSize = samples.Count * sizeof(short);

        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8.ToArray());
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8.ToArray());
        writer.Write("fmt "u8.ToArray());
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);
        writer.Write("data"u8.ToArray());
        writer.Write(dataSize);

        foreach (var sample in samples)
        {
            writer.Write(sample);
        }
    }
}
