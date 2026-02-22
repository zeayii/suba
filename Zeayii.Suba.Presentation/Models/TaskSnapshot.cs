using Zeayii.Suba.Core.Orchestration;

namespace Zeayii.Suba.Presentation.Models;

/// <summary>
/// Zeayii 任务快照模型。
/// </summary>
/// <param name="Name">Zeayii 任务名称。</param>
/// <param name="Stage">Zeayii 任务阶段。</param>
/// <param name="Status">Zeayii 任务状态。</param>
/// <param name="StartedAtUtc">Zeayii 任务执行开始时间（UTC）。</param>
/// <param name="CompletedAtUtc">Zeayii 任务完成时间（UTC）。</param>
public sealed record TaskSnapshot(
    string Name,
    TaskStage Stage,
    Zeayii.Suba.Core.Orchestration.TaskStatus Status,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc
);
