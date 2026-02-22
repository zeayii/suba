using System.CommandLine;
using System.CommandLine.Parsing;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Zeayii.Suba.CommandLine.Options;
using Zeayii.Suba.Core.Configuration.Policies;

namespace Zeayii.Suba.CommandLine.Extensions;

/// <summary>
/// Zeayii 命令行参数扩展。
/// </summary>
internal static class SubaCommandExtensions
{
    /// <summary>
    /// Zeayii 向命令注册 Suba 全量选项。
    /// </summary>
    /// <param name="command">Zeayii 目标命令。</param>
    /// <returns>Zeayii 参数定义集合。</returns>
    public static SubaCommandOptions AddSubaOptions(this Command command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var options = new SubaCommandOptions
        {
            ArgumentsTomlPathArgument = new Argument<FileInfo>("arguments-toml-path")
            {
                Description = "参数 TOML 文件路径。",
                Arity = ArgumentArity.ExactlyOne
            }.AcceptExistingOnly().AcceptLegalFilePathsOnly(),

            FfmpegPathOption = new Option<FileInfo>("--ffmpeg-path")
            {
                Description = "FFmpeg 可执行文件路径（默认自动探测：FFMPEG_PATH -> PATH）。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => new FileInfo(ResolveFfmpegPathOrPlaceholder())
            }.AcceptLegalFilePathsOnly(),

            ModelsRootOption = new Option<DirectoryInfo>("--models-root")
            {
                Description = "模型根目录（默认：当前目录下 models）。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => new DirectoryInfo(ResolveDefaultModelsRoot())
            },

            CacheDirectoryOption = new Option<DirectoryInfo>("--cache-directory")
            {
                Description = "缓存目录。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => new DirectoryInfo(Path.Combine(Path.GetTempPath(), "suba-cache"))
            },
            ConsoleLogLevelOption = new Option<LogLevel>("--console-log-level")
            {
                Description = "窗口日志等级。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => LogLevel.Information
            },
            FileLogLevelOption = new Option<LogLevel>("--file-log-level")
            {
                Description = "文件日志等级。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => LogLevel.Information
            },
            LogDirectoryOption = new Option<DirectoryInfo>("--log-directory")
            {
                Description = "日志目录。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "logs"))
            },

            VadThresholdOption = CreateFloatOption("--vad-threshold", "Speech threshold. Silero VAD outputs speech probabilities for each audio chunk, probabilities ABOVE this value are considered as SPEECH.", 0.35f),
            VadMinSilenceMsOption = CreateIntOption("--vad-min-silence-ms", "In the end of each speech chunk wait for min_silence_duration_ms before separating it.", 200),
            VadMinSpeechMsOption = CreateIntOption("--vad-min-speech-ms", "Final speech chunks shorter min_speech_duration_ms are thrown out.", 250),
            VadMaxSpeechSecondsOption = CreateFloatOption("--vad-max-speech-seconds", "Maximum duration of speech chunks in seconds.", 10.0f),
            VadSpeechPadMsOption = CreateIntOption("--vad-speech-pad-ms", "Final speech chunks are padded by speech_pad_ms each side.", 120),
            VadNegThresholdOption = new Option<float>("--vad-neg-threshold")
            {
                Description = "Negative threshold (noise or exit threshold).", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => 0.20f
            },
            VadMinSilenceAtMaxSpeechMsOption = CreateFloatOption("--vad-min-silence-at-max-speech-ms", "Minimum silence duration used when max speech duration is reached.", 98f),
            VadUseMaxPossibleSilenceAtMaxSpeechOption = CreateBoolOption("--vad-use-max-possible-silence-at-max-speech", "Whether to use maximum possible silence point when max speech duration is reached.", true),

            OverlapDetectionPolicyOption = new Option<StageSwitchPolicy>("--overlap-detection-policy")
            {
                Description = "重叠语音检测策略。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => StageSwitchPolicy.Disabled
            },
            OverlapOnsetOption = CreateFloatOption("--overlap-onset", "重叠检测起始阈值（高阈值）。", 0.8104268538848918f),
            OverlapOffsetOption = CreateFloatOption("--overlap-offset", "重叠检测结束阈值（低阈值）。", 0.4806866463041527f),
            OverlapMinDurationOnSecondsOption = CreateFloatOption("--overlap-min-duration-on-seconds", "最短重叠持续时长（秒）。", 0.05537587440407595f),
            OverlapMinDurationOffSecondsOption = CreateFloatOption("--overlap-min-duration-off-seconds", "最短非重叠持续时长（秒）。", 0.09791355693027545f),

            SeparatedVadMinSpeechMsOption = CreateIntOption("--separated-vad-min-speech-ms", "Final speech chunks shorter min_speech_duration_ms are thrown out.", 300),
            SeparatedVadMaxSpeechMsOption = CreateIntOption("--separated-vad-max-speech-ms", "Maximum duration of speech chunks in milliseconds.", 8000),
            SeparatedVadSpeechPadMsOption = new Option<int>("--separated-vad-speech-pad-ms")
            {
                Description = "分离后二次 VAD 段边界补偿时长（毫秒）。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => 0
            },
            SeparatedVadNegThresholdOption = new Option<float>("--separated-vad-neg-threshold")
            {
                Description = "分离后二次 VAD 负阈值。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => 0.20f
            },
            SeparatedVadMinSilenceAtMaxSpeechMsOption = new Option<float>("--separated-vad-min-silence-at-max-speech-ms")
            {
                Description = "分离后二次 VAD 达到最大语音时最小静音时长（毫秒）。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => 98f
            },
            SeparatedVadUseMaxPossibleSilenceAtMaxSpeechOption = new Option<bool>("--separated-vad-use-max-possible-silence-at-max-speech")
            {
                Description = "分离后二次 VAD 达到最大语音时是否优先最大静音点。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => true
            },
            SepformerNormalizeOutputOption = CreateBoolOption("--sepformer-normalize-output", "是否启用分离输出峰值归一化。", true),

            TranscribeLanguagePolicyOption = new Option<LanguagePolicy>("--transcribe-language-policy")
            {
                Description = "转写语言策略。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => LanguagePolicy.Fixed
            },
            TranscribeLanguageOption = new Option<string>("--transcribe-language")
            {
                Description = "转写语言（BCP 47）。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => "ja"
            },
            NoSpeechThresholdOption = CreateFloatOption("--no-speech-threshold", "Whisper no speech threshold.", 0.65f),
            TranscribeMaxNewTokensOption = CreateIntOption("--transcribe-max-new-tokens", "Whisper maximum new tokens for decoding.", 128),
            TranscribeTemperatureOption = CreateFloatOption("--transcribe-temperature", "Whisper decode temperature.", 0f),
            TranscribeBeamSizeOption = CreateIntOption("--transcribe-beam-size", "Whisper beam size (for compatibility, current ONNX decoder uses greedy path).", 5),
            TranscribeBestOfOption = CreateIntOption("--transcribe-best-of", "Whisper best-of candidates (for compatibility, current ONNX decoder uses greedy path).", 5),
            TranscribeLengthPenaltyOption = CreateFloatOption("--transcribe-length-penalty", "Whisper length penalty.", 1f),
            TranscribeRepetitionPenaltyOption = CreateFloatOption("--transcribe-repetition-penalty", "Whisper repetition penalty.", 1f),
            TranscribeSuppressBlankOption = CreateBoolOption("--transcribe-suppress-blank", "Whisper suppress blank outputs.", true),
            TranscribeSuppressTokensOption = new Option<int[]>("--transcribe-suppress-tokens")
            {
                Description = "Whisper suppress tokens list.", Required = false,
                AllowMultipleArgumentsPerToken = true, Arity = ArgumentArity.ZeroOrMore, DefaultValueFactory = _ => [-1]
            },
            TranscribeWithoutTimestampsOption = CreateBoolOption("--transcribe-without-timestamps", "Whisper without timestamps.", true),
            TranslationProviderOption = new Option<TranslationProviderPolicy>("--translation-provider")
            {
                Description = "翻译服务提供方策略。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => TranslationProviderPolicy.Ollama
            },
            TranslateLanguageOption = new Option<string>("--translate-language")
            {
                Description = "翻译语言（BCP 47）。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => "zh-CN"
            },
            OllamaBaseUrlOption = new Option<Uri>("--ollama-base-url")
            {
                Description = "Ollama 服务地址。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => new Uri("http://localhost:11434")
            },
            OllamaModelOption = new Option<string>("--ollama-model")
            {
                Description = "Ollama 模型名称。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => "qwen3:14b"
            },
            OpenAiBaseUrlOption = new Option<Uri>("--openai-base-url")
            {
                Description = "OpenAI 服务地址。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => new Uri("https://api.openai.com")
            },
            OpenAiApiKeyOption = new Option<string>("--openai-api-key")
            {
                Description = "OpenAI API Key。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => string.Empty
            },
            OpenAiModelOption = new Option<string>("--openai-model")
            {
                Description = "OpenAI 模型名称。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => "gpt-4o-mini"
            },
            TranslateResponseModeOption = new Option<TranslationResponseMode>("--translate-response-mode")
            {
                Description = "翻译响应模式策略。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => TranslationResponseMode.NonStreaming
            },
            TranslateContextQueueSizeOption = CreateIntOption("--translate-context-queue-size", "翻译上下文窗口大小。", 6),
            TranslateContextGapMsOption = CreateIntOption("--translate-context-gap-ms", "翻译上下文断链阈值（毫秒）。", 10000),
            TranslatePartialWriteIntervalOption = CreateIntOption("--translate-partial-write-interval", "翻译中间字幕写出间隔（条），0 表示仅最终写出。", 300),
            SubtitleFormatPolicyOption = new Option<SubtitleFormatPolicy>("--subtitle-format-policy")
            {
                Description = "字幕格式策略。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => SubtitleFormatPolicy.Vtt
            },
            MaxDegreeOfParallelismOption = CreateIntOption("--max-degree-of-parallelism", "最大并发度。", 1),
            CommandTimeoutSecondsOption = CreateIntOption("--command-timeout-seconds", "命令超时（秒）。", 120),
            PreprocessDeviceOption = new Option<ExecutionDevicePolicy>("--preprocess-device")
            {
                Description = "前处理阶段执行设备（Cpu/Gpu）。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => ExecutionDevicePolicy.Cpu
            },
            PreprocessParallelismOption = CreateIntOption("--preprocess-parallelism", "前处理阶段并发执行数量。", 5),
            TranscribeDeviceOption = new Option<ExecutionDevicePolicy>("--transcribe-device")
            {
                Description = "转录阶段执行设备（Cpu/Gpu）。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => ExecutionDevicePolicy.Gpu
            },
            TranscribeParallelismOption = CreateIntOption("--transcribe-parallelism", "转录阶段并发执行数量。", 1),
            TranslateDeviceOption = new Option<ExecutionDevicePolicy>("--translate-device")
            {
                Description = "翻译阶段执行设备（Cpu/Gpu）。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => ExecutionDevicePolicy.Gpu
            },
            TranslateParallelismOption = CreateIntOption("--translate-parallelism", "翻译阶段并发执行数量。", 1),
            TranslationExecutionModeOption = new Option<TranslationExecutionModePolicy>("--translation-mode")
            {
                Description = "翻译执行模式策略。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => TranslationExecutionModePolicy.BatchAfterTranscription
            },
            GpuConflictPolicyOption = new Option<GpuConflictPolicy>("--gpu-conflict-policy")
            {
                Description = "GPU 阶段冲突策略，支持按位组合（例如 PreprocessVsTranslate|TranscribeVsTranslate）。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => GpuConflictPolicy.TranscribeVsTranslate
            },
            ArtifactOverwritePolicyOption = new Option<ArtifactOverwritePolicy>("--artifact-overwrite-policy")
            {
                Description = "字幕产物覆盖策略。", Required = false,
                AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => ArtifactOverwritePolicy.SkipExisting
            }
        };

        AddValidators(options);
        AddOptions(command, options);
        AddCommandValidators(command, options);
        return options;
    }

