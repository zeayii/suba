namespace Zeayii.Suba.Core.Configuration.Policies;

/// <summary>
/// Zeayii 翻译执行模式策略。
/// </summary>
public enum TranslationExecutionModePolicy : byte
{
    /// <summary>
    /// Zeayii 每个任务转录后立即翻译。
    /// </summary>
    PerTask = 1,

    /// <summary>
    /// Zeayii 全量转录完成后批量翻译。
    /// </summary>
    BatchAfterTranscription = 2
}