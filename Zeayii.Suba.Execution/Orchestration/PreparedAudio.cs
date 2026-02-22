namespace Zeayii.Suba.Core.Orchestration;

/// <summary>
/// Zeayii 音频准备结果。
/// </summary>
public sealed class PreparedAudio
{
    /// <summary>
    /// Zeayii 处理后的音频路径。
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Zeayii 是否旁路了提取阶段。
    /// </summary>
    public required bool IsExtractionBypassed { get; init; }
}
