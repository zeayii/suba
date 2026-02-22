using System.Text.Json.Serialization;
using Zeayii.Suba.Core.Services;

namespace Zeayii.Suba.Core.Configuration.Options;

/// <summary>
/// Zeayii 批处理输入参数模型。
/// </summary>
public sealed class SubaArguments
{
    /// <summary>
    /// Zeayii 输入媒体文件路径列表。
    /// </summary>
    [JsonPropertyName("inputs")]
    public List<string> Inputs { get; init; } = [];

    /// <summary>
    /// Zeayii 翻译提示词。
    /// </summary>
    [JsonPropertyName("prompt")]
    public string Prompt { get; init; } = string.Empty;

    /// <summary>
    /// Zeayii 翻译修复提示词。
    /// </summary>
    [JsonPropertyName("fix_prompt")]
    public string FixPrompt { get; init; } = string.Empty;
}

/// <summary>
/// Zeayii Core JSON 源生成上下文。
/// </summary>
[JsonSerializable(typeof(OllamaChatRequest))]
[JsonSerializable(typeof(OllamaChatResponse))]
[JsonSerializable(typeof(OpenAiChatCompletionsRequest))]
[JsonSerializable(typeof(OpenAiChatCompletionsResponse))]
[JsonSerializable(typeof(OpenAiChatCompletionsChunkResponse))]
internal sealed partial class SubaCoreJsonSerializerContext : JsonSerializerContext;
