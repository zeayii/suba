using System.Globalization;

namespace Zeayii.Suba.Core.Services;

/// <summary>
/// Zeayii 语言标签解析器，负责 BCP 47 规范化与模型语言码映射。
/// </summary>
public sealed class LanguageTagResolver
{
    /// <summary>
    /// Zeayii 将 BCP 47 语言标签规范化为标准名称。
    /// </summary>
    /// <param name="languageTag">Zeayii 输入语言标签。</param>
    /// <returns>Zeayii 规范化后的语言标签。</returns>
    public string NormalizeBcp47(string languageTag)
    {
        if (string.IsNullOrWhiteSpace(languageTag))
        {
            throw new ArgumentException("Language tag is required.", nameof(languageTag));
        }

        var culture = CultureInfo.GetCultureInfo(languageTag);
        return culture.Name;
    }

    /// <summary>
    /// Zeayii 将 BCP 47 语言标签映射为 ISO 639-1 语言短码。
    /// </summary>
    /// <param name="languageTag">Zeayii 输入语言标签。</param>
    /// <returns>Zeayii ISO 639-1 语言短码。</returns>
    public string ResolveIso6391Code(string languageTag)
    {
        var culture = CultureInfo.GetCultureInfo(languageTag);
        var code = culture.TwoLetterISOLanguageName.ToLowerInvariant();
        return string.Equals(code, "iv", StringComparison.Ordinal) ? throw new CultureNotFoundException(nameof(languageTag), languageTag, "Cannot map invariant culture to ISO 639-1 code.") : code;
    }
}
