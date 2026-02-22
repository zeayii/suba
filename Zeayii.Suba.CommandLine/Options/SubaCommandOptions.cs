using System.CommandLine;
using Microsoft.Extensions.Logging;
using Zeayii.Suba.Core.Configuration.Policies;

namespace Zeayii.Suba.CommandLine.Options;

/// <summary>
/// Zeayii 命令行参数定义集合。
/// </summary>
internal sealed class SubaCommandOptions
{
    /// <summary>
    /// Zeayii 参数 TOML 文件路径参数。
    /// </summary>
    public required Argument<FileInfo> ArgumentsTomlPathArgument { get; init; }

    /// <summary>
    /// Zeayii FFmpeg 路径选项。
    /// </summary>
    public required Option<FileInfo> FfmpegPathOption { get; init; }

    /// <summary>
    /// Zeayii 模型根目录选项。
    /// </summary>
    public required Option<DirectoryInfo> ModelsRootOption { get; init; }

    /// <summary>
    /// Zeayii 缓存目录选项。
    /// </summary>
    public required Option<DirectoryInfo> CacheDirectoryOption { get; init; }

    /// <summary>
    /// Zeayii 窗口日志等级选项。
    /// </summary>
    public required Option<LogLevel> ConsoleLogLevelOption { get; init; }

    /// <summary>
    /// Zeayii 文件日志等级选项。
    /// </summary>
    public required Option<LogLevel> FileLogLevelOption { get; init; }

    /// <summary>
    /// Zeayii 日志目录选项。
    /// </summary>
    public required Option<DirectoryInfo> LogDirectoryOption { get; init; }

    /// <summary>
    /// Zeayii VAD 阈值选项。
    /// </summary>
    public required Option<float> VadThresholdOption { get; init; }

    /// <summary>
    /// Zeayii VAD 最小静音时长选项。
    /// </summary>
    public required Option<int> VadMinSilenceMsOption { get; init; }

    /// <summary>
    /// Zeayii VAD 最小语音时长选项。
    /// </summary>
    public required Option<int> VadMinSpeechMsOption { get; init; }

    /// <summary>
    /// Zeayii VAD 最大语音时长选项。
    /// </summary>
    public required Option<float> VadMaxSpeechSecondsOption { get; init; }

    /// <summary>
    /// Zeayii VAD 段边界补偿时长选项。
    /// </summary>
    public required Option<int> VadSpeechPadMsOption { get; init; }

    /// <summary>
    /// Zeayii VAD 负阈值选项。
    /// </summary>
    public required Option<float> VadNegThresholdOption { get; init; }

    /// <summary>
    /// Zeayii VAD 达到最大语音时最小静音时长选项。
    /// </summary>
    public required Option<float> VadMinSilenceAtMaxSpeechMsOption { get; init; }

    /// <summary>
    /// Zeayii VAD 达到最大语音时是否优先使用最大静音点选项。
    /// </summary>
    public required Option<bool> VadUseMaxPossibleSilenceAtMaxSpeechOption { get; init; }

    /// <summary>
    /// Zeayii 重叠检测策略选项。
    /// </summary>
    public required Option<StageSwitchPolicy> OverlapDetectionPolicyOption { get; init; }

    /// <summary>
    /// Zeayii 分离后最小语音时长选项。
    /// </summary>
    public required Option<int> SeparatedVadMinSpeechMsOption { get; init; }

    /// <summary>
    /// Zeayii 分离后最大语音时长选项。
    /// </summary>
    public required Option<int> SeparatedVadMaxSpeechMsOption { get; init; }

    /// <summary>
    /// Zeayii 分离后二次 VAD 段边界补偿时长选项。
    /// </summary>
    public required Option<int> SeparatedVadSpeechPadMsOption { get; init; }

    /// <summary>
    /// Zeayii 分离后二次 VAD 负阈值选项。
    /// </summary>
    public required Option<float> SeparatedVadNegThresholdOption { get; init; }

    /// <summary>
    /// Zeayii 分离后二次 VAD 达到最大语音时最小静音时长选项。
    /// </summary>
    public required Option<float> SeparatedVadMinSilenceAtMaxSpeechMsOption { get; init; }

    /// <summary>
    /// Zeayii 分离后二次 VAD 达到最大语音时是否优先使用最大静音点选项。
    /// </summary>
    public required Option<bool> SeparatedVadUseMaxPossibleSilenceAtMaxSpeechOption { get; init; }

    /// <summary>
    /// Zeayii 重叠检测起始阈值选项。
    /// </summary>
    public required Option<float> OverlapOnsetOption { get; init; }

    /// <summary>
    /// Zeayii 重叠检测结束阈值选项。
    /// </summary>
    public required Option<float> OverlapOffsetOption { get; init; }

    /// <summary>
    /// Zeayii 最短重叠时长选项。
    /// </summary>
    public required Option<float> OverlapMinDurationOnSecondsOption { get; init; }

    /// <summary>
    /// Zeayii 最短非重叠时长选项。
    /// </summary>
    public required Option<float> OverlapMinDurationOffSecondsOption { get; init; }

    /// <summary>
    /// Zeayii SepFormer 输出归一化选项。
    /// </summary>
    public required Option<bool> SepformerNormalizeOutputOption { get; init; }

    /// <summary>
    /// Zeayii 转写语言策略选项。
    /// </summary>
    public required Option<LanguagePolicy> TranscribeLanguagePolicyOption { get; init; }

    /// <summary>
    /// Zeayii 转写语言选项。
    /// </summary>
    public required Option<string> TranscribeLanguageOption { get; init; }

