using Microsoft.Extensions.Logging;
using Zeayii.Suba.Core.Abstractions;
using Zeayii.Suba.Core.Orchestration;
using TaskStatus = Zeayii.Suba.Core.Orchestration.TaskStatus;

namespace Zeayii.Suba.Core.Services;

/// <summary>
/// Zeayii 空窗口呈现管理器。
/// </summary>
internal sealed class NullPresentationManager : IPresentationManager
{
    /// <summary>
    /// Zeayii 启动空呈现循环。
    /// </summary>
    /// <param name="cancellationToken">Zeayii 取消令牌。</param>
    /// <returns>Zeayii 已完成任务。</returns>
    public Task RunAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Zeayii 停止空呈现循环。
    /// </summary>
    /// <returns>Zeayii 已完成值任务。</returns>
    public ValueTask StopAsync() => ValueTask.CompletedTask;

    /// <summary>
    /// Zeayii 空实现日志写入。
    /// </summary>
    /// <param name="level">Zeayii 日志等级。</param>
    /// <param name="tag">Zeayii 日志标签。</param>
    /// <param name="message">Zeayii 日志消息。</param>
    /// <param name="exception">Zeayii 异常对象。</param>
    public void WriteLog(LogLevel level, string tag, string message, Exception? exception = null) { }

    /// <summary>
    /// Zeayii 空实现任务状态更新。
    /// </summary>
    /// <param name="taskName">Zeayii 任务名称。</param>
    /// <param name="stage">Zeayii 任务阶段。</param>
    /// <param name="status">Zeayii 任务状态。</param>
    public void UpdateTask(string taskName, TaskStage stage, TaskStatus status) { }

    /// <summary>
    /// Zeayii 空实现日志等级设置。
    /// </summary>
    /// <param name="consoleLogLevel">Zeayii 窗口日志等级。</param>
    /// <param name="fileLogLevel">Zeayii 文件日志等级。</param>
    public void SetLogLevels(LogLevel consoleLogLevel, LogLevel fileLogLevel) { }
}
