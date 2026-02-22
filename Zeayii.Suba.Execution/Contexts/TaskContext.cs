using Zeayii.Suba.Core.Orchestration;
using TaskStatus = Zeayii.Suba.Core.Orchestration.TaskStatus;

namespace Zeayii.Suba.Core.Contexts;

/// <summary>
/// Zeayii 单任务执行上下文。
/// </summary>
/// <param name="global">Zeayii 全局运行上下文。</param>
/// <param name="inputPath">Zeayii 输入媒体路径。</param>
/// <param name="prompt">Zeayii 提示词。</param>
/// <param name="fixPrompt">Zeayii 修复提示词。</param>
public sealed class TaskContext(GlobalContext global, string inputPath, string prompt, string fixPrompt)
{
    /// <summary>
    /// Zeayii 任务启动时间（UTC）。
    /// </summary>
    private readonly DateTimeOffset _taskStartedAtUtc = DateTimeOffset.UtcNow;

    /// <summary>
    /// Zeayii 全局运行上下文。
    /// </summary>
    public GlobalContext Global { get; } = global ?? throw new ArgumentNullException(nameof(global));

    /// <summary>
    /// Zeayii 输入媒体路径。
    /// </summary>
    public string InputPath { get; } = string.IsNullOrWhiteSpace(inputPath) ? throw new ArgumentException("Input path is required.", nameof(inputPath)) : inputPath;

    /// <summary>
    /// Zeayii 提示词。
    /// </summary>
    public string Prompt { get; } = prompt ?? throw new ArgumentNullException(nameof(prompt));

    /// <summary>
    /// Zeayii 修复提示词。
    /// </summary>
    public string FixPrompt { get; } = fixPrompt ?? throw new ArgumentNullException(nameof(fixPrompt));

    /// <summary>
    /// Zeayii 是否已加载源语言字幕产物。
    /// </summary>
    public bool HasPreloadedSourceSubtitle { get; set; }