    /// <summary>
    /// Zeayii Whisper 无语音阈值选项。
    /// </summary>
    public required Option<float> NoSpeechThresholdOption { get; init; }

    /// <summary>
    /// Zeayii 转写最大新增 token 选项。
    /// </summary>
    public required Option<int> TranscribeMaxNewTokensOption { get; init; }

    /// <summary>
    /// Zeayii 转写温度选项。
    /// </summary>
    public required Option<float> TranscribeTemperatureOption { get; init; }

    /// <summary>
    /// Zeayii 转写 beam 选项。
    /// </summary>
    public required Option<int> TranscribeBeamSizeOption { get; init; }

    /// <summary>
    /// Zeayii 转写 best-of 选项。
    /// </summary>
    public required Option<int> TranscribeBestOfOption { get; init; }

    /// <summary>
    /// Zeayii 转写长度惩罚选项。
    /// </summary>
    public required Option<float> TranscribeLengthPenaltyOption { get; init; }

    /// <summary>
    /// Zeayii 转写重复惩罚选项。
    /// </summary>
    public required Option<float> TranscribeRepetitionPenaltyOption { get; init; }

    /// <summary>
    /// Zeayii 转写抑制空白选项。
    /// </summary>
    public required Option<bool> TranscribeSuppressBlankOption { get; init; }

    /// <summary>
    /// Zeayii 转写抑制 token 选项。
    /// </summary>
    public required Option<int[]> TranscribeSuppressTokensOption { get; init; }

    /// <summary>
    /// Zeayii 转写禁用时间戳选项。
    /// </summary>
    public required Option<bool> TranscribeWithoutTimestampsOption { get; init; }

    /// <summary>
    /// Zeayii 翻译服务提供方策略选项。
    /// </summary>
    public required Option<TranslationProviderPolicy> TranslationProviderOption { get; init; }

    /// <summary>
    /// Zeayii 翻译语言选项。
    /// </summary>
    public required Option<string> TranslateLanguageOption { get; init; }

    /// <summary>
    /// Zeayii Ollama 地址选项。
    /// </summary>
    public required Option<Uri> OllamaBaseUrlOption { get; init; }

    /// <summary>
    /// Zeayii Ollama 模型选项。
    /// </summary>
    public required Option<string> OllamaModelOption { get; init; }

    /// <summary>
    /// Zeayii OpenAI 地址选项。
    /// </summary>
    public required Option<Uri> OpenAiBaseUrlOption { get; init; }

    /// <summary>
    /// Zeayii OpenAI API Key 选项。
    /// </summary>
    public required Option<string> OpenAiApiKeyOption { get; init; }

    /// <summary>
    /// Zeayii OpenAI 模型选项。
    /// </summary>
    public required Option<string> OpenAiModelOption { get; init; }

    /// <summary>
    /// Zeayii 翻译响应模式策略选项。
    /// </summary>
    public required Option<TranslationResponseMode> TranslateResponseModeOption { get; init; }

    /// <summary>
    /// Zeayii 翻译上下文窗口选项。
    /// </summary>
    public required Option<int> TranslateContextQueueSizeOption { get; init; }

    /// <summary>
    /// Zeayii 翻译上下文断链阈值选项。
    /// </summary>
    public required Option<int> TranslateContextGapMsOption { get; init; }

    /// <summary>
    /// Zeayii 翻译中间字幕写出间隔选项。
    /// </summary>
    public required Option<int> TranslatePartialWriteIntervalOption { get; init; }

    /// <summary>
    /// Zeayii 字幕格式策略选项。
    /// </summary>
    public required Option<SubtitleFormatPolicy> SubtitleFormatPolicyOption { get; init; }

    /// <summary>
    /// Zeayii 最大并发度选项。
    /// </summary>
    public required Option<int> MaxDegreeOfParallelismOption { get; init; }

    /// <summary>
    /// Zeayii 命令超时选项。
    /// </summary>
    public required Option<int> CommandTimeoutSecondsOption { get; init; }

    /// <summary>
    /// Zeayii 前处理阶段设备选项。
    /// </summary>
    public required Option<ExecutionDevicePolicy> PreprocessDeviceOption { get; init; }

    /// <summary>
    /// Zeayii 前处理阶段并发选项。
    /// </summary>
    public required Option<int> PreprocessParallelismOption { get; init; }

    /// <summary>
    /// Zeayii 转录阶段设备选项。
    /// </summary>
    public required Option<ExecutionDevicePolicy> TranscribeDeviceOption { get; init; }

    /// <summary>
    /// Zeayii 转录阶段并发选项。
    /// </summary>
    public required Option<int> TranscribeParallelismOption { get; init; }

    /// <summary>
    /// Zeayii 翻译阶段设备选项。
    /// </summary>
    public required Option<ExecutionDevicePolicy> TranslateDeviceOption { get; init; }

    /// <summary>
    /// Zeayii 翻译阶段并发选项。
    /// </summary>
    public required Option<int> TranslateParallelismOption { get; init; }

    /// <summary>
    /// Zeayii 翻译执行模式选项。
    /// </summary>
    public required Option<TranslationExecutionModePolicy> TranslationExecutionModeOption { get; init; }

    /// <summary>
    /// Zeayii GPU 冲突策略选项。
    /// </summary>
    public required Option<GpuConflictPolicy> GpuConflictPolicyOption { get; init; }

    /// <summary>
    /// Zeayii 字幕产物覆盖策略选项。
    /// </summary>
    public required Option<ArtifactOverwritePolicy> ArtifactOverwritePolicyOption { get; init; }
}
