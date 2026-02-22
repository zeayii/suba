using System.Globalization;
using Microsoft.Extensions.Logging;
using Zeayii.Suba.Core.Configuration.Policies;

namespace Zeayii.Suba.CommandLine.Options;

/// <summary>
/// Zeayii 命令行层应用配置（解析完成后的输入）。
/// </summary>
internal sealed class ApplicationOptions
{
    /// <summary>
    /// Zeayii 参数 TOML 文件路径。
    /// </summary>
    public required FileInfo ArgumentsTomlPath { get; init; }

    /// <summary>
    /// Zeayii FFmpeg 可执行文件路径。
    /// </summary>
    public required FileInfo FfmpegPath { get; init; }

    /// <summary>
    /// Zeayii 模型根目录。
    /// </summary>
    public required DirectoryInfo ModelsRoot { get; init; }

    /// <summary>
    /// Zeayii 缓存目录。
    /// </summary>
    public required DirectoryInfo CacheDirectory { get; init; }

    /// <summary>
    /// Zeayii 窗口日志等级。
    /// </summary>
    public required LogLevel ConsoleLogLevel { get; init; }

    /// <summary>
    /// Zeayii 文件日志等级。
    /// </summary>
    public required LogLevel FileLogLevel { get; init; }

    /// <summary>
    /// Zeayii 日志目录。
    /// </summary>
    public required DirectoryInfo LogDirectory { get; init; }

    /// <summary>
    /// Zeayii Silero VAD 阈值。
    /// </summary>
    public required float VadThreshold { get; init; }

    /// <summary>
    /// Zeayii Silero VAD 最小静音时长（毫秒）。
    /// </summary>
    public required int VadMinSilenceMs { get; init; }

    /// <summary>
    /// Zeayii Silero VAD 最小语音时长（毫秒）。
    /// </summary>
    public required int VadMinSpeechMs { get; init; }

    /// <summary>
    /// Zeayii Silero VAD 最大语音时长（秒）。
    /// </summary>
    public required float VadMaxSpeechSeconds { get; init; }

    /// <summary>
    /// Zeayii Silero VAD 段边界补偿时长（毫秒）。
    /// </summary>
    public required int VadSpeechPadMs { get; init; }

    /// <summary>
    /// Zeayii Silero VAD 负阈值（空语音阈值）。
    /// </summary>
    public required float VadNegThreshold { get; init; }

    /// <summary>
    /// Zeayii 达到最大语音时用于优先切分的最小静音时长（毫秒）。
    /// </summary>
    public required float VadMinSilenceAtMaxSpeechMs { get; init; }

    /// <summary>
    /// Zeayii 达到最大语音时是否优先选择最大可用静音点切分。
    /// </summary>
    public required bool VadUseMaxPossibleSilenceAtMaxSpeech { get; init; }

    /// <summary>
    /// Zeayii 重叠检测策略。
    /// </summary>
    public required StageSwitchPolicy OverlapDetectionPolicy { get; init; }

    /// <summary>
    /// Zeayii 分离后最小语音时长（毫秒）。
    /// </summary>
    public required int SeparatedVadMinSpeechMs { get; init; }

    /// <summary>
    /// Zeayii 分离后最大语音时长（毫秒）。
    /// </summary>
    public required int SeparatedVadMaxSpeechMs { get; init; }

    /// <summary>
    /// Zeayii 分离后二次 VAD 段边界补偿时长（毫秒）。
    /// </summary>
    public required int SeparatedVadSpeechPadMs { get; init; }

    /// <summary>
    /// Zeayii 分离后二次 VAD 负阈值（空语音阈值）。
    /// </summary>
    public required float SeparatedVadNegThreshold { get; init; }

    /// <summary>
    /// Zeayii 分离后二次 VAD 达到最大语音时用于优先切分的最小静音时长（毫秒）。
    /// </summary>
    public required float SeparatedVadMinSilenceAtMaxSpeechMs { get; init; }

    /// <summary>
    /// Zeayii 分离后二次 VAD 达到最大语音时是否优先选择最大可用静音点切分。
    /// </summary>
    public required bool SeparatedVadUseMaxPossibleSilenceAtMaxSpeech { get; init; }

    /// <summary>
    /// Zeayii 重叠检测起始阈值（高阈值）。
    /// </summary>
    public required float OverlapOnset { get; init; }

    /// <summary>
    /// Zeayii 重叠检测结束阈值（低阈值）。
    /// </summary>
    public required float OverlapOffset { get; init; }

    /// <summary>
    /// Zeayii 最短重叠持续时长（秒）。
    /// </summary>
    public required float OverlapMinDurationOnSeconds { get; init; }

    /// <summary>
    /// Zeayii 最短非重叠持续时长（秒）。
    /// </summary>
    public required float OverlapMinDurationOffSeconds { get; init; }

    /// <summary>
    /// Zeayii 是否启用 SepFormer 输出归一化。
    /// </summary>
    public required bool SepformerNormalizeOutput { get; init; }

    /// <summary>
    /// Zeayii 转写语言策略。
    /// </summary>
    public required LanguagePolicy TranscribeLanguagePolicy { get; init; }

