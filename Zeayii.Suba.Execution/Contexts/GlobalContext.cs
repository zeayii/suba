using Zeayii.Suba.Core.Configuration.Options;
using Zeayii.Suba.Core.Abstractions;
using Zeayii.Suba.Core.Services;

namespace Zeayii.Suba.Core.Contexts;

/// <summary>
/// Zeayii 全局运行上下文。
/// </summary>
/// <param name="options">Zeayii 核心配置。</param>
/// <param name="console">Zeayii 窗口日志输出器。</param>
/// <param name="file">Zeayii 文件日志输出器。</param>
/// <param name="log">Zeayii 双路日志输出器。</param>
/// <param name="presentation">Zeayii 窗口呈现管理器。</param>
public sealed class GlobalContext(
    SubaOptions options,
    IConsoleLogOutput? console = null,
    IFileLogOutput? file = null,
    IDualLogOutput? log = null,
    IPresentationManager? presentation = null)
{
    /// <summary>
    /// Zeayii 固定目标采样率（Hz）。
    /// </summary>
    public const int DefaultTargetSampleRateHz = 16000;

    /// <summary>
    /// Zeayii 核心配置。
    /// </summary>
    public SubaOptions Options { get; } = options ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Zeayii 窗口日志输出器。
    /// </summary>
    public IConsoleLogOutput Console { get; } = console ?? new NullConsoleLogOutput();

    /// <summary>
    /// Zeayii 文件日志输出器。
    /// </summary>
    public IFileLogOutput File { get; } = file ?? new NullFileLogOutput();

    /// <summary>
    /// Zeayii 双路日志输出器。
    /// </summary>
    public IDualLogOutput Log { get; } = log ?? new NullDualLogOutput();

    /// <summary>
    /// Zeayii 窗口呈现管理器。
    /// </summary>
    public IPresentationManager Presentation { get; } = presentation ?? new NullPresentationManager();

    /// <summary>
    /// Zeayii 进程启动时间（UTC）。
    /// </summary>
    public DateTimeOffset ProcessStartedAtUtc { get; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Zeayii 当前进程会话标识。
    /// </summary>
    public string SessionId { get; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Zeayii 当前流程目标采样率（Hz）。
    /// </summary>
    public int TargetSampleRateHz { get; } = DefaultTargetSampleRateHz;
}
