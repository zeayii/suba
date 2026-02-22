using Zeayii.Suba.Core.Orchestration;

namespace Zeayii.Suba.Core.Abstractions;

/// <summary>
/// Zeayii 语音活动检测服务接口。
/// </summary>
public interface IVadService
{
    /// <summary>
    /// Zeayii 对音频执行 VAD，输出语音段集合。
    /// </summary>
    /// <param name="audio">Zeayii 原始单声道音频采样。</param>
    /// <param name="sampleRate">Zeayii 采样率。</param>
    /// <param name="settings">Zeayii VAD 参数。</param>
    /// <returns>Zeayii 语音段集合。</returns>
    IReadOnlyList<AudioSegment> DetectSpeech(float[] audio, int sampleRate, VadSettings settings);
}