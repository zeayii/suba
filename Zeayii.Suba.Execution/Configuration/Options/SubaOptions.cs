using Zeayii.Suba.Core.Configuration.Policies;

namespace Zeayii.Suba.Core.Configuration.Options;

/// <summary>
/// Zeayii 核心运行配置。
/// </summary>
public sealed class SubaOptions
{
    /// <summary>
    /// Zeayii 模型根目录绝对路径。
    /// </summary>
    public required string ModelsRoot { get; init; }

    /// <summary>
    /// Zeayii Pyannote 分割模型路径。
    /// </summary>
    public required string SegmentationModelPath { get; init; }

    /// <summary>
    /// Zeayii SepFormer 模型路径。
    /// </summary>
    public required string SepformerModelPath { get; init; }

    /// <summary>
    /// Zeayii Whisper 模型目录路径。
    /// </summary>
    public required string WhisperModelRoot { get; init; }

    /// <summary>
    /// Zeayii 字幕格式策略。
    /// </summary>
    public required SubtitleFormatPolicy SubtitleFormatPolicy { get; init; }

    /// <summary>
    /// Zeayii 音频提取阶段参数。
    /// </summary>
    public required AudioExtractOptions AudioExtract { get; init; }

    /// <summary>
    /// Zeayii VAD 阶段参数。
    /// </summary>
    public required VadOptions Vad { get; init; }

    /// <summary>
    /// Zeayii 重叠检测阶段参数。
    /// </summary>
    public required OverlapOptions Overlap { get; init; }

    /// <summary>
    /// Zeayii 分离后二次 VAD 阶段参数。
    /// </summary>
    public required SeparationOptions Separation { get; init; }

    /// <summary>
    /// Zeayii SepFormer 分离阶段参数。
    /// </summary>
    public required SepformerOptions Sepformer { get; init; }

    /// <summary>
    /// Zeayii 转写阶段参数。
    /// </summary>
    public required TranscriptionOptions Transcription { get; init; }

    /// <summary>
    /// Zeayii 翻译阶段参数。
    /// </summary>
    public required TranslationOptions Translation { get; init; }

    /// <summary>
    /// Zeayii 运行阶段参数。
    /// </summary>
    public required RuntimeOptions Runtime { get; init; }

    /// <summary>
    /// Zeayii 日志输出参数。
    /// </summary>
    public required LoggingOptions Logging { get; init; }
}
