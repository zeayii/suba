using System.Net.Http.Headers;
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
/// Zeayii translation service based on OpenAI chat completions API.
/// </summary>
/// <param name="httpClientFactory">Zeayii HTTP client factory.</param>
/// <param name="options">Zeayii core options.</param>
/// <param name="logger">Zeayii logger.</param>
internal sealed class OpenAiTranslationService(IHttpClientFactory httpClientFactory, SubaOptions options, ILogger<OpenAiTranslationService> logger) : ITranslationService
{
    /// <summary>
    /// Zeayii completion log action.
    /// </summary>
    private static readonly Action<ILogger, int, Exception?> TranslateDoneLogAction = LoggerMessage.Define<int>(LogLevel.Information, new EventId(1151, nameof(TranslateAsync)), "OpenAI translate done. Segments={Count}");

    /// <summary>
    /// Zeayii HTTP client factory.
    /// </summary>
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    /// <summary>
    /// Zeayii core options.
    /// </summary>
    private readonly SubaOptions _options = options;

    /// <summary>
    /// Zeayii logger.
    /// </summary>
    private readonly ILogger<OpenAiTranslationService> _logger = logger;

    /// <summary>
    /// Zeayii translate subtitle segments.
    /// </summary>
    /// <param name="segments">Zeayii subtitle segments.</param>
    /// <param name="prompt">Zeayii prompt.</param>
    /// <param name="fixPrompt">Zeayii fix prompt.</param>
    /// <param name="progressCallback">Zeayii translation progress callback.</param>
    /// <param name="cancellationToken">Zeayii cancellation token.</param>
    /// <returns>Zeayii async task.</returns>
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

        var client = _httpClientFactory.CreateClient("openai");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.Translation.OpenAiApiKey);

        var contextQueue = new Queue<string>();
        int? lastEnd = null;
        _logger.LogInformation("OpenAI translate start. Segments={Count}, Model={Model}", segments.Count, _options.Translation.OpenAiModel);

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

            var input = contextQueue.Count > 0 ? $"Reference context:\n{string.Join("\n", contextQueue)}\nTranslate current sentence: {segment.OriginalText}" : $"Translate current sentence: {segment.OriginalText}";
            _logger.LogTrace("OpenAI translate request start. Segment={Current}/{Total}", index + 1, segments.Count);
            var translated = await ChatWithTimeoutAsync(client, prompt, input, index + 1, segments.Count, cancellationToken).ConfigureAwait(false);
            if (translated.Length > 120 || translated.Contains('\n'))
            {
                var refined = await ChatWithTimeoutAsync(client, fixPrompt, translated, index + 1, segments.Count, cancellationToken).ConfigureAwait(false);
                translated = string.IsNullOrWhiteSpace(refined) ? translated : refined;
            }

            segment.TranslatedText = translated.Trim();
            contextQueue.Enqueue(segment.TranslatedText);
            lastEnd = segment.EndMs;
            _logger.LogTrace("OpenAI translate request done. Segment={Current}/{Total}, Elapsed={ElapsedMs}ms", index + 1, segments.Count, segmentWatch.ElapsedMilliseconds);
            if (progressCallback is not null)
            {
                await progressCallback(index + 1, segments.Count, cancellationToken).ConfigureAwait(false);
            }
        }

        TranslateDoneLogAction(_logger, segments.Count, null);
    }

    /// <summary>
    /// Zeayii call OpenAI chat completions endpoint.
    /// </summary>
    /// <param name="client">Zeayii HTTP client.</param>
    /// <param name="systemPrompt">Zeayii system prompt.</param>
    /// <param name="text">Zeayii user text.</param>
    /// <param name="cancellationToken">Zeayii cancellation token.</param>
    /// <returns>Zeayii model output text.</returns>
    private async Task<string> ChatAsync(HttpClient client, string systemPrompt, string text, CancellationToken cancellationToken)
    {
        var streaming = _options.Translation.ResponseMode == TranslationResponseMode.Streaming;
        var request = new OpenAiChatCompletionsRequest
        {
            Model = _options.Translation.OpenAiModel,
            Temperature = 0,
            Stream = streaming,
            Messages =
            [
                new OpenAiChatMessage { Role = "system", Content = systemPrompt },
                new OpenAiChatMessage { Role = "user", Content = text }
            ]
        };

        using var content = JsonContent.Create(request, SubaCoreJsonSerializerContext.Default.OpenAiChatCompletionsRequest);
        using var response = await client.PostAsync("/v1/chat/completions", content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        if (!streaming)
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var payload = await JsonSerializer.DeserializeAsync(stream, SubaCoreJsonSerializerContext.Default.OpenAiChatCompletionsResponse, cancellationToken).ConfigureAwait(false);
            return payload?.Choices.FirstOrDefault()?.Message?.Content ?? string.Empty;
        }

        var builder = new StringBuilder();
        await using var outputStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(outputStream, Encoding.UTF8);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                break;
            }

            if (!line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            var data = line[5..].Trim();
            if (data == "[DONE]")
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(data))
            {
                continue;
            }

            var chunk = JsonSerializer.Deserialize(data, SubaCoreJsonSerializerContext.Default.OpenAiChatCompletionsChunkResponse);
            var delta = chunk?.Choices.FirstOrDefault()?.Delta?.Content;
            if (!string.IsNullOrEmpty(delta))
            {
                builder.Append(delta);
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Zeayii invoke OpenAI chat with per-request timeout.
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
            throw new TimeoutException($"OpenAI translate timeout at segment {current}/{total}, timeout={_options.Runtime.CommandTimeoutSeconds}s.");
        }
    }
}

