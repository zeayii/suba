using Microsoft.Extensions.Logging;

namespace Zeayii.Suba.Presentation.Window.State;

/// <summary>
/// Zeayii 仪表盘运行状态。
/// </summary>
internal sealed class DashboardState
{
    /// <summary>
    /// Zeayii 可聚焦区域。
    /// </summary>
    public enum FocusRegion
    {
        Tasks = 0,
        Logs = 1
    }

    /// <summary>
    /// Zeayii 任务区域滚动状态。
    /// </summary>
    public ScrollRegion TaskRegion { get; } = new();

    /// <summary>
    /// Zeayii 日志区域滚动状态。
    /// </summary>
    public ScrollRegion LogRegion { get; } = new();

    /// <summary>
    /// Zeayii 控制台日志等级显示值。
    /// </summary>
    public LogLevel ConsoleLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Zeayii 文件日志等级显示值。
    /// </summary>
    public LogLevel FileLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Zeayii 退出标记。
    /// </summary>
    public bool ShouldExit { get; set; }

    /// <summary>
    /// Zeayii 退出确认状态（Q 后等待 Enter）。
    /// </summary>
    public bool ExitPending { get; set; }

    /// <summary>
    /// Zeayii 当前焦点区域。
    /// </summary>
    public FocusRegion ActiveRegion { get; set; } = FocusRegion.Tasks;

    /// <summary>
    /// Zeayii 是否跟随日志底部。
    /// </summary>
    public bool AutoFollowLog { get; set; } = true;

    /// <summary>
    /// Zeayii 是否跟随任务顶部。
    /// </summary>
    public bool AutoFollowTask { get; set; } = true;
}