    /// <summary>
    /// Zeayii 任务标识。
    /// </summary>
    public string TaskId { get; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Zeayii 任务显示名称（输入文件名，不含扩展名）。
    /// </summary>
    public string TaskName { get; } = Path.GetFileNameWithoutExtension(inputPath);

    /// <summary>
    /// Zeayii 当前任务阶段。
    /// </summary>
    public TaskStage CurrentStage { get; private set; } = TaskStage.None;

    /// <summary>
    /// Zeayii 当前任务状态。
    /// </summary>
    public TaskStatus CurrentStatus { get; private set; } = TaskStatus.Pending;

    /// <summary>
    /// Zeayii 任务失败异常。
    /// </summary>
    public Exception? FailureException { get; private set; }

    /// <summary>
    /// Zeayii 任务失败消息。
    /// </summary>
    public string? FailureMessage { get; private set; }

    /// <summary>
    /// Zeayii 阶段执行记录集合。
    /// </summary>
    public List<TaskStageRecord> StageRecords { get; } = [];

    /// <summary>
    /// Zeayii 输入音频是否旁路提取。
    /// </summary>
    public bool IsAudioExtractionBypassed { get; set; }

    /// <summary>
    /// Zeayii 准备后的 WAV 文件路径。
    /// </summary>
    public string PreparedWavPath { get; set; } = string.Empty;

    /// <summary>
    /// Zeayii 当前任务采样率。
    /// </summary>
    public int SampleRate { get; set; }

    /// <summary>
    /// Zeayii 语音段集合。
    /// </summary>
    public List<AudioSegment> AudioSegments { get; } = [];

    /// <summary>
    /// Zeayii 字幕段集合。
    /// </summary>
    public List<SubtitleSegment> SubtitleSegments { get; } = [];

    /// <summary>
    /// Zeayii 标记阶段开始。
    /// </summary>
    /// <param name="stage">Zeayii 阶段。</param>
    public void BeginStage(TaskStage stage)
    {
        CurrentStage = stage;
        CurrentStatus = TaskStatus.Running;
        StageRecords.Add(new TaskStageRecord
        {
            Stage = stage,
            Status = TaskStatus.Running,
            StartedAtUtc = DateTimeOffset.UtcNow
        });
        Global.Presentation.UpdateTask(TaskName, stage, CurrentStatus);
        Global.File.Debug("Stage", $"Task={TaskName} Stage={stage} Started");
    }

    /// <summary>
    /// Zeayii 标记阶段完成。
    /// </summary>
    /// <param name="stage">Zeayii 阶段。</param>
    public void CompleteStage(TaskStage stage)
    {
        CurrentStage = stage;
        var record = StageRecords.LastOrDefault(x => x.Stage == stage && x.Status == TaskStatus.Running);
        if (record is null)
        {
            return;
        }

        record.Status = TaskStatus.Succeeded;
        record.EndedAtUtc = DateTimeOffset.UtcNow;
        Global.Presentation.UpdateTask(TaskName, stage, TaskStatus.Succeeded);
        var elapsed = (record.EndedAtUtc ?? record.StartedAtUtc) - record.StartedAtUtc;
        var elapsedText = FormatElapsed(elapsed);
        Global.File.Info("Stage", $"Task={TaskName} Stage={stage} Succeeded Elapsed={elapsedText}");
        Global.Console.Debug("Stage", $"Task={TaskName} Stage={stage} Succeeded Elapsed={elapsedText}");
    }

    /// <summary>
    /// Zeayii 标记任务失败。
    /// </summary>
    /// <param name="stage">Zeayii 失败阶段。</param>
    /// <param name="exception">Zeayii 异常对象。</param>
    public void FailStage(TaskStage stage, Exception exception)
    {
        CurrentStage = TaskStage.Failed;
        CurrentStatus = TaskStatus.Failed;
        FailureException = exception;
        FailureMessage = exception.Message;

        var record = StageRecords.LastOrDefault(x => x.Stage == stage && x.Status == TaskStatus.Running);
        if (record is not null)
        {
            record.Status = TaskStatus.Failed;
            record.ErrorMessage = exception.Message;
            record.EndedAtUtc = DateTimeOffset.UtcNow;
            Global.Presentation.UpdateTask(TaskName, TaskStage.Failed, TaskStatus.Failed);
            var elapsed = (record.EndedAtUtc ?? record.StartedAtUtc) - record.StartedAtUtc;
            var elapsedText = FormatElapsed(elapsed);
            Global.File.Error("Stage", $"Task={TaskName} Stage={stage} Failed Elapsed={elapsedText}", exception);
            Global.Console.Warn("Stage", $"Task={TaskName} Stage={stage} Failed Elapsed={elapsedText}");
            return;
        }

        StageRecords.Add(new TaskStageRecord
        {
            Stage = stage,
            Status = TaskStatus.Failed,
            StartedAtUtc = DateTimeOffset.UtcNow,
            EndedAtUtc = DateTimeOffset.UtcNow,
            ErrorMessage = exception.Message
        });
        Global.Presentation.UpdateTask(TaskName, TaskStage.Failed, TaskStatus.Failed);
        Global.File.Error("Stage", $"Task={TaskName} Stage={stage} Failed Elapsed={FormatElapsed(TimeSpan.Zero)}", exception);
        Global.Console.Warn("Stage", $"Task={TaskName} Stage={stage} Failed Elapsed={FormatElapsed(TimeSpan.Zero)}");
    }

    /// <summary>
    /// Zeayii 标记任务整体成功完成。
    /// </summary>
    public void MarkCompleted()
    {
        CurrentStage = TaskStage.Completed;
        CurrentStatus = TaskStatus.Succeeded;
        Global.Presentation.UpdateTask(TaskName, TaskStage.Completed, TaskStatus.Succeeded);
        var totalElapsed = DateTimeOffset.UtcNow - _taskStartedAtUtc;
        var elapsedText = FormatElapsed(totalElapsed);
        Global.File.Info("Task", $"Task={TaskName} Completed TotalElapsed={elapsedText}");
        Global.Log.Info("Task", $"Task={TaskName} Completed TotalElapsed={elapsedText}");
    }

    /// <summary>
    /// Zeayii 格式化耗时文本（hh:mm:ss.fff）。
    /// </summary>
    /// <param name="elapsed">Zeayii 耗时。</param>
    /// <returns>Zeayii 格式化字符串。</returns>
    private static string FormatElapsed(TimeSpan elapsed) => elapsed.ToString(@"hh\:mm\:ss\.fff");
}
