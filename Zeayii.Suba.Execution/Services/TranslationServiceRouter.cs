using Zeayii.Suba.Core.Abstractions;
using Zeayii.Suba.Core.Configuration.Options;
using Zeayii.Suba.Core.Configuration.Policies;
using Zeayii.Suba.Core.Orchestration;

namespace Zeayii.Suba.Core.Services;

/// <summary>
/// Zeayii translation service router by provider policy.
/// </summary>
/// <param name="options">Zeayii core options.</param>
/// <param name="ollamaService">Zeayii Ollama translation service.</param>
/// <param name="openAiService">Zeayii OpenAI translation service.</param>
internal sealed class TranslationServiceRouter(
    SubaOptions options,
    OllamaTranslationService ollamaService,
    OpenAiTranslationService openAiService) : ITranslationService
{
    /// <summary>
    /// Zeayii core options.
    /// </summary>
    private readonly SubaOptions _options = options;

    /// <summary>
    /// Zeayii Ollama translation service.
    /// </summary>
    private readonly OllamaTranslationService _ollamaService = ollamaService;

    /// <summary>
    /// Zeayii OpenAI translation service.
    /// </summary>
    private readonly OpenAiTranslationService _openAiService = openAiService;

    /// <summary>
    /// Zeayii translate subtitle segments.
    /// </summary>
    /// <param name="segments">Zeayii subtitle segments.</param>
    /// <param name="prompt">Zeayii prompt.</param>
    /// <param name="fixPrompt">Zeayii fix prompt.</param>
    /// <param name="progressCallback">Zeayii progress callback.</param>
    /// <param name="cancellationToken">Zeayii cancellation token.</param>
    /// <returns>Zeayii async task.</returns>
    public Task TranslateAsync(
        IReadOnlyList<SubtitleSegment> segments,
        string prompt,
        string fixPrompt,
        Func<int, int, CancellationToken, Task>? progressCallback,
        CancellationToken cancellationToken
    )
    {
        return _options.Translation.Provider switch
        {
            TranslationProviderPolicy.Ollama => _ollamaService.TranslateAsync(segments, prompt, fixPrompt, progressCallback, cancellationToken),
            TranslationProviderPolicy.OpenAi => _openAiService.TranslateAsync(segments, prompt, fixPrompt, progressCallback, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported translation provider: {_options.Translation.Provider}")
        };
    }
}
