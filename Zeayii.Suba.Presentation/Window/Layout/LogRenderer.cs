using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Rendering;
using Zeayii.Suba.Presentation.Models;
using Zeayii.Suba.Presentation.Window.State;

namespace Zeayii.Suba.Presentation.Window.Layout;

/// <summary>
/// Zeayii 日志面板渲染器。
/// </summary>
internal static class LogRenderer
{
    /// <summary>
    /// Zeayii 渲染日志面板。
    /// </summary>
    /// <param name="logs">Zeayii 日志快照。</param>
    /// <param name="state">Zeayii 仪表盘状态。</param>
    /// <param name="viewportLines">Zeayii 视口行数。</param>
    /// <returns>Zeayii 渲染对象。</returns>
    public static IRenderable Render(IReadOnlyList<LogEntry> logs, DashboardState state, int viewportLines)
    {
        state.LogRegion.UpdateBounds(logs.Count, Math.Max(1, viewportLines - 3));
        if (state.AutoFollowLog)
        {
            state.LogRegion.StickToBottom();
        }
        else if (state.LogRegion.IsAtBottom)
        {
            state.AutoFollowLog = true;
        }

        var start = state.LogRegion.Offset;
        var count = Math.Min(state.LogRegion.ViewportSize, Math.Max(0, logs.Count - start));

        var table = new Table().Border(TableBorder.None).HideHeaders();
        table.AddColumn(new TableColumn("Log"));
        table.AddColumn(new TableColumn(" ").NoWrap().RightAligned());

        if (count == 0)
        {
            table.AddRow(new Markup("[grey]No logs[/]"), new Markup(string.Empty));
        }
        else
        {
            var barRows = Math.Max(1, count);
            for (var i = 0; i < count; i++)
            {
                var item = logs[start + i];
                var line = $"{item.TimestampText} [{item.Level}] [{item.Tag}] {item.Message}";
                var color = ResolveColor(item.Level);
                var bar = ResolveScrollBarChar(state.LogRegion, i, barRows);
                table.AddRow(
                    new Markup($"[{color}]{Markup.Escape(line)}[/]"),
                    new Markup(Markup.Escape(bar))
                );
            }
        }

        var title = $"Logs ({logs.Count}) {BuildProgressText(state.LogRegion)}";
        return new Panel(table).Header(new PanelHeader(title)).Expand();
    }

    /// <summary>
    /// Zeayii 解析日志颜色。
    /// </summary>
    /// <param name="level">Zeayii 日志等级。</param>
    /// <returns>Zeayii 颜色名。</returns>
    private static string ResolveColor(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => "grey",
            LogLevel.Debug => "steelblue1",
            LogLevel.Information => "green",
            LogLevel.Warning => "yellow",
            LogLevel.Error => "red1",
            LogLevel.Critical => "red",
            _ => "white"
        };
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
