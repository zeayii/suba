using Zeayii.Suba.Core.Configuration.Policies;

namespace Zeayii.Suba.Core.Configuration.Options;

/// <summary>
/// Zeayii 重叠检测阶段配置。
/// </summary>
public sealed class OverlapOptions
{
    /// <summary>
    /// Zeayii 重叠检测开关策略。
    /// </summary>
    public required StageSwitchPolicy DetectionPolicy { get; init; }

    /// <summary>
    /// Zeayii 重叠检测起始阈值（高阈值）。
    /// </summary>
    public required float Onset { get; init; }

    /// <summary>
    /// Zeayii 重叠检测结束阈值（低阈值）。
    /// </summary>
    public required float Offset { get; init; }

    /// <summary>
    /// Zeayii 最短重叠持续时长（秒）。
    /// </summary>
    public required float MinDurationOnSeconds { get; init; }

    /// <summary>
    /// Zeayii 最短非重叠持续时长（秒）。
    /// </summary>
    public required float MinDurationOffSeconds { get; init; }
}
