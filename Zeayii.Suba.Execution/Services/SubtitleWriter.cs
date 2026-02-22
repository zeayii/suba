using System.Text;
using Zeayii.Suba.Core.Abstractions;
using Zeayii.Suba.Core.Configuration.Options;
using Zeayii.Suba.Core.Configuration.Policies;
using Zeayii.Suba.Core.Contexts;

namespace Zeayii.Suba.Core.Services;

/// <summary>
/// 字幕写入实现。
/// </summary>
/// <param name="options">Zeayii core options.</param>
/// <param name="artifactResolver">Zeayii subtitle artifact resolver.</param>
internal sealed class SubtitleWriter(SubaOptions options, SubtitleArtifactResolver artifactResolver) : ISubtitleWriter
{
    /// <summary>
    /// Zeayii core options.
    /// </summary>
    private readonly SubaOptions _options = options;

    /// <summary>
    /// Zeayii subtitle artifact resolver.
    /// </summary>
    private readonly SubtitleArtifactResolver _artifactResolver = artifactResolver;

    /// <summary>
    /// 写出字幕文件。
    /// </summary>
    /// <param name="taskContext">流水线状态。</param>
    /// <param name="language">语言标签。</param>
    /// <param name="formatPolicy">字幕格式策略。</param>
    /// <param name="translated">是否输出译文。</param>
    /// <param name="partial">是否写出中间产物（.partial）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task WriteAsync(
        TaskContext taskContext,
        string language,
        SubtitleFormatPolicy formatPolicy,
        bool translated,
        bool partial,
        CancellationToken cancellationToken
    )
    {
        var outputPath = partial
            ? _artifactResolver.BuildPartialOutputPath(taskContext.InputPath, language, formatPolicy)
            : _artifactResolver.BuildOutputPath(taskContext.InputPath, language, formatPolicy);
        if (!partial && _options.Runtime.ArtifactOverwritePolicy == ArtifactOverwritePolicy.SkipExisting && File.Exists(outputPath))
        {
            return;
        }

        var ordered = taskContext.SubtitleSegments.OrderBy(x => x.Index).ToList();
        var builder = new StringBuilder();
        if (formatPolicy == SubtitleFormatPolicy.Vtt)
        {
            builder.AppendLine("WEBVTT");
            builder.AppendLine();
            foreach (var segment in ordered)
            {
                var text = translated ? segment.TranslatedText : segment.OriginalText;
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                builder.AppendLine($"{ToVtt(segment.StartMs)} --> {ToVtt(segment.EndMs)}");
                builder.AppendLine(segment.Speaker < 0 ? text : $"{segment.Speaker}: {text}");
                builder.AppendLine();
            }
        }
        else
        {
            var i = 1;
            foreach (var segment in ordered)
            {
                var text = translated ? segment.TranslatedText : segment.OriginalText;
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                builder.AppendLine(i.ToString());
                builder.AppendLine($"{ToSrt(segment.StartMs)} --> {ToSrt(segment.EndMs)}");
                builder.AppendLine(segment.Speaker < 0 ? text : $"{segment.Speaker}: {text}");
                builder.AppendLine();
                i++;
            }
        }

        await File.WriteAllTextAsync(outputPath, builder.ToString(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 格式化 VTT 时间。
    /// </summary>
    /// <param name="ms">毫秒。</param>
    /// <returns>时间字符串。</returns>
    private static string ToVtt(int ms)
    {
        var ts = TimeSpan.FromMilliseconds(ms);
        return $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}";
    }

    /// <summary>
    /// 格式化 SRT 时间。
    /// </summary>
    /// <param name="ms">毫秒。</param>
    /// <returns>时间字符串。</returns>
    private static string ToSrt(int ms)
    {
        var ts = TimeSpan.FromMilliseconds(ms);
        return $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00},{ts.Milliseconds:000}";
    }
}
