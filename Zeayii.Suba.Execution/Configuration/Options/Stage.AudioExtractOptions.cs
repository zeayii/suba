namespace Zeayii.Suba.Core.Configuration.Options;

/// <summary>
/// Zeayii 音频提取阶段配置。
/// </summary>
public sealed class AudioExtractOptions
{
    /// <summary>
    /// Zeayii FFmpeg 可执行文件绝对路径。
    /// </summary>
    public required string FfmpegPath { get; init; }

    /// <summary>
    /// Zeayii 缓存目录绝对路径。
    /// </summary>
    public required string CacheDirectory { get; init; }
}