using Zeayii.Suba.Core.Abstractions;

namespace Zeayii.Suba.Core.Logging;

/// <summary>
/// Zeayii 双路日志输出实现（窗口 + 文件）。
/// </summary>
/// <param name="console">Zeayii 窗口日志输出器。</param>
/// <param name="file">Zeayii 文件日志输出器。</param>
internal sealed class DualLogOutput(IConsoleLogOutput console, IFileLogOutput file) : IDualLogOutput
{
    /// <summary>
    /// Zeayii 窗口日志输出器。
    /// </summary>
    private readonly IConsoleLogOutput _console = console ?? throw new ArgumentNullException(nameof(console));

    /// <summary>
    /// Zeayii 文件日志输出器。
    /// </summary>
    private readonly IFileLogOutput _file = file ?? throw new ArgumentNullException(nameof(file));

    /// <inheritdoc />
    public void Trace(string tag, string message, Exception? exception = null)
    {
        _console.Trace(tag, message, exception);
        _file.Trace(tag, message, exception);
    }

    /// <inheritdoc />
    public void Debug(string tag, string message, Exception? exception = null)
    {
        _console.Debug(tag, message, exception);
        _file.Debug(tag, message, exception);
    }

    /// <inheritdoc />
    public void Info(string tag, string message, Exception? exception = null)
    {
        _console.Info(tag, message, exception);
        _file.Info(tag, message, exception);
    }

    /// <inheritdoc />
    public void Warn(string tag, string message, Exception? exception = null)
    {
        _console.Warn(tag, message, exception);
        _file.Warn(tag, message, exception);
    }

    /// <inheritdoc />
    public void Error(string tag, string message, Exception? exception = null)
    {
        _console.Error(tag, message, exception);
        _file.Error(tag, message, exception);
    }

    /// <inheritdoc />
    public void Critical(string tag, string message, Exception? exception = null)
    {
        _console.Critical(tag, message, exception);
        _file.Critical(tag, message, exception);
    }
}
