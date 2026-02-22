using System.Net.Http.Json;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Zeayii.Suba.Core.Abstractions;
using Zeayii.Suba.Core.Configuration.Options;
using Zeayii.Suba.Core.Configuration.Policies;
using Zeayii.Suba.Core.Orchestration;

namespace Zeayii.Suba.Core.Services;

/// <summary>
/// Zeayii 基于 Ollama API 的字幕翻译服务。
/// </summary>
/// <param name="httpClientFactory">Zeayii HTTP 客户端工厂。</param>
/// <param name="options">Zeayii 核心配置。</param>
/// <param name="logger">Zeayii 日志器。</param>
internal sealed class OllamaTranslationService(IHttpClientFactory httpClientFactory, SubaOptions options, ILogger<OllamaTranslationService> logger) : ITranslationService
{
    /// <summary>
    /// Zeayii 翻译完成日志委托。
    /// </summary>
    private static readonly Action<ILogger, int, Exception?> TranslateDoneLogAction = LoggerMessage.Define<int>(LogLevel.Information, new EventId(1101, nameof(TranslateAsync)), "Translate done. Segments={Count}");

    /// <summary>
    /// Zeayii HTTP 客户端工厂。
    /// </summary>
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    /// <summary>
    /// Zeayii 运行配置。
    /// </summary>
    private readonly SubaOptions _options = options;

    /// <summary>
    /// Zeayii 日志器。
    /// </summary>
    private readonly ILogger<OllamaTranslationService> _logger = logger;

    /// <summary>
    /// Zeayii 对字幕段集合执行翻译。
    /// </summary>
    /// <param name="segments">Zeayii 待翻译字幕段集合。</param>
    /// <param name="prompt">Zeayii 提示词。</param>
    /// <param name="fixPrompt">Zeayii 修复提示词。</param>
    /// <param name="progressCallback">Zeayii 翻译进度回调。</param>
    /// <param name="cancellationToken">Zeayii 取消令牌。</param>
    /// <returns>Zeayii 异步任务。</returns>
    public async Task TranslateAsync(
        IReadOnlyList<SubtitleSegment> segments,
        string prompt,
        string fixPrompt,
        Func<int, int, CancellationToken, Task>? progressCallback,
        CancellationToken cancellationToken
    )
    {
        if (segments.Count == 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new InvalidOperationException("Translation prompt cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(fixPrompt))
        {
            throw new InvalidOperationException("Translation fix prompt cannot be empty.");
        }

        var client = _httpClientFactory.CreateClient("ollama");
        var contextQueue = new Queue<string>();
        int? lastEnd = null;
        _logger.LogInformation("Ollama translate start. Segments={Count}, Model={Model}", segments.Count, _options.Translation.OllamaModel);

        for (var index = 0; index < segments.Count; index++)
        {
            var segment = segments[index];
            var segmentWatch = Stopwatch.StartNew();
            if (lastEnd.HasValue && segment.StartMs - lastEnd.Value > _options.Translation.ContextGapMs)
            {
                contextQueue.Clear();
            }

            while (contextQueue.Count > _options.Translation.ContextQueueSize)
            {
                _ = contextQueue.Dequeue();
            }

            var input = contextQueue.Count > 0 ? $"参考上下文：\n{string.Join("\n", contextQueue)}\n翻译当前句：{segment.OriginalText}" : $"翻译当前句：{segment.OriginalText}";
            _logger.LogTrace("Ollama translate request start. Segment={Current}/{Total}", index + 1, segments.Count);

            var translated = await ChatWithTimeoutAsync(client, prompt, input, index + 1, segments.Count, cancellationToken).ConfigureAwait(false);
            if (translated.Length > 60 || translated.Contains('\n'))
            {
                _logger.LogDebug("Ollama translate fix triggered. Segment={Current}/{Total}", index + 1, segments.Count);
                var refined = await ChatWithTimeoutAsync(client, fixPrompt, translated, index + 1, segments.Count, cancellationToken).ConfigureAwait(false);
                translated = string.IsNullOrWhiteSpace(refined) ? translated : refined;
            }

            var normalized = NormalizeTranslatedText(translated);
            segment.TranslatedText = normalized;
            contextQueue.Enqueue(normalized);
            lastEnd = segment.EndMs;
            _logger.LogTrace("Ollama translate request done. Segment={Current}/{Total}, Elapsed={ElapsedMs}ms", index + 1, segments.Count, segmentWatch.ElapsedMilliseconds);
            if (progressCallback is not null)
            {
                await progressCallback(index + 1, segments.Count, cancellationToken).ConfigureAwait(false);
            }
        }

        TranslateDoneLogAction(_logger, segments.Count, null);
    }

    /// <summary>
    /// Zeayii 调用 Ollama 聊天接口并返回文本。
    /// </summary>
    /// <param name="client">Zeayii HTTP 客户端。</param>
    /// <param name="systemPrompt">Zeayii 系统提示词。</param>
    /// <param name="text">Zeayii 输入文本。</param>
    /// <param name="cancellationToken">Zeayii 取消令牌。</param>
    /// <returns>Zeayii 模型输出文本。</returns>
    private async Task<string> ChatAsync(HttpClient client, string systemPrompt, string text, CancellationToken cancellationToken)
    {
        var streaming = _options.Translation.ResponseMode == TranslationResponseMode.Streaming;
        var payload = new OllamaChatRequest
        {
            Model = _options.Translation.OllamaModel,
            Think = false,
            Stream = streaming,
            KeepAlive = 5,
            Format = null,
            Options = null,
            Messages =
            [
                new OllamaChatMessage { Role = "system", Content = systemPrompt },
                new OllamaChatMessage { Role = "user", Content = text }
            ]
        };

        using var content = JsonContent.Create(payload, SubaCoreJsonSerializerContext.Default.OllamaChatRequest);
        using var response = await client.PostAsync("/api/chat", content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        if (!streaming)
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var document = await JsonSerializer.DeserializeAsync(stream, SubaCoreJsonSerializerContext.Default.OllamaChatResponse, cancellationToken).ConfigureAwait(false);
            return document?.Message?.Content ?? string.Empty;
        }

        var outputBuilder = new StringBuilder();
        await using var outputStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(outputStream, Encoding.UTF8);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var chunk = JsonSerializer.Deserialize(line, SubaCoreJsonSerializerContext.Default.OllamaChatResponse);
            if (!string.IsNullOrEmpty(chunk?.Message?.Content))
            {
                outputBuilder.Append(chunk.Message.Content);
            }

            if (chunk?.Done == true)
            {
                break;
            }
        }

        return outputBuilder.ToString();
    }

    /// <summary>
    /// Zeayii invoke Ollama chat with per-request timeout.
    /// </summary>
    /// <param name="client">Zeayii HTTP client.</param>
    /// <param name="systemPrompt">Zeayii system prompt.</param>
    /// <param name="text">Zeayii user text.</param>
    /// <param name="current">Zeayii current segment index.</param>
    /// <param name="total">Zeayii total segment count.</param>
    /// <param name="cancellationToken">Zeayii cancellation token.</param>
    /// <returns>Zeayii model output text.</returns>
    private async Task<string> ChatWithTimeoutAsync(HttpClient client, string systemPrompt, string text, int current, int total, CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.Runtime.CommandTimeoutSeconds)));
        try
        {
            return await ChatAsync(client, systemPrompt, text, timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeoutSource.IsCancellationRequested)
        {
            throw new TimeoutException($"Ollama translate timeout at segment {current}/{total}, timeout={_options.Runtime.CommandTimeoutSeconds}s.");
        }
    }

    /// <summary>
    /// Zeayii 规范化译文，避免异常长文本污染后续上下文。
    /// </summary>
    /// <param name="value">Zeayii 原始译文。</param>
    /// <returns>Zeayii 规范化译文。</returns>
    private static string NormalizeTranslatedText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
        var firstLine = normalized.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? normalized;
        if (firstLine.Length > 120)
        {
            firstLine = firstLine[..120].TrimEnd();
        }

        return firstLine;
    }
}

