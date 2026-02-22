using System.Globalization;
using Microsoft.Extensions.Logging;
using Zeayii.Suba.Core.Configuration.Policies;

namespace Zeayii.Suba.Core.Configuration.Options;

/// <summary>
/// Zeayii 核心配置构建器。
/// </summary>
public sealed class SubaOptionsBuilder
{
    /// <summary>
    /// Zeayii 构建中间状态。
    /// </summary>
    private readonly MutableState _state = new();

    /// <summary>
    /// Zeayii 创建构建器实例。
    /// </summary>
    /// <returns>Zeayii 构建器。</returns>
    public static SubaOptionsBuilder Create() => new();

    /// <summary>
    /// Zeayii 设置模型根目录。
    /// </summary>
    /// <param name="value">Zeayii 模型根目录。</param>
    /// <returns>Zeayii 构建器。</returns>
    public SubaOptionsBuilder SetModelsRoot(string value)
    {
        _state.ModelsRoot = value;
        return this;
    }

    /// <summary>
    /// Zeayii 设置字幕格式策略。
    /// </summary>
    /// <param name="value">Zeayii 字幕格式策略。</param>
    /// <returns>Zeayii 构建器。</returns>
    public SubaOptionsBuilder SetSubtitleFormatPolicy(SubtitleFormatPolicy value)
    {
        _state.SubtitleFormatPolicy = value;
        return this;
    }

    /// <summary>
    /// Zeayii 设置音频提取参数。
    /// </summary>
    /// <param name="ffmpegPath">Zeayii FFmpeg 路径。</param>
    /// <param name="cacheDirectory">Zeayii 缓存目录。</param>
    /// <returns>Zeayii 构建器。</returns>
    public SubaOptionsBuilder SetAudioExtract(string ffmpegPath, string cacheDirectory)
    {
        _state.FfmpegPath = ffmpegPath;
        _state.CacheDirectory = cacheDirectory;
        return this;
    }

    /// <summary>
    /// Zeayii 设置运行参数。
    /// </summary>
    /// <param name="maxDegreeOfParallelism">Zeayii 最大并发度。</param>
    /// <param name="commandTimeoutSeconds">Zeayii 命令超时秒数。</param>
    /// <param name="preprocessDevice">Zeayii 前处理阶段设备。</param>
    /// <param name="preprocessParallelism">Zeayii 前处理阶段并发数量。</param>
    /// <param name="transcribeDevice">Zeayii 转录阶段设备。</param>
    /// <param name="transcribeParallelism">Zeayii 转录阶段并发数量。</param>
    /// <param name="translateDevice">Zeayii 翻译阶段设备。</param>
    /// <param name="translateParallelism">Zeayii 翻译阶段并发数量。</param>
    /// <param name="translationExecutionMode">Zeayii 翻译执行模式。</param>
    /// <param name="gpuConflictPolicy">Zeayii GPU 冲突策略。</param>
    /// <param name="artifactOverwritePolicy">Zeayii 字幕产物覆盖策略。</param>
    /// <returns>Zeayii 构建器。</returns>
    public SubaOptionsBuilder SetRuntime(
        int maxDegreeOfParallelism,
        int commandTimeoutSeconds,
        ExecutionDevicePolicy preprocessDevice,
        int preprocessParallelism,
        ExecutionDevicePolicy transcribeDevice,
        int transcribeParallelism,
        ExecutionDevicePolicy translateDevice,
        int translateParallelism,
        TranslationExecutionModePolicy translationExecutionMode,
        GpuConflictPolicy gpuConflictPolicy,
        ArtifactOverwritePolicy artifactOverwritePolicy)
    {
        _state.MaxDegreeOfParallelism = maxDegreeOfParallelism;
        _state.CommandTimeoutSeconds = commandTimeoutSeconds;
        _state.PreprocessDevice = preprocessDevice;
        _state.PreprocessParallelism = preprocessParallelism;
        _state.TranscribeDevice = transcribeDevice;
        _state.TranscribeParallelism = transcribeParallelism;
        _state.TranslateDevice = translateDevice;
        _state.TranslateParallelism = translateParallelism;
        _state.TranslationExecutionMode = translationExecutionMode;
        _state.GpuConflictPolicy = gpuConflictPolicy;
        _state.ArtifactOverwritePolicy = artifactOverwritePolicy;
        return this;
    }

