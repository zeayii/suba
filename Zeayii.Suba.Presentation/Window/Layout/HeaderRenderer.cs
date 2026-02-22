using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Rendering;
using Zeayii.Suba.Presentation.Models;

namespace Zeayii.Suba.Presentation.Window.Layout;

/// <summary>
/// Zeayii 顶部标题渲染器。
/// </summary>
internal static class HeaderRenderer
{
    /// <summary>
    /// Zeayii 渲染标题行。
    /// </summary>
    /// <param name="tasks">Zeayii 任务快照。</param>
    /// <param name="consoleLevel">Zeayii 控制台日志等级。</param>
    /// <param name="fileLevel">Zeayii 文件日志等级。</param>
    /// <returns>Zeayii 渲染对象。</returns>
    public static IRenderable Render(IReadOnlyList<TaskSnapshot> tasks, LogLevel consoleLevel, LogLevel fileLevel)
    {
        var total = tasks.Count;
        var pending = tasks.Count(static x => x.Status == Zeayii.Suba.Core.Orchestration.TaskStatus.Pending);
        var running = tasks.Count(static x => x.Status == Zeayii.Suba.Core.Orchestration.TaskStatus.Running);
        var succeeded = tasks.Count(static x => x.Status == Zeayii.Suba.Core.Orchestration.TaskStatus.Succeeded);
        var failed = tasks.Count(static x => x.Status == Zeayii.Suba.Core.Orchestration.TaskStatus.Failed);

        var left = new Markup("[bold]Zeayii Suba[/]");
        var middleText = $"T:{total,5} P:{pending,5} R:{running,5} S:{succeeded,5} F:{failed,5}";
        var middle = new Markup($"[grey]{Markup.Escape(middleText)}[/]");
        var rightText = $"Console:{consoleLevel} File:{fileLevel}";
        var right = new Markup($"[deepskyblue1]{Markup.Escape(rightText)}[/]");

        var grid = new Grid();
        grid.AddColumn(new GridColumn().NoWrap());
        grid.AddColumn(new GridColumn());
        grid.AddColumn(new GridColumn().NoWrap());
        grid.AddRow(left, Align.Center(middle), Align.Right(right));
        return new Panel(grid).Border(BoxBorder.Square).Expand();
    }
}
