using Zeayii.Suba.Core.Abstractions;

namespace Zeayii.Suba.Core.Services;

/// <summary>
/// Zeayii 空文件日志输出器。
/// </summary>
internal sealed class NullFileLogOutput : IFileLogOutput
{
    /// <summary>
    /// Zeayii 空实现 Trace 输出。
    /// </summary>
    /// <param name="tag">Zeayii 日志标签。</param>
    /// <param name="message">Zeayii 日志消息。</param>
    /// <param name="exception">Zeayii 异常对象。</param>
    public void Trace(string tag, string message, Exception? exception = null) { }

    /// <summary>
    /// Zeayii 空实现 Debug 输出。
    /// </summary>
    /// <param name="tag">Zeayii 日志标签。</param>
    /// <param name="message">Zeayii 日志消息。</param>
    /// <param name="exception">Zeayii 异常对象。</param>
    public void Debug(string tag, string message, Exception? exception = null) { }

    /// <summary>
    /// Zeayii 空实现 Information 输出。
    /// </summary>
    /// <param name="tag">Zeayii 日志标签。</param>
    /// <param name="message">Zeayii 日志消息。</param>
    /// <param name="exception">Zeayii 异常对象。</param>
    public void Info(string tag, string message, Exception? exception = null) { }

    /// <summary>
    /// Zeayii 空实现 Warning 输出。
    /// </summary>
    /// <param name="tag">Zeayii 日志标签。</param>
    /// <param name="message">Zeayii 日志消息。</param>
    /// <param name="exception">Zeayii 异常对象。</param>
    public void Warn(string tag, string message, Exception? exception = null) { }

    /// <summary>
    /// Zeayii 空实现 Error 输出。
    /// </summary>
    /// <param name="tag">Zeayii 日志标签。</param>
    /// <param name="message">Zeayii 日志消息。</param>
    /// <param name="exception">Zeayii 异常对象。</param>
    public void Error(string tag, string message, Exception? exception = null) { }

    /// <summary>
    /// Zeayii 空实现 Critical 输出。
    /// </summary>
    /// <param name="tag">Zeayii 日志标签。</param>
    /// <param name="message">Zeayii 日志消息。</param>
    /// <param name="exception">Zeayii 异常对象。</param>
    public void Critical(string tag, string message, Exception? exception = null) { }
}
