namespace Zeayii.Suba.Core.Configuration.Policies;

/// <summary>
/// Zeayii GPU 阶段冲突策略。
/// </summary>
[Flags]
public enum GpuConflictPolicy : byte
{
    /// <summary>
    /// Zeayii 不设置阶段冲突。
    /// </summary>
    None = 0,

    /// <summary>
    /// Zeayii 前处理与转录冲突。
    /// </summary>
    PreprocessVsTranscribe = 1,

    /// <summary>
    /// Zeayii 前处理与翻译冲突。
    /// </summary>
    PreprocessVsTranslate = 2,

    /// <summary>
    /// Zeayii 转录与翻译冲突。
    /// </summary>
    TranscribeVsTranslate = 4
}