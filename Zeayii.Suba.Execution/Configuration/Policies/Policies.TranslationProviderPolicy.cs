namespace Zeayii.Suba.Core.Configuration.Policies;

/// <summary>
/// Zeayii translation provider policy.
/// </summary>
public enum TranslationProviderPolicy : byte
{
    /// <summary>
    /// Use locally deployed Ollama service.
    /// </summary>
    Ollama = 1,

    /// <summary>
    /// Use OpenAI chat completions API.
    /// </summary>
    OpenAi = 2
}
