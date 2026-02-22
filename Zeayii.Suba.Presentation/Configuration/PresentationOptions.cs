namespace Zeayii.Suba.Presentation.Configuration;

/// <summary>
/// Zeayii 呈现层配置。
/// </summary>
internal sealed class PresentationOptions
{
    /// <summary>
    /// Zeayii 刷新间隔（毫秒）。
    /// </summary>
    public int RefreshIntervalMs { get; init; } = 100;

    /// <summary>
    /// Zeayii 日志最大缓存行数。
    /// </summary>
    public int MaxLogLines { get; init; } = 2000;

    /// <summary>
    /// Zeayii 任务面板最小宽度。
    /// </summary>
    public int MinTaskPanelWidth { get; init; } = 42;

    /// <summary>
    /// Zeayii 日志面板最小宽度。
    /// </summary>
    public int MinLogPanelWidth { get; init; } = 48;
}