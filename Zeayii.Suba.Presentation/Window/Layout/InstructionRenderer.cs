using Spectre.Console;
using Spectre.Console.Rendering;
using Zeayii.Suba.Presentation.Window.Layout.Support;
using Zeayii.Suba.Presentation.Window.Input;

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
    public static IRenderable Render()
    {
        var text = $"[{PresentationPalette.Muted}]Logs:[/] {Markup.Escape(InputBinding.LogScrollLines)} | {Markup.Escape(InputBinding.LogScrollPages)}   [{PresentationPalette.Muted}]Tasks:[/] {Markup.Escape(InputBinding.TaskScrollLines)} | {Markup.Escape(InputBinding.TaskScrollPages)}   [{PresentationPalette.Failure}]Exit:[/] {Markup.Escape(InputBinding.Quit)}";
        return new Panel(Align.Center(new Markup(text))).Expand();
    }
}
