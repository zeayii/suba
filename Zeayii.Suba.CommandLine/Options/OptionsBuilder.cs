using Zeayii.Suba.Core.Configuration.Options;
using Zeayii.Suba.Core.Configuration.Policies;
using Zeayii.Suba.Core.Services;

namespace Zeayii.Suba.CommandLine.Options;

/// <summary>
/// Zeayii 选项构建器。
/// </summary>
internal static class OptionsBuilder
{
    /// <summary>
    /// Zeayii 将命令行应用配置构建为核心配置。
    /// </summary>
    /// <param name="applicationOptions">Zeayii 应用配置。</param>
    /// <returns>Zeayii 核心配置对象。</returns>
    public static SubaOptions BuildSubaOptions(ApplicationOptions applicationOptions)
    {
        ArgumentNullException.ThrowIfNull(applicationOptions);
        var languageTagResolver = new LanguageTagResolver();

        var modelsRoot = Path.GetFullPath(applicationOptions.ModelsRoot.FullName);
        var segmentationPath = Path.Combine(modelsRoot, "onnx-community", "pyannote-segmentation-3.0", "onnx", "model.onnx");
        var sepformerPath = Path.Combine(modelsRoot, "speechbrain", "sepformer-wsj02mix", "onnx", "model.onnx");
        var whisperRoot = Path.Combine(modelsRoot, "onnx-community", "kotoba-whisper-v2.2-ONNX");
        var fixedTranscribeLanguageTag = applicationOptions.TranscribeLanguagePolicy == LanguagePolicy.Fixed ? languageTagResolver.NormalizeBcp47(applicationOptions.TranscribeLanguageTag) : string.Empty;
        var outputTranscribeLanguageTag = applicationOptions.TranscribeLanguagePolicy == LanguagePolicy.Fixed ? fixedTranscribeLanguageTag : "und";
        var modelLanguageCode = applicationOptions.TranscribeLanguagePolicy == LanguagePolicy.Fixed ? languageTagResolver.ResolveIso6391Code(fixedTranscribeLanguageTag) : string.Empty;

        return SubaOptionsBuilder.Create()
            .SetModelsRoot(modelsRoot)
            .SetSubtitleFormatPolicy(applicationOptions.SubtitleFormatPolicy)
            .SetAudioExtract(Path.GetFullPath(applicationOptions.FfmpegPath.FullName), Path.GetFullPath(applicationOptions.CacheDirectory.FullName))
            .SetLogging(
                applicationOptions.ConsoleLogLevel,
                applicationOptions.FileLogLevel,
                Path.GetFullPath(applicationOptions.LogDirectory.FullName)
            )
            .SetRuntime(
                applicationOptions.MaxDegreeOfParallelism,
                applicationOptions.CommandTimeoutSeconds,
                applicationOptions.PreprocessDevice,
                applicationOptions.PreprocessParallelism,
                applicationOptions.TranscribeDevice,
                applicationOptions.TranscribeParallelism,
                applicationOptions.TranslateDevice,
                applicationOptions.TranslateParallelism,
                applicationOptions.TranslationExecutionMode,
                applicationOptions.GpuConflictPolicy,
                applicationOptions.ArtifactOverwritePolicy
            )
            .SetVad(
                applicationOptions.VadThreshold,
                applicationOptions.VadMinSilenceMs,
                applicationOptions.VadMinSpeechMs,
                applicationOptions.VadMaxSpeechSeconds,
                applicationOptions.VadSpeechPadMs,
                applicationOptions.VadNegThreshold,
                applicationOptions.VadMinSilenceAtMaxSpeechMs,
                applicationOptions.VadUseMaxPossibleSilenceAtMaxSpeech
            )
            .SetOverlapPolicy(
                applicationOptions.OverlapDetectionPolicy,
                applicationOptions.OverlapOnset,
                applicationOptions.OverlapOffset,
                applicationOptions.OverlapMinDurationOnSeconds,
                applicationOptions.OverlapMinDurationOffSeconds
            )
            .SetSeparatedVad(
                applicationOptions.SeparatedVadMinSpeechMs,
                applicationOptions.SeparatedVadMaxSpeechMs,
                applicationOptions.SeparatedVadSpeechPadMs,
                applicationOptions.SeparatedVadNegThreshold,
                applicationOptions.SeparatedVadMinSilenceAtMaxSpeechMs,
                applicationOptions.SeparatedVadUseMaxPossibleSilenceAtMaxSpeech
            )
            .SetSepformer(applicationOptions.SepformerNormalizeOutput)
            .SetTranscription(
                applicationOptions.TranscribeLanguagePolicy,
                fixedTranscribeLanguageTag,
                outputTranscribeLanguageTag,
                modelLanguageCode,
                applicationOptions.NoSpeechThreshold,
                applicationOptions.TranscribeMaxNewTokens,
                applicationOptions.TranscribeTemperature,
                applicationOptions.TranscribeBeamSize,
                applicationOptions.TranscribeBestOf,
                applicationOptions.TranscribeLengthPenalty,
                applicationOptions.TranscribeRepetitionPenalty,
                applicationOptions.TranscribeSuppressBlank,
                applicationOptions.TranscribeSuppressTokens,
                applicationOptions.TranscribeWithoutTimestamps
            )
            .SetTranslation(
                applicationOptions.TranslationProvider,
                applicationOptions.TranslateLanguage,
                applicationOptions.OllamaBaseUrl.ToString(),
                applicationOptions.OllamaModel,
                applicationOptions.OpenAiBaseUrl.ToString(),
                applicationOptions.OpenAiApiKey,
                applicationOptions.OpenAiModel,
                applicationOptions.TranslateResponseMode,
                applicationOptions.TranslateContextQueueSize,
                applicationOptions.TranslateContextGapMs,
                applicationOptions.TranslatePartialWriteInterval
            )
            .SetModelResolvedPaths(segmentationPath, sepformerPath, whisperRoot)
            .Build();
    }
}