    /// <summary>
    /// Zeayii 设置日志参数。
    /// </summary>
    /// <param name="consoleLogLevel">Zeayii 窗口日志等级。</param>
    /// <param name="fileLogLevel">Zeayii 文件日志等级。</param>
    /// <param name="logDirectory">Zeayii 日志目录。</param>
    /// <returns>Zeayii 构建器。</returns>
    public SubaOptionsBuilder SetLogging(LogLevel consoleLogLevel, LogLevel fileLogLevel, string logDirectory)
    {
        _state.ConsoleLogLevel = consoleLogLevel;
        _state.FileLogLevel = fileLogLevel;
        _state.LogDirectory = logDirectory;
        return this;
    }

    /// <summary>
    /// Zeayii 设置 VAD 参数。
    /// </summary>
    /// <param name="threshold">Zeayii 语音阈值。</param>
    /// <param name="minSilenceMs">Zeayii 最小静音时长毫秒。</param>
    /// <param name="minSpeechMs">Zeayii 最小语音时长毫秒。</param>
    /// <param name="maxSpeechSeconds">Zeayii 最大语音时长秒数。</param>
    /// <param name="speechPadMs">Zeayii 段边界补偿时长毫秒。</param>
    /// <param name="negThreshold">Zeayii 负阈值。</param>
    /// <param name="minSilenceAtMaxSpeechMs">Zeayii 达到最大语音时使用的最小静音时长毫秒。</param>
    /// <param name="useMaxPossibleSilenceAtMaxSpeech">Zeayii 达到最大语音时是否优先使用最大静音点。</param>
    /// <returns>Zeayii 构建器。</returns>
    public SubaOptionsBuilder SetVad(
        float threshold,
        int minSilenceMs,
        int minSpeechMs,
        float maxSpeechSeconds,
        int speechPadMs,
        float negThreshold,
        float minSilenceAtMaxSpeechMs,
        bool useMaxPossibleSilenceAtMaxSpeech)
    {
        _state.VadThreshold = threshold;
        _state.VadMinSilenceMs = minSilenceMs;
        _state.VadMinSpeechMs = minSpeechMs;
        _state.VadMaxSpeechSeconds = maxSpeechSeconds;
        _state.VadSpeechPadMs = speechPadMs;
        _state.VadNegThreshold = negThreshold;
        _state.VadMinSilenceAtMaxSpeechMs = minSilenceAtMaxSpeechMs;
        _state.VadUseMaxPossibleSilenceAtMaxSpeech = useMaxPossibleSilenceAtMaxSpeech;
        return this;
    }

    /// <summary>
    /// Zeayii 设置重叠检测策略。
    /// </summary>
    /// <param name="value">Zeayii 重叠检测策略。</param>
    /// <param name="onset">Zeayii 重叠起始阈值。</param>
    /// <param name="offset">Zeayii 重叠结束阈值。</param>
    /// <param name="minDurationOnSeconds">Zeayii 最短重叠时长秒数。</param>
    /// <param name="minDurationOffSeconds">Zeayii 最短非重叠时长秒数。</param>
    /// <returns>Zeayii 构建器。</returns>
    public SubaOptionsBuilder SetOverlapPolicy(
        StageSwitchPolicy value,
        float onset,
        float offset,
        float minDurationOnSeconds,
        float minDurationOffSeconds)
    {
        _state.OverlapDetectionPolicy = value;
        _state.OverlapOnset = onset;
        _state.OverlapOffset = offset;
        _state.OverlapMinDurationOnSeconds = minDurationOnSeconds;
        _state.OverlapMinDurationOffSeconds = minDurationOffSeconds;
        return this;
    }

