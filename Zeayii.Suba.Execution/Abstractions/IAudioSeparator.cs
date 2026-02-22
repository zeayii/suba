using Zeayii.Suba.Core.Orchestration;

namespace Zeayii.Suba.Core.Abstractions;

/// <summary>
/// Zeayii 音频分离器接口，负责将重叠语音段分离为多路说话人音频。
/// </summary>
public interface IAudioSeparator
{
    /// <summary>
    /// Zeayii 对输入语音段执行分离并返回每个说话人的音频数组。
    /// </summary>
    /// <param name="segment">Zeayii 待分离的语音段。</param>
    /// <returns>Zeayii 分离后的多路音频集合。</returns>
    IReadOnlyList<float[]> Separate(AudioSegment segment);
}