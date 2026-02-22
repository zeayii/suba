namespace Zeayii.Suba.Core.Orchestration;

/// <summary>
/// Zeayii WAV 头探测结果。
/// </summary>
public sealed class WavProbeResult
{
    /// <summary>
    /// Zeayii 输入是否为有效 WAV 容器。
    /// </summary>
    public required bool IsWav { get; init; }

    /// <summary>
    /// Zeayii 音频格式标签（PCM=1）。
    /// </summary>
    public required short AudioFormat { get; init; }

    /// <summary>
    /// Zeayii 声道数。
    /// </summary>
    public required short Channels { get; init; }

    /// <summary>
    /// Zeayii 采样率（Hz）。
    /// </summary>
    public required int SampleRate { get; init; }

    /// <summary>
    /// Zeayii 位深。
    /// </summary>
    public required short BitsPerSample { get; init; }

    /// <summary>
    /// Zeayii 是否包含 data 块。
    /// </summary>
    public required bool HasDataChunk { get; init; }

    /// <summary>
    /// Zeayii 是否可直接旁路提取阶段。
    /// </summary>
    public required bool CanBypassExtraction { get; init; }
}