/// <summary>
/// Zeayii Ollama Chat 请求模型。
/// </summary>
internal sealed class OllamaChatRequest
{
    /// <summary>
    /// Zeayii 模型名称。
    /// </summary>
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    /// <summary>
    /// Zeayii 推理思考开关。
    /// </summary>
    [JsonPropertyName("think")]
    public bool Think { get; init; }

    /// <summary>
    /// Zeayii 是否启用流式返回。
    /// </summary>
    [JsonPropertyName("stream")]
    public bool Stream { get; init; }

    /// <summary>
    /// Zeayii 会话保持时长。
    /// </summary>
    [JsonPropertyName("keep_alive")]
    public int KeepAlive { get; init; }

    /// <summary>
    /// Zeayii 结构化输出格式。
    /// </summary>
    [JsonPropertyName("format")]
    public object? Format { get; init; }

    /// <summary>
    /// Zeayii 模型附加选项集合。
    /// </summary>
    [JsonPropertyName("options")]
    public Dictionary<string, JsonElement>? Options { get; init; }

    /// <summary>
    /// Zeayii 对话消息集合。
    /// </summary>
    [JsonPropertyName("messages")]
    public required IReadOnlyList<OllamaChatMessage> Messages { get; init; }

}

/// <summary>
/// Zeayii Ollama Chat 消息模型。
/// </summary>
internal sealed class OllamaChatMessage
{
    /// <summary>
    /// Zeayii 消息角色。
    /// </summary>
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    /// <summary>
    /// Zeayii 消息内容。
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }

}

/// <summary>
/// Zeayii Ollama Chat 响应模型。
/// </summary>
internal sealed class OllamaChatResponse
{
    /// <summary>
    /// Zeayii 模型名称。
    /// </summary>
    [JsonPropertyName("model")]
    public string? Model { get; init; }

    /// <summary>
    /// Zeayii 响应创建时间。
    /// </summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; init; }

    /// <summary>
    /// Zeayii 返回消息。
    /// </summary>
    [JsonPropertyName("message")]
    public OllamaChatMessage? Message { get; init; }

    /// <summary>
    /// Zeayii 是否完成。
    /// </summary>
    [JsonPropertyName("done")]
    public bool? Done { get; init; }

    /// <summary>
    /// Zeayii 完成原因。
    /// </summary>
    [JsonPropertyName("done_reason")]
    public string? DoneReason { get; init; }

}
