using Microsoft.Extensions.Logging;
using Zeayii.Suba.Presentation.Configuration;
using Zeayii.Suba.Presentation.Models;

namespace Zeayii.Suba.Presentation.Core.Logging;

/// <summary>
/// Zeayii 窗口日志存储。
/// </summary>
/// <param name="options">Zeayii 呈现层配置。</param>
internal sealed class LogStore(PresentationOptions options)
{
    /// <summary>
    /// Zeayii 配置对象。
    /// </summary>
    private readonly PresentationOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Zeayii 日志锁。
    /// </summary>
    private readonly Lock _syncRoot = new();

    /// <summary>
    /// Zeayii 日志缓存。
    /// </summary>
    private readonly List<LogEntry> _entries = [];

    /// <summary>
    /// Zeayii 写入日志。
    /// </summary>
    /// <param name="level">Zeayii 日志等级。</param>
    /// <param name="tag">Zeayii 日志标签。</param>
    /// <param name="message">Zeayii 日志消息。</param>
    /// <param name="exception">Zeayii 异常对象。</param>
    public void Write(LogLevel level, string tag, string message, Exception? exception = null)
    {
        var text = exception is null ? message : $"{message} | {exception.Message}";
        var entry = new LogEntry(DateTimeOffset.Now.ToString("HH:mm:ss"), level, tag, text);
        lock (_syncRoot)
        {
            _entries.Add(entry);
            if (_entries.Count > _options.MaxLogLines)
            {
                _entries.RemoveRange(0, _entries.Count - _options.MaxLogLines);
            }
        }
    }

    /// <summary>
    /// Zeayii 获取日志快照。
    /// </summary>
    /// <returns>Zeayii 日志快照。</returns>
    public IReadOnlyList<LogEntry> Snapshot()
    {
        lock (_syncRoot)
        {
            return _entries.ToArray();
        }
    }
}
