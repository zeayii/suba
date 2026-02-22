namespace Zeayii.Suba.Core.Configuration.Policies;

/// <summary>
/// Zeayii 翻译响应模式策略。
/// </summary>
public enum TranslationResponseMode : byte
{
    /// <summary>
    /// Zeayii 非流式响应模式。
    /// </summary>
    NonStreaming = 1,

    /// <summary>
    /// Zeayii 流式响应模式。
    /// </summary>
    Streaming = 2
}
