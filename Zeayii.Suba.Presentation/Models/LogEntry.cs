using Microsoft.Extensions.Logging;

namespace Zeayii.Suba.Presentation.Models;

/// <summary>
/// Zeayii 窗口日志条目。
/// </summary>
/// <param name="TimestampText">Zeayii 时间文本。</param>
/// <param name="Level">Zeayii 日志等级。</param>
/// <param name="Tag">Zeayii 日志标签。</param>
/// <param name="Message">Zeayii 日志消息。</param>
public sealed record LogEntry(string TimestampText, LogLevel Level, string Tag, string Message);