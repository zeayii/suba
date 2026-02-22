using System.Globalization;
using Microsoft.Extensions.Logging;
using Zeayii.Suba.Core.Configuration.Options;
using Zeayii.Suba.Core.Configuration.Policies;

namespace Zeayii.Suba.Execution.Tests.TestSupport;

/// <summary>
/// Zeayii 测试用核心配置工厂。
/// </summary>
internal static class TestSubaOptionsFactory
{
    /// <summary>
    /// Zeayii 创建最小可运行配置。
    /// </summary>
    /// <returns>Zeayii 核心配置。</returns>
    public static SubaOptions Create()
    {
        return new SubaOptions
        {
            ModelsRoot = "models",
            SegmentationModelPath = "seg.onnx",
            SepformerModelPath = "sepformer.onnx",
            WhisperModelRoot = "whisper",
            SubtitleFormatPolicy = SubtitleFormatPolicy.Vtt,
            AudioExtract = new AudioExtractOptions
            {
                FfmpegPath = "ffmpeg",
                CacheDirectory = Path.GetTempPath()
            },
            Vad = new VadOptions
            {
                Threshold = 0.35f,
                MinSilenceMs = 200,
                MinSpeechMs = 400,
                MaxSpeechSeconds = 10.0f,
                SpeechPadMs = 0,
                NegThreshold = 0.2f,
                MinSilenceAtMaxSpeechMs = 98f,
                UseMaxPossibleSilenceAtMaxSpeech = true
            },
            Overlap = new OverlapOptions
            {
                DetectionPolicy = StageSwitchPolicy.Disabled,
                Onset = 0.8104268538848918f,
                Offset = 0.4806866463041527f,
                MinDurationOnSeconds = 0.05537587440407595f,
                MinDurationOffSeconds = 0.09791355693027545f
            },
            Separation = new SeparationOptions
            {
                SeparatedVadMinSpeechMs = 300,
                SeparatedVadMaxSpeechMs = 8000,
                SeparatedVadSpeechPadMs = 0,
                SeparatedVadNegThreshold = 0.2f,
                SeparatedVadMinSilenceAtMaxSpeechMs = 98f,
                SeparatedVadUseMaxPossibleSilenceAtMaxSpeech = true
            },
            Sepformer = new SepformerOptions
            {
                NormalizeOutput = true
            },
            Transcription = new TranscriptionOptions
            {
                LanguagePolicy = LanguagePolicy.Fixed,
                FixedLanguageTag = "ja-JP",
                OutputLanguageTag = "ja-JP",
                ModelLanguageCode = "ja",
                NoSpeechThreshold = 0.8f,
                MaxNewTokens = 128,
                Temperature = 0f,
                BeamSize = 5,
                BestOf = 5,
                LengthPenalty = 1f,
                RepetitionPenalty = 1f,
                SuppressBlank = true,
                SuppressTokens = [-1],
                WithoutTimestamps = true
            },
            Translation = new TranslationOptions
            {
                Provider = TranslationProviderPolicy.Ollama,
                Language = CultureInfo.GetCultureInfo("zh-CN"),
                OllamaBaseUrl = "http://localhost:11434",
                OllamaModel = "qwen3:14b",
                OpenAiBaseUrl = "https://api.openai.com",
                OpenAiApiKey = string.Empty,
                OpenAiModel = "gpt-4o-mini",
                ResponseMode = TranslationResponseMode.NonStreaming,
                ContextQueueSize = 6,
                ContextGapMs = 20000,
                PartialWriteInterval = 300
            },
            Runtime = new RuntimeOptions
            {
                MaxDegreeOfParallelism = 1,
                CommandTimeoutSeconds = 120,
                Preprocess = new StageExecutionOptions
                {
                    Device = ExecutionDevicePolicy.Cpu,
                    Parallelism = 1
                },
                Transcribe = new StageExecutionOptions
                {
                    Device = ExecutionDevicePolicy.Cpu,
                    Parallelism = 1
                },
                Translate = new StageExecutionOptions
                {
                    Device = ExecutionDevicePolicy.Cpu,
                    Parallelism = 1
                },
                TranslationExecutionMode = TranslationExecutionModePolicy.PerTask,
                GpuConflictPolicy = GpuConflictPolicy.TranscribeVsTranslate,
                ArtifactOverwritePolicy = ArtifactOverwritePolicy.SkipExisting
            },
            Logging = new LoggingOptions
            {
                ConsoleLogLevel = LogLevel.Information,
                FileLogLevel = LogLevel.Information,
                LogDirectory = Path.Combine(Path.GetTempPath(), "suba-test-logs")
            }
        };
    }
}
