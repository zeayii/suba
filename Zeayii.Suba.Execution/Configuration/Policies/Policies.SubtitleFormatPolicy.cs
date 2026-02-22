namespace Zeayii.Suba.Core.Configuration.Policies;

/// <summary>
/// 字幕文件格式策略。
/// </summary>
public enum SubtitleFormatPolicy : byte
{
    /// <summary>
    /// WebVTT 文本字幕格式。
    /// </summary>
    Vtt = 1,

    /// <summary>
    /// SubRip 文本字幕格式。
    /// </summary>
    Srt = 2
}