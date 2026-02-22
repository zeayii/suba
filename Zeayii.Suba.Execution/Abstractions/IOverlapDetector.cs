using Zeayii.Suba.Core.Orchestration;

namespace Zeayii.Suba.Core.Abstractions;

/// <summary>
/// Zeayii 重叠检测接口，负责识别语音段中是否存在多说话人重叠。
/// </summary>
public interface IOverlapDetector
{
    /// <summary>
    /// Zeayii 判断语音段是否存在重叠说话。
    /// </summary>
    /// <param name="segment">Zeayii 语音段。</param>
    /// <param name="sampleRate">Zeayii 采样率。</param>
    /// <returns>Zeayii 是否存在重叠语音。</returns>
    bool HasOverlap(AudioSegment segment, int sampleRate);
}