    /// <summary>
    /// Zeayii 设置分离后二次 VAD 参数。
    /// </summary>
    /// <param name="minSpeechMs">Zeayii 最小语音时长毫秒。</param>
    /// <param name="maxSpeechMs">Zeayii 最大语音时长毫秒。</param>
    /// <param name="speechPadMs">Zeayii 段边界补偿时长毫秒。</param>
    /// <param name="negThreshold">Zeayii 负阈值。</param>
    /// <param name="minSilenceAtMaxSpeechMs">Zeayii 达到最大语音时使用的最小静音时长毫秒。</param>
    /// <param name="useMaxPossibleSilenceAtMaxSpeech">Zeayii 达到最大语音时是否优先使用最大静音点。</param>
    /// <returns>Zeayii 构建器。</returns>
    public SubaOptionsBuilder SetSeparatedVad(
        int minSpeechMs,
        int maxSpeechMs,
        int speechPadMs,
        float negThreshold,
        float minSilenceAtMaxSpeechMs,
        bool useMaxPossibleSilenceAtMaxSpeech)
    {
        _state.SeparatedVadMinSpeechMs = minSpeechMs;
        _state.SeparatedVadMaxSpeechMs = maxSpeechMs;
        _state.SeparatedVadSpeechPadMs = speechPadMs;
        _state.SeparatedVadNegThreshold = negThreshold;
        _state.SeparatedVadMinSilenceAtMaxSpeechMs = minSilenceAtMaxSpeechMs;
        _state.SeparatedVadUseMaxPossibleSilenceAtMaxSpeech = useMaxPossibleSilenceAtMaxSpeech;
        return this;
    }

    /// <summary>
    /// Zeayii 设置转写参数。
    /// </summary>
    /// <param name="languagePolicy">Zeayii 转写语言策略。</param>
    /// <param name="fixedLanguageTag">Zeayii 固定转写语言标签。</param>
    /// <param name="outputLanguageTag">Zeayii 输出字幕语言标签。</param>
    /// <param name="modelLanguageCode">Zeayii 模型执行语言短码。</param>
    /// <param name="noSpeechThreshold">Zeayii 无语音阈值。</param>
    /// <param name="maxNewTokens">Zeayii 转写最大新增 token。</param>
    /// <param name="temperature">Zeayii 转写温度。</param>
    /// <param name="beamSize">Zeayii 转写 beam 大小。</param>
    /// <param name="bestOf">Zeayii 转写 best-of 候选数。</param>
    /// <param name="lengthPenalty">Zeayii 转写长度惩罚。</param>
    /// <param name="repetitionPenalty">Zeayii 转写重复惩罚。</param>
    /// <param name="suppressBlank">Zeayii 转写是否抑制空白。</param>
    /// <param name="suppressTokens">Zeayii 转写抑制 token 列表。</param>
    /// <param name="withoutTimestamps">Zeayii 转写是否禁用时间戳。</param>
    /// <returns>Zeayii 构建器。</returns>
    public SubaOptionsBuilder SetTranscription(
        LanguagePolicy languagePolicy,
        string fixedLanguageTag,
        string outputLanguageTag,
        string modelLanguageCode,
        float noSpeechThreshold,
        int maxNewTokens,
        float temperature,
        int beamSize,
        int bestOf,
        float lengthPenalty,
        float repetitionPenalty,
        bool suppressBlank,
        IReadOnlyList<int> suppressTokens,
        bool withoutTimestamps)
    {
        _state.TranscribeLanguagePolicy = languagePolicy;
        _state.TranscribeFixedLanguageTag = fixedLanguageTag;
        _state.TranscribeOutputLanguageTag = outputLanguageTag;
        _state.TranscribeModelLanguageCode = modelLanguageCode;
        _state.NoSpeechThreshold = noSpeechThreshold;
        _state.TranscribeMaxNewTokens = maxNewTokens;
        _state.TranscribeTemperature = temperature;
        _state.TranscribeBeamSize = beamSize;
        _state.TranscribeBestOf = bestOf;
        _state.TranscribeLengthPenalty = lengthPenalty;
        _state.TranscribeRepetitionPenalty = repetitionPenalty;
        _state.TranscribeSuppressBlank = suppressBlank;
        _state.TranscribeSuppressTokens = suppressTokens;
        _state.TranscribeWithoutTimestamps = withoutTimestamps;
        return this;
    }

