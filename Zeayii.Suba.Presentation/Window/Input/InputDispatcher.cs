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

        if ((keyInfo.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control && keyInfo.Key == ConsoleKey.X)
        {
            state.ShouldExit = true;
            return;
        }

        if ((keyInfo.Modifiers & ConsoleModifiers.Alt) == ConsoleModifiers.Alt)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.UpArrow:
                {
                    state.AutoFollowTask = false;
                    state.TaskRegion.ScrollLine(-1);
                    return;
                }
                case ConsoleKey.DownArrow:
                {
                    state.AutoFollowTask = false;
                    state.TaskRegion.ScrollLine(1);
                    return;
                }
                case ConsoleKey.PageUp:
                {
                    state.AutoFollowTask = false;
                    state.TaskRegion.ScrollPage(-1);
                    return;
                }
                case ConsoleKey.PageDown:
                {
                    state.AutoFollowTask = false;
                    state.TaskRegion.ScrollPage(1);
                    return;
                }
                case ConsoleKey.Home:
                {
                    state.TaskRegion.StickToTop();
                    state.AutoFollowTask = true;
                    return;
                }
            }
        }

        switch (keyInfo.Key)
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
