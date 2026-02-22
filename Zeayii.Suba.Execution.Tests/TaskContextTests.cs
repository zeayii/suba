using Zeayii.Suba.Core.Contexts;
using Zeayii.Suba.Core.Orchestration;
using Zeayii.Suba.Execution.Tests.TestSupport;

namespace Zeayii.Suba.Execution.Tests;

/// <summary>
/// Zeayii 任务上下文状态流转测试。
/// </summary>
public sealed class TaskContextTests
{
    /// <summary>
    /// Zeayii 验证阶段成功流转记录。
    /// </summary>
    [Fact]
    public void BeginAndCompleteStage_ShouldRecordSucceededStage()
    {
        var context = CreateTaskContext();

        context.BeginStage(TaskStage.Vad);
        context.CompleteStage(TaskStage.Vad);
        context.MarkCompleted();

        Assert.Equal(TaskStage.Completed, context.CurrentStage);
        Assert.Equal(Zeayii.Suba.Core.Orchestration.TaskStatus.Succeeded, context.CurrentStatus);
        var record = Assert.Single(context.StageRecords);
        Assert.Equal(TaskStage.Vad, record.Stage);
        Assert.Equal(Zeayii.Suba.Core.Orchestration.TaskStatus.Succeeded, record.Status);
        Assert.NotNull(record.EndedAtUtc);
    }

    /// <summary>
    /// Zeayii 验证失败阶段落盘记录。
    /// </summary>
    [Fact]
    public void FailStage_ShouldSetFailedStatusAndMessage()
    {
        var context = CreateTaskContext();
        context.BeginStage(TaskStage.Transcribe);

        context.FailStage(TaskStage.Transcribe, new InvalidOperationException("boom"));

        Assert.Equal(TaskStage.Failed, context.CurrentStage);
        Assert.Equal(Zeayii.Suba.Core.Orchestration.TaskStatus.Failed, context.CurrentStatus);
        Assert.Equal("boom", context.FailureMessage);
        var record = Assert.Single(context.StageRecords);
        Assert.Equal(Zeayii.Suba.Core.Orchestration.TaskStatus.Failed, record.Status);
        Assert.Equal("boom", record.ErrorMessage);
    }

    /// <summary>
    /// Zeayii 创建测试任务上下文。
    /// </summary>
    /// <returns>Zeayii 任务上下文。</returns>
    private static TaskContext CreateTaskContext()
    {
        var options = TestSubaOptionsFactory.Create();
        var global = new GlobalContext(options);
        return new TaskContext(global, "input.wav", "p1", "fp1");
    }
}


