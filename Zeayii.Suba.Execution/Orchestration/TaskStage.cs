namespace Zeayii.Suba.Core.Orchestration;

/// <summary>
/// Zeayii 任务执行阶段。
/// </summary>
public enum TaskStage
{
    /// <summary>
    /// Zeayii 未开始阶段。
    /// </summary>
    None = 0,

    /// <summary>
    /// Zeayii 音频准备阶段。
    /// </summary>
    AudioPrepare = 1,

    /// <summary>
    /// Zeayii 语音活动检测阶段。
    /// </summary>
    Vad = 2,

    /// <summary>
    /// Zeayii 重叠段解析阶段。
    /// </summary>
    OverlapResolve = 3,

    /// <summary>
    /// Zeayii 转写阶段。
    /// </summary>
    Transcribe = 4,

    /// <summary>
    /// Zeayii 翻译阶段。
    /// </summary>
    Translate = 5,

    /// <summary>
    /// Zeayii 字幕写出阶段。
    /// </summary>
    SubtitleWrite = 6,

    /// <summary>
    /// Zeayii 任务完成阶段。
    /// </summary>
    Completed = 7,

    /// <summary>
    /// Zeayii 任务失败阶段。
    /// </summary>
    Failed = 8
}
