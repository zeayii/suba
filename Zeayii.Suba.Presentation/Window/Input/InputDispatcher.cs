using Zeayii.Suba.Presentation.Window.State;

namespace Zeayii.Suba.Presentation.Window.Input;

/// <summary>
/// Zeayii 输入分发器。
/// </summary>
internal static class InputDispatcher
{
    /// <summary>
    /// Zeayii 处理输入按键。
    /// </summary>
    /// <param name="state">Zeayii 仪表盘状态。</param>
    /// <param name="keyInfo">Zeayii 输入按键信息。</param>
    public static void HandleKey(DashboardState state, ConsoleKeyInfo keyInfo)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (keyInfo.Key == ConsoleKey.Enter && state.ExitPending)
        {
            state.ShouldExit = true;
            return;
        }

        if (keyInfo.Key == ConsoleKey.Q && keyInfo.Modifiers == 0)
        {
            state.ExitPending = true;
            return;
        }

        state.ExitPending = false;

        if (keyInfo.Key == ConsoleKey.Tab && keyInfo.Modifiers == 0)
        {
            state.ActiveRegion = state.ActiveRegion == DashboardState.FocusRegion.Tasks
                ? DashboardState.FocusRegion.Logs
                : DashboardState.FocusRegion.Tasks;
            return;
        }

        if (keyInfo.Modifiers != 0)
        {
            return;
        }

        if (state.ActiveRegion == DashboardState.FocusRegion.Tasks)
        {
            HandleTaskKey(state, keyInfo.Key);
            return;
        }

        HandleLogKey(state, keyInfo.Key);
    }

    /// <summary>
    /// Zeayii 处理任务区域按键。
    /// </summary>
    /// <param name="state">Zeayii 仪表盘状态。</param>
    /// <param name="key">Zeayii 按键。</param>
    private static void HandleTaskKey(DashboardState state, ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.UpArrow:
            {
                state.AutoFollowTask = false;
                state.TaskRegion.ScrollLine(-1);
                break;
            }
            case ConsoleKey.DownArrow:
            {
                state.AutoFollowTask = false;
                state.TaskRegion.ScrollLine(1);
                break;
            }
            case ConsoleKey.PageUp:
            {
                state.AutoFollowTask = false;
                state.TaskRegion.ScrollPage(-1);
                break;
            }
            case ConsoleKey.PageDown:
            {
                state.AutoFollowTask = false;
                state.TaskRegion.ScrollPage(1);
                break;
            }
            case ConsoleKey.Home:
            {
                state.TaskRegion.StickToTop();
                state.AutoFollowTask = true;
                break;
            }
        }
    }

    /// <summary>
    /// Zeayii 处理日志区域按键。
    /// </summary>
    /// <param name="state">Zeayii 仪表盘状态。</param>
    /// <param name="key">Zeayii 按键。</param>
    private static void HandleLogKey(DashboardState state, ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.UpArrow:
            {
                state.AutoFollowLog = false;
                state.LogRegion.ScrollLine(-1);
                break;
            }
            case ConsoleKey.DownArrow:
            {
                state.AutoFollowLog = false;
                state.LogRegion.ScrollLine(1);
                break;
            }
            case ConsoleKey.PageUp:
            {
                state.AutoFollowLog = false;
                state.LogRegion.ScrollPage(-1);
                break;
            }
            case ConsoleKey.PageDown:
            {
                state.AutoFollowLog = false;
                state.LogRegion.ScrollPage(1);
                break;
            }
            case ConsoleKey.End:
            {
                state.LogRegion.StickToBottom();
                state.AutoFollowLog = true;
                break;
            }
        }
    }
}
