namespace Zeayii.Suba.Core.Configuration.Policies;

/// <summary>
/// Zeayii subtitle artifact overwrite policy.
/// </summary>
public enum ArtifactOverwritePolicy : byte
{
    /// <summary>
    /// Skip writing when target subtitle already exists.
    /// </summary>
    SkipExisting = 0,

    /// <summary>
    /// Always overwrite existing subtitle artifacts.
    /// </summary>
    Overwrite = 1
}