    /// <summary>
    /// Zeayii 将解析结果绑定为应用配置。
    /// </summary>
    /// <param name="parseResult">Zeayii 解析结果。</param>
    /// <param name="options">Zeayii 参数集合。</param>
    /// <returns>Zeayii 应用配置对象。</returns>
    public static ApplicationOptions BuildApplicationOptions(this ParseResult parseResult, SubaCommandOptions options)
    {
        ArgumentNullException.ThrowIfNull(parseResult);
        ArgumentNullException.ThrowIfNull(options);

        var transcribeLanguagePolicy = parseResult.GetValue(options.TranscribeLanguagePolicyOption);
        var transcribeLanguageTag = parseResult.GetValue(options.TranscribeLanguageOption)!;

        return new ApplicationOptions
        {
            ArgumentsTomlPath = parseResult.GetRequiredValue(options.ArgumentsTomlPathArgument),
            FfmpegPath = parseResult.GetRequiredValue(options.FfmpegPathOption),
            ModelsRoot = parseResult.GetRequiredValue(options.ModelsRootOption),
            CacheDirectory = parseResult.GetRequiredValue(options.CacheDirectoryOption),
            ConsoleLogLevel = parseResult.GetRequiredValue(options.ConsoleLogLevelOption),
            FileLogLevel = parseResult.GetRequiredValue(options.FileLogLevelOption),
            LogDirectory = parseResult.GetRequiredValue(options.LogDirectoryOption),
            VadThreshold = parseResult.GetRequiredValue(options.VadThresholdOption),
            VadMinSilenceMs = parseResult.GetRequiredValue(options.VadMinSilenceMsOption),
            VadMinSpeechMs = parseResult.GetRequiredValue(options.VadMinSpeechMsOption),
            VadMaxSpeechSeconds = parseResult.GetRequiredValue(options.VadMaxSpeechSecondsOption),
            VadSpeechPadMs = parseResult.GetRequiredValue(options.VadSpeechPadMsOption),
            VadNegThreshold = parseResult.GetRequiredValue(options.VadNegThresholdOption),
            VadMinSilenceAtMaxSpeechMs = parseResult.GetRequiredValue(options.VadMinSilenceAtMaxSpeechMsOption),
            VadUseMaxPossibleSilenceAtMaxSpeech = parseResult.GetRequiredValue(options.VadUseMaxPossibleSilenceAtMaxSpeechOption),
            OverlapDetectionPolicy = parseResult.GetRequiredValue(options.OverlapDetectionPolicyOption),
            OverlapOnset = parseResult.GetRequiredValue(options.OverlapOnsetOption),
            OverlapOffset = parseResult.GetRequiredValue(options.OverlapOffsetOption),
            OverlapMinDurationOnSeconds = parseResult.GetRequiredValue(options.OverlapMinDurationOnSecondsOption),
            OverlapMinDurationOffSeconds = parseResult.GetRequiredValue(options.OverlapMinDurationOffSecondsOption),
            SeparatedVadMinSpeechMs = parseResult.GetRequiredValue(options.SeparatedVadMinSpeechMsOption),
            SeparatedVadMaxSpeechMs = parseResult.GetRequiredValue(options.SeparatedVadMaxSpeechMsOption),
            SeparatedVadSpeechPadMs = parseResult.GetRequiredValue(options.SeparatedVadSpeechPadMsOption),
            SeparatedVadNegThreshold = parseResult.GetRequiredValue(options.SeparatedVadNegThresholdOption),
            SeparatedVadMinSilenceAtMaxSpeechMs = parseResult.GetRequiredValue(options.SeparatedVadMinSilenceAtMaxSpeechMsOption),
            SeparatedVadUseMaxPossibleSilenceAtMaxSpeech = parseResult.GetRequiredValue(options.SeparatedVadUseMaxPossibleSilenceAtMaxSpeechOption),
            SepformerNormalizeOutput = parseResult.GetRequiredValue(options.SepformerNormalizeOutputOption),
            TranscribeLanguagePolicy = transcribeLanguagePolicy,
            TranscribeLanguageTag = transcribeLanguageTag,
            NoSpeechThreshold = parseResult.GetRequiredValue(options.NoSpeechThresholdOption),
            TranscribeMaxNewTokens = parseResult.GetRequiredValue(options.TranscribeMaxNewTokensOption),
            TranscribeTemperature = parseResult.GetRequiredValue(options.TranscribeTemperatureOption),
            TranscribeBeamSize = parseResult.GetRequiredValue(options.TranscribeBeamSizeOption),
            TranscribeBestOf = parseResult.GetRequiredValue(options.TranscribeBestOfOption),
            TranscribeLengthPenalty = parseResult.GetRequiredValue(options.TranscribeLengthPenaltyOption),
            TranscribeRepetitionPenalty = parseResult.GetRequiredValue(options.TranscribeRepetitionPenaltyOption),
            TranscribeSuppressBlank = parseResult.GetRequiredValue(options.TranscribeSuppressBlankOption),
            TranscribeSuppressTokens = parseResult.GetRequiredValue(options.TranscribeSuppressTokensOption),
            TranscribeWithoutTimestamps = parseResult.GetRequiredValue(options.TranscribeWithoutTimestampsOption),
            TranslationProvider = parseResult.GetRequiredValue(options.TranslationProviderOption),
            TranslateLanguage = CultureInfo.GetCultureInfo(parseResult.GetRequiredValue(options.TranslateLanguageOption)),
            OllamaBaseUrl = parseResult.GetRequiredValue(options.OllamaBaseUrlOption),
            OllamaModel = parseResult.GetRequiredValue(options.OllamaModelOption),
            OpenAiBaseUrl = parseResult.GetRequiredValue(options.OpenAiBaseUrlOption),
            OpenAiApiKey = parseResult.GetRequiredValue(options.OpenAiApiKeyOption),
            OpenAiModel = parseResult.GetRequiredValue(options.OpenAiModelOption),
            TranslateResponseMode = parseResult.GetRequiredValue(options.TranslateResponseModeOption),
            TranslateContextQueueSize = parseResult.GetRequiredValue(options.TranslateContextQueueSizeOption),
            TranslateContextGapMs = parseResult.GetRequiredValue(options.TranslateContextGapMsOption),
            TranslatePartialWriteInterval = parseResult.GetRequiredValue(options.TranslatePartialWriteIntervalOption),
            SubtitleFormatPolicy = parseResult.GetRequiredValue(options.SubtitleFormatPolicyOption),
            MaxDegreeOfParallelism = parseResult.GetRequiredValue(options.MaxDegreeOfParallelismOption),
            CommandTimeoutSeconds = parseResult.GetRequiredValue(options.CommandTimeoutSecondsOption),
            PreprocessDevice = parseResult.GetRequiredValue(options.PreprocessDeviceOption),
            PreprocessParallelism = parseResult.GetRequiredValue(options.PreprocessParallelismOption),
            TranscribeDevice = parseResult.GetRequiredValue(options.TranscribeDeviceOption),
            TranscribeParallelism = parseResult.GetRequiredValue(options.TranscribeParallelismOption),
            TranslateDevice = parseResult.GetRequiredValue(options.TranslateDeviceOption),
            TranslateParallelism = parseResult.GetRequiredValue(options.TranslateParallelismOption),
            TranslationExecutionMode = parseResult.GetRequiredValue(options.TranslationExecutionModeOption),
            GpuConflictPolicy = parseResult.GetRequiredValue(options.GpuConflictPolicyOption),
            ArtifactOverwritePolicy = parseResult.GetRequiredValue(options.ArtifactOverwritePolicyOption)
        };
    }

