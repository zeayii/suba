namespace Zeayii.Suba.Core.Configuration.Options;

/// <summary>
/// Zeayii 分离后二次 VAD 阶段配置。
/// </summary>
public sealed class SeparationOptions
{
    /// <summary>
    /// Zeayii 二次 VAD 最小语音时长（毫秒）。
    /// </summary>
    public required int SeparatedVadMinSpeechMs { get; init; }

    /// <summary>
    /// Zeayii 二次 VAD 最大语音时长（毫秒）。
    /// </summary>
    public required int SeparatedVadMaxSpeechMs { get; init; }

    /// <summary>
    /// Zeayii 二次 VAD 段边界补偿时长（毫秒）。
    /// </summary>
    public required int SeparatedVadSpeechPadMs { get; init; }

    /// <summary>
    /// Zeayii 二次 VAD 负阈值（空语音阈值）。
    /// </summary>
    public required float SeparatedVadNegThreshold { get; init; }

    /// <summary>
    /// Zeayii 二次 VAD 达到最大语音时用于优先切分的最小静音时长（毫秒）。
    /// </summary>
    public required float SeparatedVadMinSilenceAtMaxSpeechMs { get; init; }

    /// <summary>
    /// Zeayii 二次 VAD 达到最大语音时是否优先选择最大可用静音点切分。
    /// </summary>
    public required bool SeparatedVadUseMaxPossibleSilenceAtMaxSpeech { get; init; }
}
