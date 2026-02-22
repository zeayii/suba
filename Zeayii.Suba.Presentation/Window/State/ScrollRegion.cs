namespace Zeayii.Suba.Presentation.Window.State;

/// <summary>
/// Zeayii 可滚动区域状态。
/// </summary>
internal sealed class ScrollRegion
{
    /// <summary>
    /// Zeayii 视口行数。
    /// </summary>
    public int ViewportSize { get; private set; }

    /// <summary>
    /// Zeayii 当前偏移。
    /// </summary>
    public int Offset { get; private set; }

    /// <summary>
    /// Zeayii 区域总行数。
    /// </summary>
    public int TotalSize { get; private set; }

    /// <summary>
    /// Zeayii 是否位于顶部。
    /// </summary>
    public bool IsAtTop => Offset == 0;

    /// <summary>
    /// Zeayii 是否位于底部。
    /// </summary>
    public bool IsAtBottom => Offset >= Math.Max(0, TotalSize - ViewportSize);

    /// <summary>
    /// Zeayii 更新边界。
    /// </summary>
    /// <param name="totalSize">Zeayii 总行数。</param>
    /// <param name="viewportSize">Zeayii 视口行数。</param>
    public void UpdateBounds(int totalSize, int viewportSize)
    {
        TotalSize = Math.Max(0, totalSize);
        ViewportSize = Math.Max(1, viewportSize);
        Offset = Math.Clamp(Offset, 0, Math.Max(0, TotalSize - ViewportSize));
    }

    /// <summary>
    /// Zeayii 按行滚动。
    /// </summary>
    /// <param name="delta">Zeayii 偏移量。</param>
    public void ScrollLine(int delta)
    {
        Offset = Math.Clamp(Offset + delta, 0, Math.Max(0, TotalSize - ViewportSize));
    }

    /// <summary>
    /// Zeayii 按页滚动。
    /// </summary>
    /// <param name="pageDelta">Zeayii 页偏移。</param>
    public void ScrollPage(int pageDelta)
    {
        ScrollLine(pageDelta * ViewportSize);
    }

    /// <summary>
    /// Zeayii 吸附到底部。
    /// </summary>
    public void StickToBottom()
    {
        Offset = Math.Max(0, TotalSize - ViewportSize);
    }

    /// <summary>
    /// Zeayii 吸附到顶部。
    /// </summary>
    public void StickToTop()
    {
        Offset = 0;
    }
}
