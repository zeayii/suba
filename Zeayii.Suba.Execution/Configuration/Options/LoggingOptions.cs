using Microsoft.Extensions.Logging;

namespace Zeayii.Suba.Core.Configuration.Options;

/// <summary>
/// Zeayii 日志输出配置。
/// </summary>
public sealed class LoggingOptions
{
    /// <summary>
    /// Zeayii 窗口日志等级。
    /// </summary>
    public required LogLevel ConsoleLogLevel { get; init; }

    /// <summary>
    /// Zeayii 文件日志等级。
    /// </summary>
    public required LogLevel FileLogLevel { get; init; }

    /// <summary>
    /// Zeayii 日志目录。
    /// </summary>
    public required string LogDirectory { get; init; }
}