    /// <summary>
    /// Zeayii 注册参数校验规则。
    /// </summary>
    /// <param name="options">Zeayii 参数集合。</param>
    private static void AddValidators(SubaCommandOptions options)
    {
        options.VadThresholdOption.Validators.Add(result => ValidateRangeInclusive(result.GetValueOrDefault<float>(), 0f, 1f, result));
        options.VadNegThresholdOption.Validators.Add(result => ValidateRangeInclusive(result.GetValueOrDefault<float>(), 0f, 1f, result));
        options.NoSpeechThresholdOption.Validators.Add(result => ValidateRangeInclusive(result.GetValueOrDefault<float>(), 0f, 1f, result));
        options.OverlapOnsetOption.Validators.Add(result => ValidateRangeInclusive(result.GetValueOrDefault<float>(), 0f, 1f, result));
        options.OverlapOffsetOption.Validators.Add(result => ValidateRangeInclusive(result.GetValueOrDefault<float>(), 0f, 1f, result));
        options.TranscribeTemperatureOption.Validators.Add(result => ValidateGreaterOrEqual(result.GetValueOrDefault<float>(), 0f, result));

        options.VadMinSilenceMsOption.Validators.Add(result => ValidateNonNegative(result.GetValueOrDefault<int>(), result));
        options.VadMinSpeechMsOption.Validators.Add(result => ValidatePositive(result.GetValueOrDefault<int>(), result));
        options.VadSpeechPadMsOption.Validators.Add(result => ValidateNonNegative(result.GetValueOrDefault<int>(), result));
        options.VadMinSilenceAtMaxSpeechMsOption.Validators.Add(result => ValidateNonNegative(result.GetValueOrDefault<float>(), result));
        options.SeparatedVadMinSpeechMsOption.Validators.Add(result => ValidatePositive(result.GetValueOrDefault<int>(), result));
        options.SeparatedVadMaxSpeechMsOption.Validators.Add(result => ValidatePositive(result.GetValueOrDefault<int>(), result));
        options.SeparatedVadSpeechPadMsOption.Validators.Add(result => ValidateNonNegative(result.GetValueOrDefault<int>(), result));
        options.SeparatedVadNegThresholdOption.Validators.Add(result => ValidateRangeInclusive(result.GetValueOrDefault<float>(), 0f, 1f, result));
        options.SeparatedVadMinSilenceAtMaxSpeechMsOption.Validators.Add(result => ValidateNonNegative(result.GetValueOrDefault<float>(), result));
        options.TranscribeMaxNewTokensOption.Validators.Add(result => ValidatePositive(result.GetValueOrDefault<int>(), result));
        options.TranscribeBeamSizeOption.Validators.Add(result => ValidatePositive(result.GetValueOrDefault<int>(), result));
        options.TranscribeBestOfOption.Validators.Add(result => ValidatePositive(result.GetValueOrDefault<int>(), result));
        options.TranslateContextQueueSizeOption.Validators.Add(result => ValidatePositive(result.GetValueOrDefault<int>(), result));
        options.TranslateContextGapMsOption.Validators.Add(result => ValidatePositive(result.GetValueOrDefault<int>(), result));
        options.TranslatePartialWriteIntervalOption.Validators.Add(result => ValidateNonNegative(result.GetValueOrDefault<int>(), result));
        options.MaxDegreeOfParallelismOption.Validators.Add(result => ValidatePositive(result.GetValueOrDefault<int>(), result));
        options.CommandTimeoutSecondsOption.Validators.Add(result => ValidatePositive(result.GetValueOrDefault<int>(), result));
        options.PreprocessParallelismOption.Validators.Add(result => ValidatePositive(result.GetValueOrDefault<int>(), result));
        options.TranscribeParallelismOption.Validators.Add(result => ValidatePositive(result.GetValueOrDefault<int>(), result));
        options.TranslateParallelismOption.Validators.Add(result => ValidatePositive(result.GetValueOrDefault<int>(), result));

        options.TranscribeLanguageOption.Validators.Add(ValidateCulture);
        options.TranslateLanguageOption.Validators.Add(ValidateCulture);
    }

