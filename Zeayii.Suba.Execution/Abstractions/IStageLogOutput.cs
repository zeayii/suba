namespace Zeayii.Suba.Core.Abstractions;

/// <summary>
/// Zeayii 统一阶段日志输出契约。
/// </summary>
public interface IStageLogOutput
{
    /// <summary>
    /// Zeayii 输出 Trace 级日志。
    /// </summary>
    void Trace(string tag, string message, Exception? exception = null);

    /// <summary>
    /// Zeayii 输出 Debug 级日志。
    /// </summary>
    void Debug(string tag, string message, Exception? exception = null);

    /// <summary>
    /// Zeayii 输出 Information 级日志。
    /// </summary>
    void Info(string tag, string message, Exception? exception = null);

    /// <summary>
    /// Zeayii 输出 Warning 级日志。
    /// </summary>
    void Warn(string tag, string message, Exception? exception = null);

    /// <summary>
    /// Zeayii 输出 Error 级日志。
    /// </summary>
    void Error(string tag, string message, Exception? exception = null);

    /// <summary>
    /// Zeayii 输出 Critical 级日志。
    /// </summary>
    void Critical(string tag, string message, Exception? exception = null);
}