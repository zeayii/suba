using Zeayii.Suba.Core.Orchestration;
using Zeayii.Suba.Presentation.Models;

namespace Zeayii.Suba.Presentation.Core.Progress;

/// <summary>
/// Zeayii 任务状态存储。
/// </summary>
internal sealed class TaskStore
{
    /// <summary>
    /// Zeayii 状态锁。
    /// </summary>
    private readonly Lock _syncRoot = new();

    /// <summary>
    /// Zeayii 任务字典。
    /// </summary>
    private readonly Dictionary<string, TaskSnapshot> _tasks = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Zeayii 更新任务快照。
    /// </summary>
    /// <param name="name">Zeayii 任务名称。</param>
    /// <param name="stage">Zeayii 任务阶段。</param>
    /// <param name="status">Zeayii 任务状态。</param>
    public void Update(string name, TaskStage stage, Zeayii.Suba.Core.Orchestration.TaskStatus status)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        lock (_syncRoot)
        {
            var now = DateTimeOffset.UtcNow;
            if (_tasks.TryGetValue(name, out var existing))
            {
                var startedAt = existing.StartedAtUtc;
                if (status == Zeayii.Suba.Core.Orchestration.TaskStatus.Running && startedAt is null)
                {
                    startedAt = now;
                }

                DateTimeOffset? completedAt = status is Zeayii.Suba.Core.Orchestration.TaskStatus.Succeeded or Zeayii.Suba.Core.Orchestration.TaskStatus.Failed
                    ? now
                    : null;
                _tasks[name] = existing with
                {
                    Stage = stage,
                    Status = status,
                    StartedAtUtc = startedAt,
                    CompletedAtUtc = completedAt
                };
            }
            else
            {
                DateTimeOffset? startedAt = status == Zeayii.Suba.Core.Orchestration.TaskStatus.Running ? now : null;
                DateTimeOffset? completedAt = status is Zeayii.Suba.Core.Orchestration.TaskStatus.Succeeded or Zeayii.Suba.Core.Orchestration.TaskStatus.Failed
                    ? now
                    : null;
                _tasks[name] = new TaskSnapshot(
                    name,
                    stage,
                    status,
                    startedAt,
                    completedAt
                );
            }
        }
    }

    /// <summary>
    /// Zeayii 获取任务快照。
    /// </summary>
    /// <returns>Zeayii 任务快照数组。</returns>
    public IReadOnlyList<TaskSnapshot> Snapshot()
    {
        lock (_syncRoot)
        {
            return _tasks.Values
                .OrderBy(static x => GetStatusPriority(x.Status))
                .ThenBy(static x => GetStagePriority(x.Stage))
                .ThenBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    /// <summary>
    /// Zeayii 获取状态排序优先级（数值越小优先级越高）。
    /// </summary>
    /// <param name="status">Zeayii 任务状态。</param>
    /// <returns>Zeayii 排序优先级。</returns>
    private static int GetStatusPriority(Zeayii.Suba.Core.Orchestration.TaskStatus status)
    {
        return status switch
        {
            Zeayii.Suba.Core.Orchestration.TaskStatus.Running => 0,
            Zeayii.Suba.Core.Orchestration.TaskStatus.Failed => 1,
            Zeayii.Suba.Core.Orchestration.TaskStatus.Succeeded => 2,
            Zeayii.Suba.Core.Orchestration.TaskStatus.Pending => 3,
            _ => 4
        };
    }

    /// <summary>
    /// Zeayii 获取阶段排序优先级（数值越小优先级越高）。
    /// </summary>
    /// <param name="stage">Zeayii 任务阶段。</param>
    /// <returns>Zeayii 阶段优先级。</returns>
    private static int GetStagePriority(TaskStage stage)
    {
        return stage switch
        {
            TaskStage.AudioPrepare => 0,
            TaskStage.Vad => 1,
            TaskStage.OverlapResolve => 2,
            TaskStage.Transcribe => 3,
            TaskStage.Translate => 4,
            TaskStage.SubtitleWrite => 5,
            TaskStage.Completed => 6,
            TaskStage.Failed => 7,
            TaskStage.None => 8,
            _ => 9
        };
    }
}