    /// <summary>
    /// Zeayii 设置 SepFormer 参数。
    /// </summary>
    /// <param name="normalizeOutput">Zeayii 是否归一化分离输出。</param>
    /// <returns>Zeayii 构建器。</returns>
    public SubaOptionsBuilder SetSepformer(bool normalizeOutput)
    {
        _state.SepformerNormalizeOutput = normalizeOutput;
        return this;
    }

    /// <summary>
    /// Zeayii 设置翻译参数。
    /// </summary>
    /// <param name="provider">Zeayii 翻译服务提供方策略。</param>
    /// <param name="language">Zeayii 译文语言。</param>
    /// <param name="ollamaBaseUrl">Zeayii Ollama 地址。</param>
    /// <param name="ollamaModel">Zeayii Ollama 模型。</param>
    /// <param name="openAiBaseUrl">Zeayii OpenAI 地址。</param>
    /// <param name="openAiApiKey">Zeayii OpenAI API Key。</param>
    /// <param name="openAiModel">Zeayii OpenAI 模型。</param>
    /// <param name="responseMode">Zeayii 翻译响应模式。</param>
    /// <param name="contextQueueSize">Zeayii 上下文窗口大小。</param>
    /// <param name="contextGapMs">Zeayii 上下文断链阈值毫秒。</param>
    /// <param name="partialWriteInterval">Zeayii 翻译中间字幕写出间隔。</param>
    /// <returns>Zeayii 构建器。</returns>
    public SubaOptionsBuilder SetTranslation(
        TranslationProviderPolicy provider,
        CultureInfo language,
        string ollamaBaseUrl,
        string ollamaModel,
        string openAiBaseUrl,
        string openAiApiKey,
        string openAiModel,
        TranslationResponseMode responseMode,
        int contextQueueSize,
        int contextGapMs,
        int partialWriteInterval)
    {
        _state.TranslationProvider = provider;
        _state.TranslateLanguage = language;
        _state.OllamaBaseUrl = ollamaBaseUrl;
        _state.OllamaModel = ollamaModel;
        _state.OpenAiBaseUrl = openAiBaseUrl;
        _state.OpenAiApiKey = openAiApiKey;
        _state.OpenAiModel = openAiModel;
        _state.TranslationResponseMode = responseMode;
        _state.TranslateContextQueueSize = contextQueueSize;
        _state.TranslateContextGapMs = contextGapMs;
        _state.TranslatePartialWriteInterval = partialWriteInterval;
        return this;
    }

    /// <summary>
    /// Zeayii 设置模型解析后的路径。
    /// </summary>
    /// <param name="segmentationModelPath">Zeayii 分割模型路径。</param>
    /// <param name="sepformerModelPath">Zeayii 分离模型路径。</param>
    /// <param name="whisperModelRoot">Zeayii Whisper 模型目录。</param>
    /// <returns>Zeayii 构建器。</returns>
    public SubaOptionsBuilder SetModelResolvedPaths(string segmentationModelPath, string sepformerModelPath, string whisperModelRoot)
    {
        _state.SegmentationModelPath = segmentationModelPath;
        _state.SepformerModelPath = sepformerModelPath;
        _state.WhisperModelRoot = whisperModelRoot;
        return this;
    }

