using Zeayii.Suba.Core.Configuration.Policies;
using Zeayii.Suba.Core.Contexts;

namespace Zeayii.Suba.Core.Abstractions;

/// <summary>
/// 字幕写入器。
/// </summary>
public interface ISubtitleWriter
{
    /// <summary>
    /// 写出字幕文件。
    /// </summary>
    /// <param name="taskContext">流水线状态。</param>
    /// <param name="language">语言标签。</param>
    /// <param name="formatPolicy">字幕格式策略。</param>
    /// <param name="translated">是否输出译文。</param>
    /// <param name="partial">是否写出中间产物（.partial）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task WriteAsync(TaskContext taskContext, string language, SubtitleFormatPolicy formatPolicy, bool translated, bool partial, CancellationToken cancellationToken);
}
