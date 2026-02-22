namespace Zeayii.Suba.Core.Orchestration;

/// <summary>
/// Zeayii 音频语义段模型。
/// </summary>
public sealed class AudioSegment
{
    /// <summary>
    /// Zeayii 语义段序号。
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// Zeayii 起始采样点。
    /// </summary>
    public required int StartSample { get; init; }

    /// <summary>
    /// Zeayii 结束采样点。
    /// </summary>
    public required int EndSample { get; init; }

    /// <summary>
    /// Zeayii 段内音频数据。
    /// </summary>
    public required float[] Audio { get; init; }

    /// <summary>
    /// Zeayii 段内音频起始偏移。
    /// </summary>
    public int AudioOffset { get; init; }

    /// <summary>
    /// Zeayii 段内音频有效长度，默认表示使用到数组末尾。
    /// </summary>
    public int AudioLength { get; init; } = -1;

    /// <summary>
    /// Zeayii 说话人编号，未知时为 -1。
    /// </summary>
    public int Speaker { get; init; } = -1;

    /// <summary>
    /// Zeayii 是否由重叠语音拆分得到。
    /// </summary>
    public bool IsSpeechOverlapped { get; init; }

    /// <summary>
    /// Zeayii 获取段内音频只读切片。
    /// </summary>
    /// <returns>Zeayii 段内音频只读切片。</returns>
    public ReadOnlySpan<float> GetAudioSpan()
    {
        var length = AudioLength < 0 ? Audio.Length - AudioOffset : AudioLength;
        return Audio.AsSpan(AudioOffset, length);
    }
}
