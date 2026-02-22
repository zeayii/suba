namespace Zeayii.Suba.Core.Orchestration;

/// <summary>
/// Zeayii 字幕片段模型。
/// </summary>
public sealed class SubtitleSegment
{
    /// <summary>
    /// Zeayii 字幕序号。
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// Zeayii 起始时间（毫秒）。
    /// </summary>
    public required int StartMs { get; init; }

    /// <summary>
    /// Zeayii 结束时间（毫秒）。
    /// </summary>
    public required int EndMs { get; init; }

    /// <summary>
    /// Zeayii 说话人编号，未知时为 -1。
    /// </summary>
    public int Speaker { get; init; } = -1;

    /// <summary>
    /// Zeayii 原文文本。
    /// </summary>
    public required string OriginalText { get; init; }

    /// <summary>
    /// Zeayii 译文文本。
    /// </summary>
    public string? TranslatedText { get; set; }
}