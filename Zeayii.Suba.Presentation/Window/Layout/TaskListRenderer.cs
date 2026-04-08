using Spectre.Console;
using Spectre.Console.Rendering;
using Zeayii.Suba.Core.Orchestration;
using Zeayii.Suba.Presentation.Models;
using Zeayii.Suba.Presentation.Window.Layout.Support;
using Zeayii.Suba.Presentation.Window.State;

namespace Zeayii.Suba.Presentation.Window.Layout;

/// <summary>
/// Zeayii 任务列表渲染器。
/// </summary>
internal static class TaskListRenderer
{
    /// <summary>
    /// Zeayii 任务名称最大显示字符数。
    /// </summary>
    private const int TaskNameMaxChars = 26;

    /// <summary>
    /// Zeayii 渲染任务面板。
    /// </summary>
    /// <param name="tasks">Zeayii 任务快照。</param>
    /// <param name="state">Zeayii 仪表盘状态。</param>
    /// <param name="viewportLines">Zeayii 视口行数。</param>
    /// <returns>Zeayii 渲染对象。</returns>
    public static IRenderable Render(IReadOnlyList<TaskSnapshot> tasks, DashboardState state, int viewportLines)
    {
        state.TaskRegion.UpdateBounds(tasks.Count, Math.Max(1, viewportLines - 3));
        if (state.AutoFollowTask)
        {
            state.TaskRegion.StickToTop();
        }
        else if (state.TaskRegion.IsAtTop)
        {
            state.AutoFollowTask = true;
        }

        var start = state.TaskRegion.Offset;
        var count = Math.Min(state.TaskRegion.ViewportSize, Math.Max(0, tasks.Count - start));

        var table = new Table().Border(TableBorder.None);
        table.AddColumn(new TableColumn("Task"));
        table.AddColumn(new TableColumn("Stage").NoWrap());
        table.AddColumn(new TableColumn("Status").NoWrap());
        table.AddColumn(new TableColumn("Elapsed").NoWrap().RightAligned());
        table.AddColumn(new TableColumn(" ").NoWrap().RightAligned());

        if (count == 0)
        {
            table.AddRow(new Markup($"[{PresentationPalette.Muted}]No tasks[/]"), new Markup(string.Empty), new Markup(string.Empty), new Markup(string.Empty), new Markup(string.Empty));
        }
        else
        {
            var now = DateTimeOffset.UtcNow;
            var barRows = Math.Max(1, count);
            for (var i = 0; i < count; i++)
            {
                var item = tasks[start + i];
                var color = ResolveColor(item.Stage, item.Status);
                var elapsed = ResolveElapsed(item, now);
                var taskName = TruncateWithEllipsis(item.Name, TaskNameMaxChars);
                var bar = ResolveScrollBarChar(state.TaskRegion, i, barRows);
                table.AddRow(
                    new Markup($"[{color}]{Markup.Escape(taskName)}[/]"),
                    new Markup($"[{color}]{Markup.Escape(item.Stage.ToString())}[/]"),
                    new Markup($"[{color}]{Markup.Escape(item.Status.ToString())}[/]"),
                    new Markup($"[{color}]{Markup.Escape(elapsed)}[/]"),
                    new Markup(Markup.Escape(bar))
                );
            }
        }

        var title = $"Tasks ({tasks.Count}) {BuildProgressText(state.TaskRegion)}";
        return new Panel(table).Header(new PanelHeader(title)).Expand();
    }

    /// <summary>
    /// Zeayii 解析任务颜色。
    /// </summary>
    /// <param name="stage">Zeayii 阶段。</param>
    /// <param name="status">Zeayii 状态。</param>
    /// <returns>Zeayii 颜色名。</returns>
    private static Color ResolveColor(TaskStage stage, Zeayii.Suba.Core.Orchestration.TaskStatus status)
    {
        if (status == Zeayii.Suba.Core.Orchestration.TaskStatus.Failed || stage == TaskStage.Failed)
        {
            return PresentationPalette.Failure;
        }

        if (status == Zeayii.Suba.Core.Orchestration.TaskStatus.Succeeded || stage == TaskStage.Completed)
        {
            return PresentationPalette.Success;
        }

        return stage switch
        {
            TaskStage.Transcribe => PresentationPalette.Accent,
            TaskStage.Translate => Color.Orchid,
            TaskStage.AudioPrepare => PresentationPalette.Warning,
            TaskStage.Vad => PresentationPalette.Warning,
            TaskStage.OverlapResolve => Color.Khaki1,
            TaskStage.SubtitleWrite => Color.SteelBlue1,
            _ => PresentationPalette.Muted
        };
    }

    /// <summary>
    /// Zeayii 计算任务耗时文本。
    /// </summary>
    /// <param name="snapshot">Zeayii 任务快照。</param>
    /// <param name="now">Zeayii 当前 UTC 时间。</param>
    /// <returns>Zeayii 耗时文本。</returns>
    private static string ResolveElapsed(TaskSnapshot snapshot, DateTimeOffset now)
    {
        if (snapshot.StartedAtUtc is null)
        {
            return "--";
        }

        var end = snapshot.CompletedAtUtc ?? now;
        var elapsed = end - snapshot.StartedAtUtc.Value;
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        return elapsed.ToString(@"hh\:mm\:ss");
    }

    /// <summary>
    /// Zeayii 截断任务名称并使用单字符省略号。
    /// </summary>
    /// <param name="value">Zeayii 原始任务名称。</param>
    /// <param name="maxChars">Zeayii 最大字符数。</param>
    /// <returns>Zeayii 截断后的任务名称。</returns>
    private static string TruncateWithEllipsis(string value, int maxChars)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxChars)
        {
            return value;
        }

        if (maxChars <= 1)
        {
            return "…";
        }

        return $"{value[..(maxChars - 1)]}…";
    }

    /// <summary>
    /// Zeayii 生成滚动条字符。
    /// </summary>
    /// <param name="region">Zeayii 滚动区域。</param>
    /// <param name="rowIndex">Zeayii 行索引。</param>
    /// <param name="rowCount">Zeayii 行总数。</param>
    /// <returns>Zeayii 滚动条字符。</returns>
    private static string ResolveScrollBarChar(ScrollRegion region, int rowIndex, int rowCount)
    {
        if (region.TotalSize <= region.ViewportSize || rowCount <= 1)
        {
            return " ";
        }

        var maxOffset = Math.Max(1, region.TotalSize - region.ViewportSize);
        var thumb = (int)Math.Round(region.Offset * (rowCount - 1d) / maxOffset, MidpointRounding.AwayFromZero);
        return thumb == rowIndex ? "█" : "│";
    }

    /// <summary>
    /// Zeayii 构建滚动进度文本。
    /// </summary>
    /// <param name="region">Zeayii 滚动区域。</param>
    /// <returns>Zeayii 进度文本。</returns>
    private static string BuildProgressText(ScrollRegion region)
    {
        var total = region.TotalSize;
        if (total <= 0)
        {
            return "0/0 (0%)";
        }

        var start = region.Offset + 1;
        var end = Math.Min(total, region.Offset + region.ViewportSize);
        var ratio = total <= region.ViewportSize ? 1d : Math.Clamp((double)region.Offset / Math.Max(1, total - region.ViewportSize), 0d, 1d);
        var percent = (int)Math.Round(ratio * 100d, MidpointRounding.AwayFromZero);
        return $"{start}-{end}/{total} ({percent}%)";
    }
}
