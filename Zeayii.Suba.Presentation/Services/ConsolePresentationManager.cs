using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Rendering;
using Zeayii.Suba.Core.Abstractions;
using Zeayii.Suba.Core.Orchestration;
using Zeayii.Suba.Presentation.Configuration;
using Zeayii.Suba.Presentation.Core.Logging;
using Zeayii.Suba.Presentation.Core.Progress;
using Zeayii.Suba.Presentation.Window.Input;
using Zeayii.Suba.Presentation.Window.Layout;
using Zeayii.Suba.Presentation.Window.State;
using TaskStatus = Zeayii.Suba.Core.Orchestration.TaskStatus;

namespace Zeayii.Suba.Presentation.Services;

/// <summary>
/// Zeayii Spectre 控制台呈现管理器。
/// </summary>
/// <param name="logStore">Zeayii 日志存储。</param>
/// <param name="taskStore">Zeayii 任务存储。</param>
/// <param name="options">Zeayii 呈现配置。</param>
internal sealed class ConsolePresentationManager(LogStore logStore, TaskStore taskStore, PresentationOptions options) : IPresentationManager
{
    /// <summary>
    /// Zeayii 日志存储。
    /// </summary>
    private readonly LogStore _logStore = logStore ?? throw new ArgumentNullException(nameof(logStore));

    /// <summary>
    /// Zeayii 任务存储。
    /// </summary>
    private readonly TaskStore _taskStore = taskStore ?? throw new ArgumentNullException(nameof(taskStore));

    /// <summary>
    /// Zeayii 配置对象。
    /// </summary>
    private readonly PresentationOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Zeayii 仪表盘状态。
    /// </summary>
    private readonly DashboardState _state = new();

    /// <summary>
    /// Zeayii 启动标记。
    /// </summary>
    private int _started;

    /// <summary>
    /// Zeayii 运行取消源。
    /// </summary>
    private CancellationTokenSource? _runCancellationTokenSource;

    /// <summary>
    /// Zeayii 是否交互终端。
    /// </summary>
    private readonly bool _interactive = !Console.IsInputRedirected && !Console.IsOutputRedirected;

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            throw new InvalidOperationException("Presentation manager already started.");
        }

        if (!_interactive)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            return;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _runCancellationTokenSource = cts;
        var runToken = cts.Token;

        try
        {
            AnsiConsole.Clear();
            await AnsiConsole.Live(BuildLayoutFrame()).AutoClear(false).StartAsync(async ctx =>
                {
                    while (true)
                    {
                        PollInput();
                        ctx.UpdateTarget(BuildLayoutFrame());

                        if (runToken.IsCancellationRequested || _state.ShouldExit)
                        {
                            break;
                        }

                        try
                        {
                            await Task.Delay(_options.RefreshIntervalMs, runToken).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }

                    // 退出前强制渲染最终状态，避免最后阶段停留在旧帧。
                    ctx.UpdateTarget(BuildLayoutFrame());
                }
            ).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            // Zeayii 在退出后额外输出一空行，避免命令提示符顶起最后一帧。
            AnsiConsole.WriteLine();
        }
    }

    /// <inheritdoc />
    public ValueTask StopAsync()
    {
        _state.ShouldExit = true;
        var cts = Volatile.Read(ref _runCancellationTokenSource);
        if (cts is not null)
        {
            try
            {
                cts.Cancel();
            }
            catch
            {
                // ignore
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public void WriteLog(LogLevel level, string tag, string message, Exception? exception = null)
    {
        _logStore.Write(level, tag, message, exception);
    }

    /// <inheritdoc />
    public void UpdateTask(string taskName, TaskStage stage, TaskStatus status)
    {
        _taskStore.Update(taskName, stage, status);
    }

    /// <inheritdoc />
    public void SetLogLevels(LogLevel consoleLogLevel, LogLevel fileLogLevel)
    {
        _state.ConsoleLogLevel = consoleLogLevel;
        _state.FileLogLevel = fileLogLevel;
    }

    /// <summary>
    /// Zeayii 轮询处理输入。
    /// </summary>
    private void PollInput()
    {
        while (Console.KeyAvailable)
        {
            var key = Console.ReadKey(intercept: true);
            InputDispatcher.HandleKey(_state, key);
        }
    }

    /// <summary>
    /// Zeayii 构建整帧布局。
    /// </summary>
    /// <returns>Zeayii 渲染帧。</returns>
    private IRenderable BuildLayoutFrame()
    {
        var tasks = _taskStore.Snapshot();
        var logs = _logStore.Snapshot();
        var profile = AnsiConsole.Console.Profile;
        var height = Math.Max(12, profile.Height);

        var header = HeaderRenderer.Render(tasks, _state.ConsoleLogLevel, _state.FileLogLevel);
        var footer = InstructionRenderer.Render(_state);
        const int headerHeight = 3;
        const int footerHeight = 3;
        var bodyLines = Math.Max(4, height - headerHeight - footerHeight);

        var root = new Layout("root").SplitRows(
            new Layout("header").Size(headerHeight),
            new Layout("body"),
            new Layout("footer").Size(footerHeight)
        );

        root["header"].Update(header);
        root["footer"].Update(footer);
        root["body"].SplitColumns(
            new Layout("tasks").Ratio(2).MinimumSize(_options.MinTaskPanelWidth),
            new Layout("logs").Ratio(3).MinimumSize(_options.MinLogPanelWidth)
        );

        root["body"]["tasks"].Update(TaskListRenderer.Render(tasks, _state, bodyLines));
        root["body"]["logs"].Update(LogRenderer.Render(logs, _state, bodyLines));
        return root;
    }
}