    /// <summary>
    /// Zeayii 构建最终配置。
    /// </summary>
    /// <returns>Zeayii 核心配置。</returns>
    public SubaOptions Build()
    {
        return new SubaOptions
        {
            ModelsRoot = _state.ModelsRoot,
            SegmentationModelPath = _state.SegmentationModelPath,
            SepformerModelPath = _state.SepformerModelPath,
            WhisperModelRoot = _state.WhisperModelRoot,
            SubtitleFormatPolicy = _state.SubtitleFormatPolicy,
            AudioExtract = new AudioExtractOptions
            {
                FfmpegPath = _state.FfmpegPath,
                CacheDirectory = _state.CacheDirectory
            },
            Runtime = new RuntimeOptions
            {
                MaxDegreeOfParallelism = _state.MaxDegreeOfParallelism,
                CommandTimeoutSeconds = _state.CommandTimeoutSeconds,
                Preprocess = new StageExecutionOptions
                {
                    Device = _state.PreprocessDevice,
                    Parallelism = _state.PreprocessParallelism
                },
                Transcribe = new StageExecutionOptions
                {
                    Device = _state.TranscribeDevice,
                    Parallelism = _state.TranscribeParallelism
                },
                Translate = new StageExecutionOptions
                {
                    Device = _state.TranslateDevice,
                    Parallelism = _state.TranslateParallelism
                },
                TranslationExecutionMode = _state.TranslationExecutionMode,
                GpuConflictPolicy = _state.GpuConflictPolicy,
                ArtifactOverwritePolicy = _state.ArtifactOverwritePolicy
            },
            Logging = new LoggingOptions
            {
                ConsoleLogLevel = _state.ConsoleLogLevel,
                FileLogLevel = _state.FileLogLevel,
                LogDirectory = _state.LogDirectory
            },
            Vad = new VadOptions
            {
                Threshold = _state.VadThreshold,
                MinSilenceMs = _state.VadMinSilenceMs,
                MinSpeechMs = _state.VadMinSpeechMs,
                MaxSpeechSeconds = _state.VadMaxSpeechSeconds,
                SpeechPadMs = _state.VadSpeechPadMs,
                NegThreshold = _state.VadNegThreshold,
                MinSilenceAtMaxSpeechMs = _state.VadMinSilenceAtMaxSpeechMs,
                UseMaxPossibleSilenceAtMaxSpeech = _state.VadUseMaxPossibleSilenceAtMaxSpeech
            },
            Overlap = new OverlapOptions
            {
                DetectionPolicy = _state.OverlapDetectionPolicy,
                Onset = _state.OverlapOnset,
                Offset = _state.OverlapOffset,
                MinDurationOnSeconds = _state.OverlapMinDurationOnSeconds,
                MinDurationOffSeconds = _state.OverlapMinDurationOffSeconds
            },
            Separation = new SeparationOptions
            {
                SeparatedVadMinSpeechMs = _state.SeparatedVadMinSpeechMs,
                SeparatedVadMaxSpeechMs = _state.SeparatedVadMaxSpeechMs,
                SeparatedVadSpeechPadMs = _state.SeparatedVadSpeechPadMs,
                SeparatedVadNegThreshold = _state.SeparatedVadNegThreshold,
                SeparatedVadMinSilenceAtMaxSpeechMs = _state.SeparatedVadMinSilenceAtMaxSpeechMs,
                SeparatedVadUseMaxPossibleSilenceAtMaxSpeech = _state.SeparatedVadUseMaxPossibleSilenceAtMaxSpeech
            },
            Sepformer = new SepformerOptions
            {
                NormalizeOutput = _state.SepformerNormalizeOutput
            },
            Transcription = new TranscriptionOptions
            {
                LanguagePolicy = _state.TranscribeLanguagePolicy,
                FixedLanguageTag = _state.TranscribeFixedLanguageTag,
                OutputLanguageTag = _state.TranscribeOutputLanguageTag,
                ModelLanguageCode = _state.TranscribeModelLanguageCode,
                NoSpeechThreshold = _state.NoSpeechThreshold,
                MaxNewTokens = _state.TranscribeMaxNewTokens,
                Temperature = _state.TranscribeTemperature,
                BeamSize = _state.TranscribeBeamSize,
                BestOf = _state.TranscribeBestOf,
                LengthPenalty = _state.TranscribeLengthPenalty,
                RepetitionPenalty = _state.TranscribeRepetitionPenalty,
                SuppressBlank = _state.TranscribeSuppressBlank,
                SuppressTokens = _state.TranscribeSuppressTokens,
                WithoutTimestamps = _state.TranscribeWithoutTimestamps
            },
            Translation = new TranslationOptions
            {
                Provider = _state.TranslationProvider,
                Language = _state.TranslateLanguage,
                OllamaBaseUrl = _state.OllamaBaseUrl,
                OllamaModel = _state.OllamaModel,
                OpenAiBaseUrl = _state.OpenAiBaseUrl,
                OpenAiApiKey = _state.OpenAiApiKey,
                OpenAiModel = _state.OpenAiModel,
                ResponseMode = _state.TranslationResponseMode,
                ContextQueueSize = _state.TranslateContextQueueSize,
                ContextGapMs = _state.TranslateContextGapMs,
                PartialWriteInterval = _state.TranslatePartialWriteInterval
            }
        };
    }

