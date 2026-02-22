namespace Zeayii.Suba.CommandLine.Models;

/// <summary>
/// Zeayii TOML 输入文档模型。
/// </summary>
internal sealed class SubaTomlArgumentsDocument
{
    /// <summary>
    /// Zeayii 输入媒体路径列表。
    /// </summary>
    public required IReadOnlyList<string> Inputs { get; init; }

    /// <summary>
    /// Zeayii 主提示词文本。
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// Zeayii 修复提示词文本。
    /// </summary>
    public required string FixPrompt { get; init; }
}
