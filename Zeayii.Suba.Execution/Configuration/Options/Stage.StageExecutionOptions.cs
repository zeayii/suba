using Zeayii.Suba.Core.Configuration.Policies;

namespace Zeayii.Suba.Core.Configuration.Options;

/// <summary>
/// Zeayii 单阶段执行配置。
/// </summary>
public sealed class StageExecutionOptions
{
    /// <summary>
    /// Zeayii 阶段执行设备。
    /// </summary>
    public required ExecutionDevicePolicy Device { get; init; }

    /// <summary>
    /// Zeayii 阶段并发执行数量。
    /// </summary>
    public required int Parallelism { get; init; }
}