    /// <summary>
    /// Zeayii 构建中间状态。
    /// </summary>
    private sealed class MutableState
    {
        /// <summary>
        /// Zeayii FFmpeg 路径。
        /// </summary>
        public string FfmpegPath { get; set; } = string.Empty;

        /// <summary>
        /// Zeayii 模型根目录。
        /// </summary>
        public string ModelsRoot { get; set; } = string.Empty;

        /// <summary>
        /// Zeayii 缓存目录。
        /// </summary>
        public string CacheDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Zeayii VAD 阈值。
        /// </summary>
        public float VadThreshold { get; set; }

        /// <summary>
        /// Zeayii 最小静音时长。
        /// </summary>
        public int VadMinSilenceMs { get; set; }

        /// <summary>
        /// Zeayii 最小语音时长。
        /// </summary>
        public int VadMinSpeechMs { get; set; }

        /// <summary>
        /// Zeayii 最大语音时长。
        /// </summary>
        public float VadMaxSpeechSeconds { get; set; }

        /// <summary>
        /// Zeayii 段边界补偿时长。
        /// </summary>
        public int VadSpeechPadMs { get; set; }

        /// <summary>
        /// Zeayii 负阈值。
        /// </summary>
        public float VadNegThreshold { get; set; }

        /// <summary>
        /// Zeayii 达到最大语音时使用的最小静音时长。
        /// </summary>
        public float VadMinSilenceAtMaxSpeechMs { get; set; }

        /// <summary>
        /// Zeayii 达到最大语音时是否优先使用最大静音点。
        /// </summary>
        public bool VadUseMaxPossibleSilenceAtMaxSpeech { get; set; }

        /// <summary>
        /// Zeayii 重叠检测策略。
        /// </summary>
        public StageSwitchPolicy OverlapDetectionPolicy { get; set; }

        /// <summary>
        /// Zeayii 重叠起始阈值。
        /// </summary>
        public float OverlapOnset { get; set; }

        /// <summary>
        /// Zeayii 重叠结束阈值。
        /// </summary>
        public float OverlapOffset { get; set; }

        /// <summary>
        /// Zeayii 最短重叠时长。
        /// </summary>
        public float OverlapMinDurationOnSeconds { get; set; }

        /// <summary>
        /// Zeayii 最短非重叠时长。
        /// </summary>
        public float OverlapMinDurationOffSeconds { get; set; }

