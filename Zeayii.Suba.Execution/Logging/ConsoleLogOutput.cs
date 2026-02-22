using Microsoft.Extensions.Logging;
using Zeayii.Suba.Core.Abstractions;
using Zeayii.Suba.Core.Configuration.Options;

namespace Zeayii.Suba.Core.Logging;

/// <summary>
/// Zeayii 窗口日志输出实现。
/// </summary>
/// <param name="presentationManager">Zeayii 呈现管理器。</param>
/// <param name="options">Zeayii 核心配置。</param>
internal sealed class ConsoleLogOutput(IPresentationManager presentationManager, SubaOptions options) : IConsoleLogOutput
{
    /// <summary>
    /// Zeayii 窗口呈现管理器。
    /// </summary>
    private readonly IPresentationManager _presentationManager = presentationManager ?? throw new ArgumentNullException(nameof(presentationManager));

    /// <summary>
    /// Zeayii 核心配置。
    /// </summary>
    private readonly SubaOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Zeayii 写入 Trace 级别日志到窗口。
    /// </summary>
    /// <param name="tag">Zeayii 日志标签。</param>
    /// <param name="message">Zeayii 日志消息。</param>
    /// <param name="exception">Zeayii 异常对象。</param>
    public void Trace(string tag, string message, Exception? exception = null) => Write(LogLevel.Trace, tag, message, exception);

    /// <summary>
    /// Zeayii 写入 Debug 级别日志到窗口。
    /// </summary>
    /// <param name="tag">Zeayii 日志标签。</param>
    /// <param name="message">Zeayii 日志消息。</param>
    /// <param name="exception">Zeayii 异常对象。</param>
    public void Debug(string tag, string message, Exception? exception = null) => Write(LogLevel.Debug, tag, message, exception);

    /// <summary>
    /// Zeayii 写入 Information 级别日志到窗口。
    /// </summary>
    /// <param name="tag">Zeayii 日志标签。</param>
    /// <param name="message">Zeayii 日志消息。</param>
    /// <param name="exception">Zeayii 异常对象。</param>
    public void Info(string tag, string message, Exception? exception = null) => Write(LogLevel.Information, tag, message, exception);

    /// <summary>
    /// Zeayii 写入 Warning 级别日志到窗口。
    /// </summary>
    /// <param name="tag">Zeayii 日志标签。</param>
    /// <param name="message">Zeayii 日志消息。</param>
    /// <param name="exception">Zeayii 异常对象。</param>
    public void Warn(string tag, string message, Exception? exception = null) => Write(LogLevel.Warning, tag, message, exception);

    /// <summary>
    /// Zeayii 写入 Error 级别日志到窗口。
    /// </summary>
    /// <param name="tag">Zeayii 日志标签。</param>
    /// <param name="message">Zeayii 日志消息。</param>
    /// <param name="exception">Zeayii 异常对象。</param>
    public void Error(string tag, string message, Exception? exception = null) => Write(LogLevel.Error, tag, message, exception);

    /// <summary>
    /// Zeayii 写入 Critical 级别日志到窗口。
    /// </summary>
    /// <param name="tag">Zeayii 日志标签。</param>
    /// <param name="message">Zeayii 日志消息。</param>
    /// <param name="exception">Zeayii 异常对象。</param>
    public void Critical(string tag, string message, Exception? exception = null) => Write(LogLevel.Critical, tag, message, exception);

    /// <summary>
    /// Zeayii 执行等级过滤并输出到窗口。
    /// </summary>
    /// <param name="level">Zeayii 日志等级。</param>
    /// <param name="tag">Zeayii 日志标签。</param>
    /// <param name="message">Zeayii 日志消息。</param>
    /// <param name="exception">Zeayii 异常对象。</param>
    private void Write(LogLevel level, string tag, string message, Exception? exception)
    {
        if (level < _options.Logging.ConsoleLogLevel)
        {
            return;
        }

        _presentationManager.WriteLog(level, tag, message, exception);
    }
}
