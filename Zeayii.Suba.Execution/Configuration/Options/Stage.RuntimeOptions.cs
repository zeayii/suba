using Zeayii.Suba.Core.Configuration.Policies;

namespace Zeayii.Suba.Core.Configuration.Options;

/// <summary>
/// Zeayii 运行阶段配置。
/// </summary>
public sealed class RuntimeOptions
{
    /// <summary>
    /// Zeayii 最大并发度。
    /// </summary>
    public required int MaxDegreeOfParallelism { get; init; }

    /// <summary>
    /// Zeayii 外部命令超时（秒）。
    /// </summary>
    public required int CommandTimeoutSeconds { get; init; }

    /// <summary>
    /// Zeayii 前处理阶段执行配置。
    /// </summary>
    public required StageExecutionOptions Preprocess { get; init; }

    /// <summary>
    /// Zeayii 转录阶段执行配置。
    /// </summary>
    public required StageExecutionOptions Transcribe { get; init; }

    /// <summary>
    /// Zeayii 翻译阶段执行配置。
    /// </summary>
    public required StageExecutionOptions Translate { get; init; }

    /// <summary>
    /// Zeayii 翻译执行模式策略。
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
