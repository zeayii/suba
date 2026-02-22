namespace Zeayii.Suba.Core.Orchestration;

/// <summary>
/// Zeayii 任务执行状态。
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// Zeayii 等待执行。
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Zeayii 执行中。
    /// </summary>
    Running = 1,

    /// <summary>
    /// Zeayii 执行成功。
    /// </summary>
    Succeeded = 2,

    /// <summary>
    /// Zeayii 执行失败。
    /// </summary>
    Failed = 3
}
