using System.Text;
using Microsoft.Extensions.Logging;
using Zeayii.Suba.Core.Abstractions;
using Zeayii.Suba.Core.Configuration.Options;

namespace Zeayii.Suba.Core.Logging;

/// <summary>
/// Zeayii 文件日志输出实现。
/// </summary>
/// <param name="options">Zeayii 核心配置。</param>
internal sealed class FileLogOutput(SubaOptions options) : IFileLogOutput, IDisposable
{
    /// <summary>
    /// Zeayii 核心配置。
    /// </summary>
    private readonly SubaOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Zeayii 文件写入锁。
    /// </summary>
    private readonly Lock _syncRoot = new();

    /// <summary>
    /// Zeayii 日志写入器。
    /// </summary>
    private StreamWriter? _writer;

    /// <summary>
    /// Zeayii 是否已输出文件写失败提示。
    /// </summary>
    private int _failureReported;

    /// <summary>
    /// Zeayii 写入 Trace 级别日志到文件。
    /// </summary>
    /// <param name="tag">Zeayii 日志标签。</param>
    /// <param name="message">Zeayii 日志消息。</param>
    /// <param name="exception">Zeayii 异常对象。</param>
    public void Trace(string tag, string message, Exception? exception = null) => Write(LogLevel.Trace, tag, message, exception);

    /// <summary>
    /// Zeayii 写入 Debug 级别日志到文件。
    /// </summary>
    /// <param name="tag">Zeayii 日志标签。</param>
    /// <param name="message">Zeayii 日志消息。</param>
    /// <param name="exception">Zeayii 异常对象。</param>
    public void Debug(string tag, string message, Exception? exception = null) => Write(LogLevel.Debug, tag, message, exception);

    /// <summary>
    /// Zeayii 写入 Information 级别日志到文件。
    /// </summary>
    /// <param name="tag">Zeayii 日志标签。</param>
    /// <param name="message">Zeayii 日志消息。</param>
    /// <param name="exception">Zeayii 异常对象。</param>
    public void Info(string tag, string message, Exception? exception = null) => Write(LogLevel.Information, tag, message, exception);

    /// <summary>
    /// Zeayii 写入 Warning 级别日志到文件。
    /// </summary>
    /// <param name="tag">Zeayii 日志标签。</param>
    /// <param name="message">Zeayii 日志消息。</param>
    /// <param name="exception">Zeayii 异常对象。</param>
    public void Warn(string tag, string message, Exception? exception = null) => Write(LogLevel.Warning, tag, message, exception);

    /// <summary>
    /// Zeayii 写入 Error 级别日志到文件。
    /// </summary>
    /// <param name="tag">Zeayii 日志标签。</param>
    /// <param name="message">Zeayii 日志消息。</param>
    /// <param name="exception">Zeayii 异常对象。</param>
    public void Error(string tag, string message, Exception? exception = null) => Write(LogLevel.Error, tag, message, exception);

    /// <summary>
    /// Zeayii 写入 Critical 级别日志到文件。
    /// </summary>
    /// <param name="tag">Zeayii 日志标签。</param>
    /// <param name="message">Zeayii 日志消息。</param>
    /// <param name="exception">Zeayii 异常对象。</param>
    public void Critical(string tag, string message, Exception? exception = null) => Write(LogLevel.Critical, tag, message, exception);

    /// <summary>
    /// Zeayii 执行等级过滤并写入文件。
    /// </summary>
    /// <param name="level">Zeayii 日志等级。</param>
    /// <param name="tag">Zeayii 日志标签。</param>
    /// <param name="message">Zeayii 日志消息。</param>
    /// <param name="exception">Zeayii 异常对象。</param>
    private void Write(LogLevel level, string tag, string message, Exception? exception)
    {
        if (level < _options.Logging.FileLogLevel)
        {
            return;
        }

        lock (_syncRoot)
        {
            try
            {
                EnsureWriter();
                if (_writer is null)
                {
                    return;
                }

                var line = $"{DateTimeOffset.Now:O} [{level}] [{tag}] {message}";
                _writer.WriteLine(line);
                if (exception is not null)
                {
                    _writer.WriteLine(exception);
                }

                _writer.Flush();
            }
            catch (Exception ex)
            {
                if (Interlocked.Exchange(ref _failureReported, 1) == 0)
                {
                    Console.Error.WriteLine($"FileLogOutput unavailable: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Zeayii 确保日志写入器已初始化。
    /// </summary>
    private void EnsureWriter()
    {
        if (_writer is not null)
        {
            return;
        }

        var directory = _options.Logging.LogDirectory;
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, $"suba-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.log");
        _writer = new StreamWriter(filePath, append: true, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_syncRoot)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }
}