    /// <summary>
    /// Zeayii 将选项挂载到命令。
    /// </summary>
    /// <param name="command">Zeayii 目标命令。</param>
    /// <param name="options">Zeayii 参数集合。</param>
    private static void AddOptions(Command command, SubaCommandOptions options)
    {
        command.Arguments.Add(options.ArgumentsTomlPathArgument);

        command.Options.Add(options.FfmpegPathOption);
        command.Options.Add(options.ModelsRootOption);
        command.Options.Add(options.CacheDirectoryOption);
        command.Options.Add(options.LogDirectoryOption);
        command.Options.Add(options.ConsoleLogLevelOption);
        command.Options.Add(options.FileLogLevelOption);
        command.Options.Add(options.MaxDegreeOfParallelismOption);
        command.Options.Add(options.CommandTimeoutSecondsOption);
        command.Options.Add(options.SubtitleFormatPolicyOption);
        command.Options.Add(options.TranslationExecutionModeOption);
        command.Options.Add(options.GpuConflictPolicyOption);
        command.Options.Add(options.ArtifactOverwritePolicyOption);
        command.Options.Add(options.PreprocessDeviceOption);
        command.Options.Add(options.PreprocessParallelismOption);
        command.Options.Add(options.TranscribeDeviceOption);
        command.Options.Add(options.TranscribeParallelismOption);
        command.Options.Add(options.TranslateDeviceOption);
        command.Options.Add(options.TranslateParallelismOption);

        command.Options.Add(options.VadThresholdOption);
        command.Options.Add(options.VadMinSilenceMsOption);
        command.Options.Add(options.VadMinSpeechMsOption);
        command.Options.Add(options.VadMaxSpeechSecondsOption);
        command.Options.Add(options.VadSpeechPadMsOption);
        command.Options.Add(options.VadNegThresholdOption);
        command.Options.Add(options.VadMinSilenceAtMaxSpeechMsOption);
        command.Options.Add(options.VadUseMaxPossibleSilenceAtMaxSpeechOption);

        command.Options.Add(options.OverlapDetectionPolicyOption);
        command.Options.Add(options.OverlapOnsetOption);
        command.Options.Add(options.OverlapOffsetOption);
        command.Options.Add(options.OverlapMinDurationOnSecondsOption);
        command.Options.Add(options.OverlapMinDurationOffSecondsOption);

        command.Options.Add(options.SeparatedVadMinSpeechMsOption);
        command.Options.Add(options.SeparatedVadMaxSpeechMsOption);
        command.Options.Add(options.SeparatedVadSpeechPadMsOption);
        command.Options.Add(options.SeparatedVadNegThresholdOption);
        command.Options.Add(options.SeparatedVadMinSilenceAtMaxSpeechMsOption);
        command.Options.Add(options.SeparatedVadUseMaxPossibleSilenceAtMaxSpeechOption);
        command.Options.Add(options.SepformerNormalizeOutputOption);

        command.Options.Add(options.TranscribeLanguagePolicyOption);
        command.Options.Add(options.TranscribeLanguageOption);
        command.Options.Add(options.NoSpeechThresholdOption);
        command.Options.Add(options.TranscribeMaxNewTokensOption);
        command.Options.Add(options.TranscribeTemperatureOption);
        command.Options.Add(options.TranscribeBeamSizeOption);
        command.Options.Add(options.TranscribeBestOfOption);
        command.Options.Add(options.TranscribeLengthPenaltyOption);
        command.Options.Add(options.TranscribeRepetitionPenaltyOption);
        command.Options.Add(options.TranscribeSuppressBlankOption);
        command.Options.Add(options.TranscribeSuppressTokensOption);
        command.Options.Add(options.TranscribeWithoutTimestampsOption);

        command.Options.Add(options.TranslationProviderOption);
        command.Options.Add(options.TranslateLanguageOption);
        command.Options.Add(options.OllamaBaseUrlOption);
        command.Options.Add(options.OllamaModelOption);
        command.Options.Add(options.OpenAiBaseUrlOption);
        command.Options.Add(options.OpenAiApiKeyOption);
        command.Options.Add(options.OpenAiModelOption);
        command.Options.Add(options.TranslateResponseModeOption);
        command.Options.Add(options.TranslateContextQueueSizeOption);
        command.Options.Add(options.TranslateContextGapMsOption);
        command.Options.Add(options.TranslatePartialWriteIntervalOption);
    }

