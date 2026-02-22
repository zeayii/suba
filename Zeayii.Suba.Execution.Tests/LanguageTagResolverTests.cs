using Zeayii.Suba.Core.Services;

namespace Zeayii.Suba.Execution.Tests;

/// <summary>
/// Zeayii 语言标签解析器测试。
/// </summary>
public sealed class LanguageTagResolverTests
{
    /// <summary>
    /// Zeayii 验证 BCP 47 规范化行为。
    /// </summary>
    [Fact]
    public void NormalizeBcp47_ShouldReturnCanonicalName()
    {
        var resolver = new LanguageTagResolver();
        var tag = resolver.NormalizeBcp47("ja-jp");
        Assert.Equal("ja-JP", tag);
    }

    /// <summary>
    /// Zeayii 验证 ISO 639-1 映射行为。
    /// </summary>
    [Fact]
    public void ResolveIso6391Code_ShouldReturnTwoLetterCode()
    {
        var resolver = new LanguageTagResolver();
        var code = resolver.ResolveIso6391Code("zh-CN");
        Assert.Equal("zh", code);
    }
}
