namespace Zeayii.Suba.Core.Configuration.Policies;

/// <summary>
/// Zeayii 阶段执行设备策略。
/// </summary>
public enum ExecutionDevicePolicy : byte
{
    /// <summary>
    /// Zeayii 使用 CPU 执行。
    /// </summary>
    Cpu = 1,

    /// <summary>
    /// Zeayii 使用 GPU 执行。
    /// </summary>
    Gpu = 2
}