    /// <summary>
    /// Zeayii 注册命令级组合参数校验规则。
    /// </summary>
    /// <param name="command">Zeayii 目标命令。</param>
    /// <param name="options">Zeayii 参数集合。</param>
    private static void AddCommandValidators(Command command, SubaCommandOptions options)
    {
        command.Validators.Add(result =>
        {
            if (IsInitInvocation(result))
            {
                return;
            }

            var ffmpegPath = result.GetValue(options.FfmpegPathOption);
            if (ffmpegPath is null)
            {
                result.AddError("--ffmpeg-path cannot be null.");
            }
            else
            {
                ValidateFfmpegPath(ffmpegPath, result);
            }

            var modelsRoot = result.GetValue(options.ModelsRootOption);
            if (modelsRoot is null)
            {
                result.AddError("--models-root cannot be null.");
            }
            else
            {
                ValidateModelsRoot(modelsRoot, result);
            }

            var policy = result.GetValue(options.TranscribeLanguagePolicyOption);
            var language = result.GetValue(options.TranscribeLanguageOption);
            if (policy == LanguagePolicy.Fixed && string.IsNullOrWhiteSpace(language))
            {
                result.AddError("--transcribe-language is required when --transcribe-language-policy is Fixed.");
            }

            var translationProvider = result.GetValue(options.TranslationProviderOption);
            var openAiApiKey = result.GetValue(options.OpenAiApiKeyOption);
            if (translationProvider == TranslationProviderPolicy.OpenAi && string.IsNullOrWhiteSpace(openAiApiKey))
            {
                result.AddError("--openai-api-key is required when --translation-provider is OpenAi.");
            }
        });
    }

