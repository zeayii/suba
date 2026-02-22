using Zeayii.Suba.Core.Orchestration;

namespace Zeayii.Suba.Core.Abstractions;

/// <summary>
/// Zeayii 音频提取器接口，负责将输入媒体提取为单声道 WAV 音频。
/// </summary>
public interface IAudioExtractor
{
    /// <summary>
    /// Zeayii 从输入媒体提取单声道 WAV 音频并返回输出路径。
    /// </summary>
    /// <param name="inputPath">Zeayii 输入媒体绝对路径。</param>
    /// <param name="cancellationToken">Zeayii 取消令牌。</param>
    /// <returns>Zeayii 准备后的音频结果。</returns>
    Task<PreparedAudio> ExtractMonoWavAsync(string inputPath, CancellationToken cancellationToken);
}
