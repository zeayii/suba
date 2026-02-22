namespace Zeayii.Suba.Core.Configuration.Options;

/// <summary>
/// Zeayii SepFormer 分离阶段配置。
/// </summary>
public sealed class SepformerOptions
{
    /// <summary>
    /// Zeayii 是否启用分离结果峰值归一化。
    /// </summary>
    public required bool NormalizeOutput { get; init; }
}