        /// <summary>
        /// Zeayii 分离后最小语音时长。
        /// </summary>
        public int SeparatedVadMinSpeechMs { get; set; }

        /// <summary>
        /// Zeayii 分离后最大语音时长。
        /// </summary>
        public int SeparatedVadMaxSpeechMs { get; set; }

        /// <summary>
        /// Zeayii 分离后二次 VAD 段边界补偿时长。
        /// </summary>
        public int SeparatedVadSpeechPadMs { get; set; }

        /// <summary>
        /// Zeayii 分离后二次 VAD 负阈值。
        /// </summary>
        public float SeparatedVadNegThreshold { get; set; }

        /// <summary>
        /// Zeayii 分离后二次 VAD 达到最大语音时的最小静音时长。
        /// </summary>
        public float SeparatedVadMinSilenceAtMaxSpeechMs { get; set; }

        /// <summary>
        /// Zeayii 分离后二次 VAD 达到最大语音时是否优先使用最大静音点。
        /// </summary>
        public bool SeparatedVadUseMaxPossibleSilenceAtMaxSpeech { get; set; }

        /// <summary>
        /// Zeayii 转写语言策略。
        /// </summary>
        public LanguagePolicy TranscribeLanguagePolicy { get; set; }

        /// <summary>
        /// Zeayii 固定转写语言标签。
        /// </summary>
        public string TranscribeFixedLanguageTag { get; set; } = string.Empty;

        /// <summary>
        /// Zeayii 输出字幕语言标签。
        /// </summary>
        public string TranscribeOutputLanguageTag { get; set; } = string.Empty;

        /// <summary>
        /// Zeayii 模型执行语言短码。
        /// </summary>
        public string TranscribeModelLanguageCode { get; set; } = string.Empty;

        /// <summary>
        /// Zeayii 无语音阈值。
        /// </summary>
        public float NoSpeechThreshold { get; set; }

        /// <summary>
        /// Zeayii 转写最大新增 token。
        /// </summary>
        public int TranscribeMaxNewTokens { get; set; }

        /// <summary>
        /// Zeayii 转写温度。
        /// </summary>
        public float TranscribeTemperature { get; set; }

        /// <summary>
        /// Zeayii 转写 beam 大小。
        /// </summary>
        public int TranscribeBeamSize { get; set; }

        /// <summary>
        /// Zeayii 转写 best-of 候选数。
        /// </summary>
        public int TranscribeBestOf { get; set; }

        /// <summary>
        /// Zeayii 转写长度惩罚。
        /// </summary>
        public float TranscribeLengthPenalty { get; set; }

        /// <summary>
        /// Zeayii 转写重复惩罚。
        /// </summary>
        public float TranscribeRepetitionPenalty { get; set; }

        /// <summary>
        /// Zeayii 转写是否抑制空白。
        /// </summary>
        public bool TranscribeSuppressBlank { get; set; }

        /// <summary>
        /// Zeayii 转写抑制 token 列表。
        /// </summary>
        public IReadOnlyList<int> TranscribeSuppressTokens { get; set; } = [];

        /// <summary>
        /// Zeayii 转写是否禁用时间戳。
        /// </summary>
        public bool TranscribeWithoutTimestamps { get; set; }

        /// <summary>
        /// Zeayii SepFormer 输出归一化开关。
        /// </summary>
        public bool SepformerNormalizeOutput { get; set; }

        /// <summary>
        /// Zeayii 翻译服务提供方策略。
        /// </summary>
        public TranslationProviderPolicy TranslationProvider { get; set; } = TranslationProviderPolicy.Ollama;

        /// <summary>
        /// Zeayii 译文语言。
        /// </summary>
        public CultureInfo TranslateLanguage { get; set; } = CultureInfo.InvariantCulture;

        /// <summary>
        /// Zeayii Ollama 基址。
        /// </summary>
        public string OllamaBaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Zeayii Ollama 模型。
        /// </summary>
        public string OllamaModel { get; set; } = string.Empty;

