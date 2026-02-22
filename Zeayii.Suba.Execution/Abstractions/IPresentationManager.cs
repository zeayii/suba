using Microsoft.Extensions.Logging;
using Zeayii.Suba.Core.Orchestration;
using TaskStatus = Zeayii.Suba.Core.Orchestration.TaskStatus;

namespace Zeayii.Suba.Core.Abstractions;

/// <summary>
/// Zeayii 窗口呈现管理器契约。
/// </summary>
public interface IPresentationManager
{
    /// <summary>
    /// Zeayii 启动窗口循环。
    /// </summary>
    /// <param name="cancellationToken">Zeayii 取消令牌。</param>
    /// <returns>Zeayii 异步任务。</returns>
    Task RunAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Zeayii 停止窗口循环。
    /// </summary>
    /// <returns>Zeayii 异步任务。</returns>
    ValueTask StopAsync();

    /// <summary>
    /// Zeayii 写入窗口日志。
    /// </summary>
    /// <param name="level">Zeayii 日志等级。</param>
    /// <param name="tag">Zeayii 日志标签。</param>
    /// <param name="message">Zeayii 日志消息。</param>
    /// <param name="exception">Zeayii 异常对象。</param>
    void WriteLog(LogLevel level, string tag, string message, Exception? exception = null);

    /// <summary>
    /// Zeayii 更新任务阶段状态。
    /// </summary>
    /// <param name="taskName">Zeayii 任务名称。</param>
    /// <param name="stage">Zeayii 任务阶段。</param>
    /// <param name="status">Zeayii 任务状态。</param>
    void UpdateTask(string taskName, TaskStage stage, TaskStatus status);

    /// <summary>
    /// Zeayii 设置标题栏日志等级信息。
    /// </summary>
    /// <param name="consoleLogLevel">Zeayii 窗口日志等级。</param>
    /// <param name="fileLogLevel">Zeayii 文件日志等级。</param>
    void SetLogLevels(LogLevel consoleLogLevel, LogLevel fileLogLevel);
}
