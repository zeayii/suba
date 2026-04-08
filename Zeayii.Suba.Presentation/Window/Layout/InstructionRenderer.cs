using Spectre.Console;
using Spectre.Console.Rendering;
using Zeayii.Suba.Presentation.Window.Layout.Support;
using Zeayii.Suba.Presentation.Window.Input;
using Zeayii.Suba.Presentation.Window.State;

namespace Zeayii.Suba.Presentation.Window.Layout;

/// <summary>
/// Zeayii 底部按键说明渲染器。
/// </summary>
internal static class InstructionRenderer
{
    /// <summary>
    /// Zeayii 渲染底部说明。
    /// </summary>
    /// <returns>Zeayii 渲染对象。</returns>
    public static IRenderable Render(DashboardState state)
    {
        var text = state.ExitPending
            ? $"[{PresentationPalette.Failure}]Exit armed:[/] Press Enter to quit"
            : $"[{PresentationPalette.Muted}]Focus:[/] {Markup.Escape(InputBinding.FocusSwitch)}   [{PresentationPalette.Muted}]Scroll:[/] {Markup.Escape(InputBinding.RegionScrollLines)} | {Markup.Escape(InputBinding.RegionScrollPages)}   [{PresentationPalette.Muted}]Tasks:[/] {Markup.Escape(InputBinding.TaskTop)} Top   [{PresentationPalette.Muted}]Logs:[/] {Markup.Escape(InputBinding.LogBottom)} Follow   [{PresentationPalette.Failure}]Exit:[/] {Markup.Escape(InputBinding.Quit)}";
        return new Panel(Align.Center(new Markup(text))).Expand();
    }
}