        /// <summary>
        /// Zeayii OpenAI 基址。
        /// </summary>
        public string OpenAiBaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Zeayii OpenAI API Key。
        /// </summary>
        public string OpenAiApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Zeayii OpenAI 模型。
        /// </summary>
        public string OpenAiModel { get; set; } = string.Empty;

        /// <summary>
        /// Zeayii 翻译响应模式。
        /// </summary>
        public TranslationResponseMode TranslationResponseMode { get; set; }

        /// <summary>
        /// Zeayii 翻译上下文窗口。
        /// </summary>
        public int TranslateContextQueueSize { get; set; }

        /// <summary>
        /// Zeayii 翻译上下文断链阈值。
        /// </summary>
        public int TranslateContextGapMs { get; set; }

        /// <summary>
        /// Zeayii 翻译中间字幕写出间隔。
        /// </summary>
        public int TranslatePartialWriteInterval { get; set; }

        /// <summary>
        /// Zeayii 字幕格式策略。
        /// </summary>
        public SubtitleFormatPolicy SubtitleFormatPolicy { get; set; }

        /// <summary>
        /// Zeayii 最大并发度。
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; }

        /// <summary>
        /// Zeayii 命令超时。
        /// </summary>
        public int CommandTimeoutSeconds { get; set; }

        /// <summary>
        /// Zeayii 前处理阶段设备。
        /// </summary>
        public ExecutionDevicePolicy PreprocessDevice { get; set; } = ExecutionDevicePolicy.Cpu;

        /// <summary>
        /// Zeayii 前处理阶段并发数量。
        /// </summary>
        public int PreprocessParallelism { get; set; } = 1;

        /// <summary>
        /// Zeayii 转录阶段设备。
        /// </summary>
        public ExecutionDevicePolicy TranscribeDevice { get; set; } = ExecutionDevicePolicy.Cpu;

        /// <summary>
        /// Zeayii 转录阶段并发数量。
        /// </summary>
        public int TranscribeParallelism { get; set; } = 1;

        /// <summary>
        /// Zeayii 翻译阶段设备。
        /// </summary>
        public ExecutionDevicePolicy TranslateDevice { get; set; } = ExecutionDevicePolicy.Cpu;

        /// <summary>
        /// Zeayii 翻译阶段并发数量。
        /// </summary>
        public int TranslateParallelism { get; set; } = 1;

        /// <summary>
        /// Zeayii 翻译执行模式。
        /// </summary>
        public TranslationExecutionModePolicy TranslationExecutionMode { get; set; } = TranslationExecutionModePolicy.BatchAfterTranscription;

        /// <summary>
        /// Zeayii GPU 冲突策略。
        /// </summary>
        public GpuConflictPolicy GpuConflictPolicy { get; set; } = GpuConflictPolicy.TranscribeVsTranslate;

        /// <summary>
        /// Zeayii 字幕产物覆盖策略。
        /// </summary>
        public ArtifactOverwritePolicy ArtifactOverwritePolicy { get; set; } = ArtifactOverwritePolicy.SkipExisting;

        /// <summary>
        /// Zeayii 分割模型路径。
        /// </summary>
        public string SegmentationModelPath { get; set; } = string.Empty;

        /// <summary>
        /// Zeayii 分离模型路径。
        /// </summary>
        public string SepformerModelPath { get; set; } = string.Empty;

        /// <summary>
        /// Zeayii Whisper 根目录。
        /// </summary>
        public string WhisperModelRoot { get; set; } = string.Empty;

        /// <summary>
        /// Zeayii 窗口日志等级。
        /// </summary>
        public LogLevel ConsoleLogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Zeayii 文件日志等级。
        /// </summary>
        public LogLevel FileLogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// Zeayii 日志目录。
        /// </summary>
        public string LogDirectory { get; set; } = Path.Combine(AppContext.BaseDirectory, "logs");
    }
}