    /// <summary>
    /// Zeayii 固定转写语言标签（BCP 47），仅在固定策略下生效。
    /// </summary>
    public required string TranscribeLanguageTag { get; init; }

    /// <summary>
    /// Zeayii Whisper 无语音阈值。
    /// </summary>
    public required float NoSpeechThreshold { get; init; }

    /// <summary>
    /// Zeayii 解码最大新增 token 数。
    /// </summary>
    public required int TranscribeMaxNewTokens { get; init; }

    /// <summary>
    /// Zeayii 解码温度。
    /// </summary>
    public required float TranscribeTemperature { get; init; }

    /// <summary>
    /// Zeayii 解码 beam 大小。
    /// </summary>
    public required int TranscribeBeamSize { get; init; }

    /// <summary>
    /// Zeayii 解码候选数量。
    /// </summary>
    public required int TranscribeBestOf { get; init; }

    /// <summary>
    /// Zeayii 长度惩罚。
    /// </summary>
    public required float TranscribeLengthPenalty { get; init; }

    /// <summary>
    /// Zeayii 重复惩罚。
    /// </summary>
    public required float TranscribeRepetitionPenalty { get; init; }

    /// <summary>
    /// Zeayii 是否抑制空白输出。
    /// </summary>
    public required bool TranscribeSuppressBlank { get; init; }

    /// <summary>
    /// Zeayii 额外抑制 token 列表。
    /// </summary>
    public required IReadOnlyList<int> TranscribeSuppressTokens { get; init; }

    /// <summary>
    /// Zeayii 是否禁用时间戳输出。
    /// </summary>
    public required bool TranscribeWithoutTimestamps { get; init; }

    /// <summary>
    /// Zeayii 翻译服务提供方策略。
    /// </summary>
    public required TranslationProviderPolicy TranslationProvider { get; init; }

    /// <summary>
    /// Zeayii 翻译语言（BCP 47）。
    /// </summary>
    public required CultureInfo TranslateLanguage { get; init; }

    /// <summary>
    /// Zeayii Ollama 服务地址。
    /// </summary>
    public required Uri OllamaBaseUrl { get; init; }

    /// <summary>
    /// Zeayii Ollama 模型名称。
    /// </summary>
    public required string OllamaModel { get; init; }

    /// <summary>
    /// Zeayii OpenAI 服务地址。
    /// </summary>
    public required Uri OpenAiBaseUrl { get; init; }

    /// <summary>
    /// Zeayii OpenAI API Key。
    /// </summary>
    public required string OpenAiApiKey { get; init; }

    /// <summary>
    /// Zeayii OpenAI 模型名称。
    /// </summary>
    public required string OpenAiModel { get; init; }

    /// <summary>
    /// Zeayii 翻译响应模式策略。
    /// </summary>
    public required TranslationResponseMode TranslateResponseMode { get; init; }

    /// <summary>
    /// Zeayii 翻译上下文窗口大小。
    /// </summary>
    public required int TranslateContextQueueSize { get; init; }

    /// <summary>
    /// Zeayii 翻译上下文断链阈值（毫秒）。
    /// </summary>
    public required int TranslateContextGapMs { get; init; }

    /// <summary>
    /// Zeayii 翻译中间字幕写出间隔（条）；0 表示仅最终写出。
    /// </summary>
    public required int TranslatePartialWriteInterval { get; init; }

    /// <summary>
    /// Zeayii 字幕格式策略。
    /// </summary>
    public required SubtitleFormatPolicy SubtitleFormatPolicy { get; init; }

    /// <summary>
    /// Zeayii 最大并发度。
    /// </summary>
    public required int MaxDegreeOfParallelism { get; init; }

    /// <summary>
    /// Zeayii 命令超时（秒）。
    /// </summary>
    public required int CommandTimeoutSeconds { get; init; }

    /// <summary>
    /// Zeayii 前处理阶段设备。
    /// </summary>
    public required ExecutionDevicePolicy PreprocessDevice { get; init; }

    /// <summary>
    /// Zeayii 前处理阶段并发数量。
    /// </summary>
    public required int PreprocessParallelism { get; init; }

    /// <summary>
    /// Zeayii 转录阶段设备。
    /// </summary>
    public required ExecutionDevicePolicy TranscribeDevice { get; init; }

    /// <summary>
    /// Zeayii 转录阶段并发数量。
    /// </summary>
    public required int TranscribeParallelism { get; init; }

    /// <summary>
    /// Zeayii 翻译阶段设备。
    /// </summary>
    public required ExecutionDevicePolicy TranslateDevice { get; init; }

    /// <summary>
    /// Zeayii 翻译阶段并发数量。
    /// </summary>
    public required int TranslateParallelism { get; init; }

    /// <summary>
    /// Zeayii 翻译执行模式。
    /// </summary>
    public required TranslationExecutionModePolicy TranslationExecutionMode { get; init; }

    /// <summary>
    /// Zeayii GPU 冲突策略。
    /// </summary>
    public required GpuConflictPolicy GpuConflictPolicy { get; init; }

    /// <summary>
    /// Zeayii 字幕产物覆盖策略。
    /// </summary>
    public required ArtifactOverwritePolicy ArtifactOverwritePolicy { get; init; }
}
