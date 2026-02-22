using Zeayii.Suba.Core.Configuration.Policies;

namespace Zeayii.Suba.Core.Configuration.Options;

/// <summary>
/// Zeayii 转写阶段配置。
/// </summary>
public sealed class TranscriptionOptions
{
    /// <summary>
    /// Zeayii 转写语言策略。
    /// </summary>
    public required LanguagePolicy LanguagePolicy { get; init; }

    /// <summary>
    /// Zeayii 固定转写语言标签（BCP 47），仅在固定策略下生效。
    /// </summary>
    public required string FixedLanguageTag { get; init; }

    /// <summary>
    /// Zeayii 输出字幕语言标签（BCP 47 或 auto）。
    /// </summary>
    public required string OutputLanguageTag { get; init; }

    /// <summary>
    /// Zeayii 模型执行语言短码（ISO 639-1），自动策略下为空字符串。
    /// </summary>
    public required string ModelLanguageCode { get; init; }

    /// <summary>
    /// Zeayii Whisper 无语音阈值。
    /// </summary>
    public required float NoSpeechThreshold { get; init; }

    /// <summary>
    /// Zeayii 解码最大新增 token 数。
    /// </summary>
    public required int MaxNewTokens { get; init; }

    /// <summary>
    /// Zeayii 解码温度。
    /// </summary>
    public required float Temperature { get; init; }

    /// <summary>
    /// Zeayii 解码 beam 大小（当前 ONNX 解码器仅在大于 1 时退化为 greedy）。
    /// </summary>
    public required int BeamSize { get; init; }

    /// <summary>
    /// Zeayii 采样候选数（当前 ONNX 解码器仅在大于 1 时退化为 greedy）。
    /// </summary>
    public required int BestOf { get; init; }

    /// <summary>
    /// Zeayii 长度惩罚（当前 ONNX 解码器保留参数但不改变 greedy 选择逻辑）。
    /// </summary>
    public required float LengthPenalty { get; init; }

    /// <summary>
    /// Zeayii 重复惩罚（当前 ONNX 解码器保留参数但不改变 greedy 选择逻辑）。
    /// </summary>
    public required float RepetitionPenalty { get; init; }

    /// <summary>
    /// Zeayii 是否抑制空白输出。
    /// </summary>
    public required bool SuppressBlank { get; init; }

    /// <summary>
    /// Zeayii 附加抑制 token 列表。
    /// </summary>
    public required IReadOnlyList<int> SuppressTokens { get; init; }

    /// <summary>
    /// Zeayii 是否禁用时间戳输出。
    /// </summary>
    public required bool WithoutTimestamps { get; init; }
}
