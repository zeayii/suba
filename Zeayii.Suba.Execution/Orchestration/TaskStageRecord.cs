namespace Zeayii.Suba.Core.Orchestration;

/// <summary>
/// Zeayii 任务阶段执行记录。
/// </summary>
public sealed class TaskStageRecord
{
    /// <summary>
    /// Zeayii 阶段标识。
    /// </summary>
    public required TaskStage Stage { get; init; }

    /// <summary>
    /// Zeayii 阶段状态。
    /// </summary>
    public required TaskStatus Status { get; set; }

    /// <summary>
    /// Zeayii 阶段开始时间（UTC）。
    /// </summary>
    public required DateTimeOffset StartedAtUtc { get; init; }

    /// <summary>
    /// Zeayii 阶段结束时间（UTC）。
    /// </summary>
    public DateTimeOffset? EndedAtUtc { get; set; }

    /// <summary>
    /// Zeayii 阶段错误消息。
    /// </summary>
    public string? ErrorMessage { get; set; }
}
