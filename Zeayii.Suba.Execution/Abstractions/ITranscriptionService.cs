using Zeayii.Suba.Core.Orchestration;

namespace Zeayii.Suba.Core.Abstractions;

/// <summary>
/// Zeayii 转写服务接口。
/// </summary>
public interface ITranscriptionService
{
    /// <summary>
    /// Zeayii 将音频段集合转写为字幕段集合。
    /// </summary>
    /// <param name="segments">Zeayii 音频段集合。</param>
    /// <param name="sampleRate">Zeayii 采样率。</param>
    /// <param name="cancellationToken">Zeayii 取消令牌。</param>
    /// <returns>Zeayii 字幕段集合。</returns>
    Task<IReadOnlyList<SubtitleSegment>> TranscribeAsync(IReadOnlyList<AudioSegment> segments, int sampleRate, CancellationToken cancellationToken);
}