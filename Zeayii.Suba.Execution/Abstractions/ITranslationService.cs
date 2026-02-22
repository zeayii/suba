using Zeayii.Suba.Core.Orchestration;

namespace Zeayii.Suba.Core.Abstractions;

/// <summary>
/// Zeayii 翻译服务接口。
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// Zeayii 对字幕段执行翻译。
    /// </summary>
    /// <param name="segments">Zeayii 待翻译字幕段集合。</param>
    /// <param name="prompt">Zeayii 提示词。</param>
    /// <param name="fixPrompt">Zeayii 修复提示词。</param>
    /// <param name="progressCallback">Zeayii 翻译进度回调（已完成条数，总条数）。</param>
    /// <param name="cancellationToken">Zeayii 取消令牌。</param>
    /// <returns>Zeayii 异步任务。</returns>
    Task TranslateAsync(
        IReadOnlyList<SubtitleSegment> segments,
        string prompt,
        string fixPrompt,
        Func<int, int, CancellationToken, Task>? progressCallback,
        CancellationToken cancellationToken
    );
}