    /// <summary>
    /// Zeayii 创建整型选项。
    /// </summary>
    /// <param name="alias">Zeayii 选项别名。</param>
    /// <param name="description">Zeayii 选项说明。</param>
    /// <param name="defaultValue">Zeayii 默认值。</param>
    /// <returns>Zeayii 整型选项。</returns>
    private static Option<int> CreateIntOption(string alias, string description, int defaultValue)
        => new(alias)
        {
            Description = description,
            Required = false,
            AllowMultipleArgumentsPerToken = false,
            Arity = ArgumentArity.ZeroOrOne,
            DefaultValueFactory = _ => defaultValue
        };

    /// <summary>
    /// Zeayii 创建浮点选项。
    /// </summary>
    /// <param name="alias">Zeayii 选项别名。</param>
    /// <param name="description">Zeayii 选项说明。</param>
    /// <param name="defaultValue">Zeayii 默认值。</param>
    /// <returns>Zeayii 浮点选项。</returns>
    private static Option<float> CreateFloatOption(string alias, string description, float defaultValue)
        => new(alias) { Description = description, Required = false, AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => defaultValue };

    /// <summary>
    /// Zeayii 创建布尔选项。
    /// </summary>
    /// <param name="alias">Zeayii 选项别名。</param>
    /// <param name="description">Zeayii 选项说明。</param>
    /// <param name="defaultValue">Zeayii 默认值。</param>
    /// <returns>Zeayii 布尔选项。</returns>
    private static Option<bool> CreateBoolOption(string alias, string description, bool defaultValue)
        => new(alias) { Description = description, Required = false, AllowMultipleArgumentsPerToken = false, Arity = ArgumentArity.ZeroOrOne, DefaultValueFactory = _ => defaultValue };

