namespace Zeayii.Suba.Core.Orchestration;

/// <summary>
/// Zeayii VAD 配置模型。
/// </summary>
public sealed class VadSettings
{
    /// <summary>
    /// Zeayii 语音阈值。
    /// </summary>
    public required float Threshold { get; init; }

    /// <summary>
    /// Zeayii 最小静音时长（毫秒）。
    /// </summary>
    public required int MinSilenceMs { get; init; }

    /// <summary>
    /// Zeayii 最小语音时长（毫秒）。
    /// </summary>
    public required int MinSpeechMs { get; init; }

    /// <summary>
    /// Zeayii 最大语音时长（秒）。
    /// </summary>
    public required float MaxSpeechSeconds { get; init; }

    /// <summary>
    /// Zeayii 段边界补偿时长（毫秒）。
    /// </summary>
    public required int SpeechPadMs { get; init; }

    /// <summary>
    /// Zeayii 负阈值（空语音阈值）。
    /// </summary>
    public required float NegThreshold { get; init; }

    /// <summary>
    /// Zeayii 达到最大语音时用于优先切分的最小静音时长（毫秒）。
    /// </summary>
    public required float MinSilenceAtMaxSpeechMs { get; init; }

    /// <summary>
    /// Zeayii 达到最大语音时是否优先选择最大可用静音点切分。
    /// </summary>
    public required bool UseMaxPossibleSilenceAtMaxSpeech { get; init; }
}