/// <summary>
/// Zeayii OpenAI chat completions request model.
/// </summary>
internal sealed class OpenAiChatCompletionsRequest
{
    /// <summary>
    /// Zeayii model name.
    /// </summary>
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    /// <summary>
    /// Zeayii messages.
    /// </summary>
    [JsonPropertyName("messages")]
    public required IReadOnlyList<OpenAiChatMessage> Messages { get; init; }

    /// <summary>
    /// Zeayii sampling temperature.
    /// </summary>
    [JsonPropertyName("temperature")]
    public float Temperature { get; init; }

    /// <summary>
    /// Zeayii streaming switch.
    /// </summary>
    [JsonPropertyName("stream")]
    public bool Stream { get; init; }
}

/// <summary>
/// Zeayii OpenAI chat message model.
/// </summary>
internal sealed class OpenAiChatMessage
{
    /// <summary>
    /// Zeayii message role.
    /// </summary>
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    /// <summary>
    /// Zeayii message content.
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }
}

/// <summary>
/// Zeayii OpenAI chat completions non-streaming response.
/// </summary>
internal sealed class OpenAiChatCompletionsResponse
{
    /// <summary>
    /// Zeayii response choices.
    /// </summary>
    [JsonPropertyName("choices")]
    public IReadOnlyList<OpenAiChoice> Choices { get; init; } = [];
}

/// <summary>
/// Zeayii OpenAI choice model.
/// </summary>
internal sealed class OpenAiChoice
{
    /// <summary>
    /// Zeayii assistant message.
    /// </summary>
    [JsonPropertyName("message")]
    public OpenAiChatMessage? Message { get; init; }
}

/// <summary>
/// Zeayii OpenAI chat completions streaming chunk response.
/// </summary>
internal sealed class OpenAiChatCompletionsChunkResponse
{
    /// <summary>
    /// Zeayii response choices.
    /// </summary>
    [JsonPropertyName("choices")]
    public IReadOnlyList<OpenAiChunkChoice> Choices { get; init; } = [];
}

/// <summary>
/// Zeayii OpenAI stream chunk choice.
/// </summary>
internal sealed class OpenAiChunkChoice
{
    /// <summary>
    /// Zeayii streaming delta.
    /// </summary>
    [JsonPropertyName("delta")]
    public OpenAiChunkDelta? Delta { get; init; }
}

/// <summary>
/// Zeayii OpenAI stream delta model.
/// </summary>
internal sealed class OpenAiChunkDelta
{
    /// <summary>
    /// Zeayii delta content.
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; init; }
}