    /// <summary>
    /// Zeayii 校验 FFmpeg 路径。
    /// </summary>
    /// <param name="path">Zeayii FFmpeg 路径。</param>
    /// <param name="result">Zeayii 命令校验结果。</param>
    private static void ValidateFfmpegPath(FileInfo path, CommandResult result)
    {
        if (!path.Exists)
        {
            result.AddError("Cannot resolve ffmpeg executable. Set --ffmpeg-path or FFMPEG_PATH/PATH.");
        }
    }

    /// <summary>
    /// Zeayii 校验模型根目录及强依赖文件完整性。
    /// </summary>
    /// <param name="directory">Zeayii 模型根目录。</param>
    /// <param name="result">Zeayii 命令校验结果。</param>
    private static void ValidateModelsRoot(DirectoryInfo directory, CommandResult result)
    {
        if (!directory.Exists)
        {
            result.AddError("models-root does not exist.");
            return;
        }

        var requiredFiles = new[]
        {
            Path.Combine(directory.FullName, "onnx-community", "silero-vad", "onnx", "model.onnx"),
            Path.Combine(directory.FullName, "onnx-community", "pyannote-segmentation-3.0", "onnx", "model.onnx"),
            Path.Combine(directory.FullName, "speechbrain", "sepformer-wsj02mix", "onnx", "model.onnx"),
            Path.Combine(directory.FullName, "onnx-community", "kotoba-whisper-v2.2-ONNX", "onnx", "encoder_model.onnx"),
            Path.Combine(directory.FullName, "onnx-community", "kotoba-whisper-v2.2-ONNX", "onnx", "encoder_model.onnx_data"),
            Path.Combine(directory.FullName, "onnx-community", "kotoba-whisper-v2.2-ONNX", "onnx", "decoder_model.onnx"),
            Path.Combine(directory.FullName, "onnx-community", "kotoba-whisper-v2.2-ONNX", "onnx", "decoder_with_past_model.onnx"),
            Path.Combine(directory.FullName, "onnx-community", "kotoba-whisper-v2.2-ONNX", "tokenizer.json"),
            Path.Combine(directory.FullName, "onnx-community", "kotoba-whisper-v2.2-ONNX", "added_tokens.json"),
            Path.Combine(directory.FullName, "onnx-community", "kotoba-whisper-v2.2-ONNX", "generation_config.json")
        };

        var missing = requiredFiles.Where(path => !File.Exists(path)).ToArray();
        if (missing.Length == 0)
        {
            return;
        }

        result.AddError($"models-root is incomplete. Missing files:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
    }

    /// <summary>
    /// Zeayii check whether current invocation targets init command.
    /// </summary>
    /// <param name="commandResult">Zeayii command result.</param>
    /// <returns>Zeayii init invocation flag.</returns>
    private static bool IsInitInvocation(CommandResult commandResult)
    {
        if (commandResult.Command.Name.Equals("init", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var child in commandResult.Children)
        {
            if (child is CommandResult childCommand && IsInitInvocation(childCommand))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Zeayii 校验正整数。
    /// </summary>
    /// <param name="value">Zeayii 待校验值。</param>
    /// <param name="result">Zeayii 选项校验结果。</param>
    private static void ValidatePositive(int value, OptionResult result)
    {
        if (value <= 0)
        {
            result.AddError($"{result.Option.Name} must be greater than 0.");
        }
    }

    /// <summary>
    /// Zeayii 校验非负整数。
    /// </summary>
    /// <param name="value">Zeayii 待校验值。</param>
    /// <param name="result">Zeayii 选项校验结果。</param>
    private static void ValidateNonNegative(int value, OptionResult result)
    {
        if (value < 0)
        {
            result.AddError($"{result.Option.Name} must be greater than or equal to 0.");
        }
    }

    /// <summary>
    /// Zeayii 校验非负浮点。
    /// </summary>
    /// <param name="value">Zeayii 待校验值。</param>
    /// <param name="result">Zeayii 选项校验结果。</param>
    private static void ValidateNonNegative(float value, OptionResult result)
    {
        if (value < 0f)
        {
            result.AddError($"{result.Option.Name} must be greater than or equal to 0.");
        }
    }

    /// <summary>
    /// Zeayii 校验大于等于边界值。
    /// </summary>
    /// <param name="value">Zeayii 待校验值。</param>
    /// <param name="min">Zeayii 下限值。</param>
    /// <param name="result">Zeayii 选项校验结果。</param>
    private static void ValidateGreaterOrEqual(float value, float min, OptionResult result)
    {
        if (value < min)
        {
            result.AddError($"{result.Option.Name} must be greater than or equal to {min}.");
        }
    }

    /// <summary>
    /// Zeayii 校验浮点范围。
    /// </summary>
    /// <param name="value">Zeayii 待校验值。</param>
    /// <param name="min">Zeayii 下限值。</param>
    /// <param name="max">Zeayii 上限值。</param>
    /// <param name="result">Zeayii 选项校验结果。</param>
    private static void ValidateRangeInclusive(float value, float min, float max, OptionResult result)
    {
        if (value < min || value > max)
        {
            result.AddError($"{result.Option.Name} must be in range [{min}, {max}].");
        }
    }

    /// <summary>
    /// Zeayii 校验语言标签并映射 BCP 47。
    /// </summary>
    /// <param name="result">Zeayii 选项校验结果。</param>
    private static void ValidateCulture(OptionResult result)
    {
        var value = result.Tokens.Count > 0 ? result.Tokens[^1].Value : null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        try
        {
            _ = CultureInfo.GetCultureInfo(value);
        }
        catch (CultureNotFoundException)
        {
            result.AddError($"{result.Option.Name} must be valid BCP 47 culture name.");
        }
    }

    /// <summary>
    /// Zeayii 解析默认模型目录。
    /// </summary>
    private static string ResolveDefaultModelsRoot() => Path.Combine(Environment.CurrentDirectory, "models");

    /// <summary>
    /// Zeayii 解析 FFmpeg 路径，失败时返回占位文件名用于触发 validator。
    /// </summary>
    private static string ResolveFfmpegPathOrPlaceholder()
    {
        var envPath = Environment.GetEnvironmentVariable("FFMPEG_PATH");
        if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
        {
            return Path.GetFullPath(envPath);
        }

        string[] candidates = OperatingSystem.IsWindows() ? ["ffmpeg.exe", "ffmpeg.cmd", "ffmpeg.bat"] : ["ffmpeg"];
        var pathSegments = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var path in pathSegments)
        {
            foreach (var candidate in candidates)
            {
                var target = Path.Combine(path, candidate);
                if (File.Exists(target))
                {
                    return target;
                }
            }
        }

        return OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
    }
}
