using System.Globalization;
using Zeayii.Suba.Core.Configuration.Policies;

namespace Zeayii.Suba.Core.Configuration.Options;

/// <summary>
/// Zeayii 翻译阶段配置。
/// </summary>
public sealed class TranslationOptions
{
    /// <summary>
    /// Zeayii translation provider policy.
    /// </summary>
    public required TranslationProviderPolicy Provider { get; init; }

    /// <summary>
    /// Zeayii 译文语言（BCP 47）。
    /// </summary>
    public required CultureInfo Language { get; init; }

    /// <summary>
    /// Zeayii Ollama 服务地址。
    /// </summary>
    public required string OllamaBaseUrl { get; init; }

    /// <summary>
    /// Zeayii Ollama model name.
    /// </summary>
    public required string OllamaModel { get; init; }

    /// <summary>
    /// Zeayii OpenAI service base URL.
    /// </summary>
    public required string OpenAiBaseUrl { get; init; }

    /// <summary>
    /// Zeayii OpenAI API key.
    /// </summary>
    public required string OpenAiApiKey { get; init; }

    /// <summary>
    /// Zeayii OpenAI model name.
    /// </summary>
    public required string OpenAiModel { get; init; }

    /// <summary>
    /// Zeayii 翻译响应模式策略。
    /// </summary>
    public required TranslationResponseMode ResponseMode { get; init; }

    /// <summary>
    /// Zeayii 翻译上下文窗口大小。
    /// </summary>
    public required int ContextQueueSize { get; init; }

    /// <summary>
    /// Zeayii 翻译上下文断链阈值（毫秒）。
    /// </summary>
    public required int ContextGapMs { get; init; }

    /// <summary>
    /// Zeayii 翻译中间字幕写出间隔（条）；0 表示仅最终写出。
    /// </summary>
    public required int PartialWriteInterval { get; init; }
}
