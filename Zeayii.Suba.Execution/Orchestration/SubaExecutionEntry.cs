using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using Zeayii.Suba.Core.Abstractions;
using Zeayii.Suba.Core.Configuration.Options;
using Zeayii.Suba.Core.Configuration.Policies;
using Zeayii.Suba.Core.Contexts;
using Zeayii.Suba.Core.Services;

namespace Zeayii.Suba.Core.Orchestration;

/// <summary>
/// Zeayii Suba 三阶段处理流水线。
/// </summary>
/// <param name="options">Zeayii 核心配置。</param>
/// <param name="globalContext">Zeayii 全局运行上下文。</param>
/// <param name="audioExtractor">Zeayii 音频提取服务。</param>
/// <param name="vadService">Zeayii VAD 服务。</param>
/// <param name="overlapDetector">Zeayii 重叠检测服务。</param>
/// <param name="audioSeparator">Zeayii 人声分离服务。</param>
/// <param name="transcriptionService">Zeayii 转录服务。</param>
/// <param name="translationService">Zeayii 翻译服务。</param>
/// <param name="subtitleWriter">Zeayii 字幕写出服务。</param>
/// <param name="subtitleArtifactResolver">Zeayii 字幕产物解析器。</param>
internal sealed class SubaPipeline(
    SubaOptions options,
    GlobalContext globalContext,
    IAudioExtractor audioExtractor,
    IVadService vadService,
    IOverlapDetector overlapDetector,
    IAudioSeparator audioSeparator,
    ITranscriptionService transcriptionService,
    ITranslationService translationService,
    ISubtitleWriter subtitleWriter,
    SubtitleArtifactResolver subtitleArtifactResolver
) : ISubaPipeline
{
    /// <summary>
    /// Zeayii 核心配置。
    /// </summary>
    private readonly SubaOptions _options = options;

    /// <summary>
    /// Zeayii 全局运行上下文。
    /// </summary>
    private readonly GlobalContext _globalContext = globalContext;

    /// <summary>
    /// Zeayii 音频提取服务。
    /// </summary>
    private readonly IAudioExtractor _audioExtractor = audioExtractor;

    /// <summary>
    /// Zeayii VAD 服务。
    /// </summary>
    private readonly IVadService _vadService = vadService;

    /// <summary>
    /// Zeayii 重叠检测服务。
    /// </summary>
    private readonly IOverlapDetector _overlapDetector = overlapDetector;

    /// <summary>
    /// Zeayii 分离服务。
    /// </summary>
    private readonly IAudioSeparator _audioSeparator = audioSeparator;

    /// <summary>
    /// Zeayii 转写服务。
    /// </summary>
    private readonly ITranscriptionService _transcriptionService = transcriptionService;

    /// <summary>
    /// Zeayii 翻译服务。
    /// </summary>
    private readonly ITranslationService _translationService = translationService;

    /// <summary>
    /// Zeayii 字幕输出服务。
    /// </summary>
    private readonly ISubtitleWriter _subtitleWriter = subtitleWriter;

    /// <summary>
    /// Zeayii 字幕产物解析器。
    /// </summary>
    private readonly SubtitleArtifactResolver _subtitleArtifactResolver = subtitleArtifactResolver;

    /// <summary>
    /// Zeayii GPU 冲突门控器。
    /// </summary>
    private readonly GpuConflictGate _gpuConflictGate = new(options.Runtime.GpuConflictPolicy);

    /// <summary>
    /// Zeayii 执行全部媒体任务。
    /// </summary>
    /// <param name="arguments">Zeayii 命令参数。</param>
    /// <param name="cancellationToken">Zeayii 取消令牌。</param>
    public async Task RunAsync(SubaArguments arguments, CancellationToken cancellationToken)
    {
        var contexts = new List<TaskContext>(arguments.Inputs.Count);
        foreach (var input in arguments.Inputs)
        {
            var context = new TaskContext(_globalContext, Path.GetFullPath(input), arguments.Prompt, arguments.FixPrompt);
            _globalContext.Presentation.UpdateTask(context.TaskName, TaskStage.None, TaskStatus.Pending);
            if (await ShouldSkipByTranslatedArtifactAsync(context, cancellationToken).ConfigureAwait(false))
            {
                context.MarkCompleted();
                _globalContext.Log.Info("Pipeline", $"Skip task (translated subtitle exists): {context.InputPath}");
                continue;
            }

            await TryPreloadSourceSubtitleAsync(context, cancellationToken).ConfigureAwait(false);
            contexts.Add(context);
        }

        if (contexts.Count == 0)
        {
            return;
        }

        switch (_options.Runtime.TranslationExecutionMode)
        {
            case TranslationExecutionModePolicy.PerTask:
            {
                await RunPerTaskAsync(contexts, cancellationToken).ConfigureAwait(false);
                break;
            }
            case TranslationExecutionModePolicy.BatchAfterTranscription:
            {
                await RunBatchAfterTranscriptionAsync(contexts, cancellationToken).ConfigureAwait(false);
                break;
            }
            default:
            {
                throw new InvalidOperationException($"Unsupported translation execution mode: {_options.Runtime.TranslationExecutionMode}");
            }
        }
    }

    /// <summary>
    /// Zeayii 按任务内串行执行翻译。
    /// </summary>
    /// <param name="contexts">Zeayii 任务上下文集合。</param>
    /// <param name="cancellationToken">Zeayii 取消令牌。</param>
    private async Task RunPerTaskAsync(IReadOnlyList<TaskContext> contexts, CancellationToken cancellationToken)
    {
        var inputChannel = CreateContextChannel(GetChannelCapacity(_options.Runtime.Preprocess.Parallelism));
        var preprocessChannel = CreateContextChannel(GetChannelCapacity(_options.Runtime.Transcribe.Parallelism));
        var transcribeChannel = CreateContextChannel(GetChannelCapacity(_options.Runtime.Translate.Parallelism));

        await WriteInputsAsync(inputChannel.Writer, contexts, cancellationToken).ConfigureAwait(false);

        var preprocessTasks = StartWorkers(
            GetWorkerCount(_options.Runtime.Preprocess.Parallelism),
            cancellationToken,
            async token =>
            {
                await foreach (var context in inputChannel.Reader.ReadAllAsync(token).ConfigureAwait(false))
                {
                    await ExecuteWithFailureHandlingAsync(context, async taskToken =>
                    {
                        if (!context.HasPreloadedSourceSubtitle)
                        {
                            await RunPreprocessAsync(context, taskToken).ConfigureAwait(false);
                        }

                        await preprocessChannel.Writer.WriteAsync(context, taskToken).ConfigureAwait(false);
                    }, token).ConfigureAwait(false);
                }
            });

        var transcribeTasks = StartWorkers(
            GetWorkerCount(_options.Runtime.Transcribe.Parallelism),
            cancellationToken,
            async token =>
            {
                await foreach (var context in preprocessChannel.Reader.ReadAllAsync(token).ConfigureAwait(false))
                {
                    await ExecuteWithFailureHandlingAsync(context, async taskToken =>
                    {
                        if (!context.HasPreloadedSourceSubtitle)
                        {
                            await RunTranscribeAsync(context, taskToken).ConfigureAwait(false);
                        }

                        await transcribeChannel.Writer.WriteAsync(context, taskToken).ConfigureAwait(false);
                    }, token).ConfigureAwait(false);
                }
            });

        var translateTasks = StartWorkers(
            GetWorkerCount(_options.Runtime.Translate.Parallelism),
            cancellationToken,
            async token =>
            {
                await foreach (var context in transcribeChannel.Reader.ReadAllAsync(token).ConfigureAwait(false))
                {
                    await ExecuteWithFailureHandlingAsync(
                        context,
                        async taskToken => { await RunTranslateAndWriteAsync(context, taskToken).ConfigureAwait(false); },
                        token
                    ).ConfigureAwait(false);
                }
            });

        _ = CompleteWriterAfterWorkersAsync(preprocessChannel.Writer, preprocessTasks);
        _ = CompleteWriterAfterWorkersAsync(transcribeChannel.Writer, transcribeTasks);

        await Task.WhenAll(preprocessTasks.Concat(transcribeTasks).Concat(translateTasks)).ConfigureAwait(false);
    }

    /// <summary>
    /// Zeayii 全量转录后批量翻译执行。
    /// </summary>
    /// <param name="contexts">Zeayii 任务上下文集合。</param>
    /// <param name="cancellationToken">Zeayii 取消令牌。</param>
    private async Task RunBatchAfterTranscriptionAsync(IReadOnlyList<TaskContext> contexts, CancellationToken cancellationToken)
    {
        var transcribedContexts = new ConcurrentQueue<TaskContext>();

        var inputChannel = CreateContextChannel(GetChannelCapacity(_options.Runtime.Preprocess.Parallelism));
        var preprocessChannel = CreateContextChannel(GetChannelCapacity(_options.Runtime.Transcribe.Parallelism));

        await WriteInputsAsync(inputChannel.Writer, contexts, cancellationToken).ConfigureAwait(false);

        var preprocessTasks = StartWorkers(
            GetWorkerCount(_options.Runtime.Preprocess.Parallelism),
            cancellationToken,
            async token =>
            {
                await foreach (var context in inputChannel.Reader.ReadAllAsync(token).ConfigureAwait(false))
                {
                    await ExecuteWithFailureHandlingAsync(context, async taskToken =>
                    {
                        if (!context.HasPreloadedSourceSubtitle)
                        {
                            await RunPreprocessAsync(context, taskToken).ConfigureAwait(false);
                        }

                        await preprocessChannel.Writer.WriteAsync(context, taskToken).ConfigureAwait(false);
                    }, token).ConfigureAwait(false);
                }
            });

        var transcribeTasks = StartWorkers(
            GetWorkerCount(_options.Runtime.Transcribe.Parallelism),
            cancellationToken,
            async token =>
            {
                await foreach (var context in preprocessChannel.Reader.ReadAllAsync(token).ConfigureAwait(false))
                {
                    await ExecuteWithFailureHandlingAsync(context, async taskToken =>
                        {
                            if (!context.HasPreloadedSourceSubtitle)
                            {
                                await RunTranscribeAsync(context, taskToken).ConfigureAwait(false);
                            }

                            transcribedContexts.Enqueue(context);
                        }, token
                    ).ConfigureAwait(false);
                }
            });

        _ = CompleteWriterAfterWorkersAsync(preprocessChannel.Writer, preprocessTasks);
        await Task.WhenAll(preprocessTasks.Concat(transcribeTasks)).ConfigureAwait(false);

        var translateChannel = CreateContextChannel(GetChannelCapacity(_options.Runtime.Translate.Parallelism));
        await WriteInputsAsync(translateChannel.Writer, transcribedContexts.ToArray(), cancellationToken).ConfigureAwait(false);

        var translateTasks = StartWorkers(
            GetWorkerCount(_options.Runtime.Translate.Parallelism),
            cancellationToken,
            async token =>
            {
                await foreach (var context in translateChannel.Reader.ReadAllAsync(token).ConfigureAwait(false))
                {
                    await ExecuteWithFailureHandlingAsync(
                        context,
                        async taskToken => { await RunTranslateAndWriteAsync(context, taskToken).ConfigureAwait(false); },
                        token
                    ).ConfigureAwait(false);
                }
            });

        await Task.WhenAll(translateTasks).ConfigureAwait(false);
    }

    /// <summary>
    /// Zeayii 执行前处理阶段（音频准备、VAD、重叠检测与分离）。
    /// </summary>
    /// <param name="taskContext">Zeayii 任务上下文。</param>
    /// <param name="cancellationToken">Zeayii 取消令牌。</param>
    private async Task RunPreprocessAsync(TaskContext taskContext, CancellationToken cancellationToken)
    {
        _globalContext.Log.Info("Pipeline", $"Process start: {taskContext.InputPath}");

        await ExecuteRuntimeStageAsync(RuntimeStageKind.Preprocess, async () =>
        {
            taskContext.BeginStage(TaskStage.AudioPrepare);
            var prepared = await _audioExtractor.ExtractMonoWavAsync(taskContext.InputPath, cancellationToken).ConfigureAwait(false);
            taskContext.PreparedWavPath = prepared.OutputPath;
            taskContext.IsAudioExtractionBypassed = prepared.IsExtractionBypassed;
            _globalContext.Log.Info("Pipeline", $"Audio prepared: {taskContext.PreparedWavPath}, bypassed={taskContext.IsAudioExtractionBypassed}");

            var audio = WavReader.ReadMono16Pcm(taskContext.PreparedWavPath, out var sampleRate);
            if (sampleRate != taskContext.Global.TargetSampleRateHz)
            {
                throw new InvalidDataException($"Unsupported sample rate {sampleRate}. Expected {taskContext.Global.TargetSampleRateHz}.");
            }

            taskContext.SampleRate = sampleRate;
            taskContext.CompleteStage(TaskStage.AudioPrepare);

            taskContext.BeginStage(TaskStage.Vad);
            var primaryVad = _vadService.DetectSpeech(audio, sampleRate, new VadSettings
                {
                    Threshold = _options.Vad.Threshold,
                    MinSilenceMs = _options.Vad.MinSilenceMs,
                    MinSpeechMs = _options.Vad.MinSpeechMs,
                    MaxSpeechSeconds = _options.Vad.MaxSpeechSeconds,
                    SpeechPadMs = _options.Vad.SpeechPadMs,
                    NegThreshold = _options.Vad.NegThreshold,
                    MinSilenceAtMaxSpeechMs = _options.Vad.MinSilenceAtMaxSpeechMs,
                    UseMaxPossibleSilenceAtMaxSpeech = _options.Vad.UseMaxPossibleSilenceAtMaxSpeech
                }
            );
            taskContext.CompleteStage(TaskStage.Vad);

            taskContext.BeginStage(TaskStage.OverlapResolve);
            taskContext.AudioSegments.AddRange(_options.Overlap.DetectionPolicy == StageSwitchPolicy.Enabled ? ResolveOverlapSegments(primaryVad, sampleRate) : primaryVad);
            taskContext.CompleteStage(TaskStage.OverlapResolve);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Zeayii 执行转录与原文字幕写出阶段。
    /// </summary>
    /// <param name="taskContext">Zeayii 任务上下文。</param>
    /// <param name="cancellationToken">Zeayii 取消令牌。</param>
    private async Task RunTranscribeAsync(TaskContext taskContext, CancellationToken cancellationToken)
    {
        await ExecuteRuntimeStageAsync(RuntimeStageKind.Transcribe, async () =>
        {
            taskContext.BeginStage(TaskStage.Transcribe);
            var subtitles = await _transcriptionService.TranscribeAsync(taskContext.AudioSegments, taskContext.SampleRate, cancellationToken).ConfigureAwait(false);
            taskContext.SubtitleSegments.AddRange(subtitles);
            taskContext.AudioSegments.Clear();
            taskContext.CompleteStage(TaskStage.Transcribe);

            taskContext.BeginStage(TaskStage.SubtitleWrite);
            await _subtitleWriter.WriteAsync(
                taskContext,
                _options.Transcription.OutputLanguageTag,
                _options.SubtitleFormatPolicy,
                translated: false,
                partial: false,
                cancellationToken
            ).ConfigureAwait(false);
            taskContext.CompleteStage(TaskStage.SubtitleWrite);
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Zeayii 执行翻译与译文字幕写出阶段。
    /// </summary>
    /// <param name="taskContext">Zeayii 任务上下文。</param>
    /// <param name="cancellationToken">Zeayii 取消令牌。</param>
    private async Task RunTranslateAndWriteAsync(TaskContext taskContext, CancellationToken cancellationToken)
    {
        await ExecuteRuntimeStageAsync(RuntimeStageKind.Translate, async () =>
        {
            _subtitleArtifactResolver.DeletePartialIfExists(taskContext.InputPath, _options.Translation.Language.Name, _options.SubtitleFormatPolicy);
            taskContext.BeginStage(TaskStage.Translate);
            var interval = Math.Max(0, _options.Translation.PartialWriteInterval);
            var nextMilestone = interval <= 0 ? int.MaxValue : interval;
            await _translationService.TranslateAsync(
                taskContext.SubtitleSegments,
                taskContext.Prompt,
                taskContext.FixPrompt,
                async (completed, all, token) =>
                {
                    if (interval <= 0)
                    {
                        return;
                    }

                    if (completed < nextMilestone && completed != all)
                    {
                        return;
                    }

                    if (completed >= nextMilestone)
                    {
                        while (nextMilestone <= completed)
                        {
                            nextMilestone += interval;
                        }
                    }

                    _globalContext.Log.Info("Translate", $"Task={taskContext.TaskName} Progress={completed}/{all}");
                    await _subtitleWriter.WriteAsync(
                        taskContext,
                        _options.Translation.Language.Name,
                        _options.SubtitleFormatPolicy,
                        translated: true,
                        partial: true,
                        token
                    ).ConfigureAwait(false);
                },
                cancellationToken
            ).ConfigureAwait(false);
            taskContext.CompleteStage(TaskStage.Translate);

            taskContext.BeginStage(TaskStage.SubtitleWrite);
            await _subtitleWriter.WriteAsync(
                taskContext,
                _options.Translation.Language.Name,
                _options.SubtitleFormatPolicy,
                translated: true,
                partial: false,
                cancellationToken
            ).ConfigureAwait(false);
            _subtitleArtifactResolver.DeletePartialIfExists(taskContext.InputPath, _options.Translation.Language.Name, _options.SubtitleFormatPolicy);
            taskContext.CompleteStage(TaskStage.SubtitleWrite);
        }, cancellationToken).ConfigureAwait(false);

        taskContext.MarkCompleted();
        _globalContext.Log.Info("Pipeline", $"Process done: {taskContext.InputPath}");
    }

    /// <summary>
    /// Zeayii 执行单个运行阶段。
    /// </summary>
    /// <param name="stageKind">Zeayii 运行阶段类型。</param>
    /// <param name="action">Zeayii 执行逻辑。</param>
    /// <param name="cancellationToken">Zeayii 取消令牌。</param>
    private async Task ExecuteRuntimeStageAsync(RuntimeStageKind stageKind, Func<Task> action, CancellationToken cancellationToken)
    {
        var device = ResolveDevice(stageKind);
        if (device == ExecutionDevicePolicy.Cpu)
        {
            await action().ConfigureAwait(false);
            return;
        }

        await using var lease = await _gpuConflictGate.AcquireAsync(stageKind, cancellationToken).ConfigureAwait(false);
        await action().ConfigureAwait(false);
    }

    /// <summary>
    /// Zeayii 解析阶段执行设备。
    /// </summary>
    /// <param name="stageKind">Zeayii 运行阶段类型。</param>
    /// <returns>Zeayii 设备策略。</returns>
    private ExecutionDevicePolicy ResolveDevice(RuntimeStageKind stageKind)
    {
        if (stageKind == RuntimeStageKind.Translate && _options.Translation.Provider == TranslationProviderPolicy.OpenAi)
        {
            return ExecutionDevicePolicy.Cpu;
        }

        return stageKind switch
        {
            RuntimeStageKind.Preprocess => _options.Runtime.Preprocess.Device,
            RuntimeStageKind.Transcribe => _options.Runtime.Transcribe.Device,
            RuntimeStageKind.Translate => _options.Runtime.Translate.Device,
            _ => ExecutionDevicePolicy.Cpu
        };
    }

    /// <summary>
    /// Zeayii 统一执行失败处理。
    /// </summary>
    /// <param name="taskContext">Zeayii 任务上下文。</param>
    /// <param name="action">Zeayii 执行逻辑，参数为任务级取消令牌。</param>
    /// <param name="cancellationToken">Zeayii 全局取消令牌。</param>
    private async Task ExecuteWithFailureHandlingAsync(TaskContext taskContext, Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        using var taskCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var watch = Stopwatch.StartNew();

        try
        {
            await action(taskCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            await taskCts.CancelAsync();
            var canceled = new TaskCanceledException($"Task canceled: {taskContext.TaskName}", ex);
            taskContext.FailStage(taskContext.CurrentStage == TaskStage.None ? TaskStage.Failed : taskContext.CurrentStage, canceled);
            _globalContext.Log.Warn("Pipeline", $"Task canceled: name={taskContext.TaskName}, stage={taskContext.CurrentStage}, elapsed={FormatElapsed(watch.Elapsed)}", ex);
        }
        catch (Exception ex)
        {
            await taskCts.CancelAsync();
            taskContext.FailStage(taskContext.CurrentStage == TaskStage.None ? TaskStage.Failed : taskContext.CurrentStage, ex);
            _globalContext.Log.Error("Pipeline", $"Task failed: name={taskContext.TaskName}, stage={taskContext.CurrentStage}, elapsed={FormatElapsed(watch.Elapsed)}", ex);
        }
    }

    /// <summary>
    /// Zeayii 格式化耗时文本（hh:mm:ss.fff）。
    /// </summary>
    /// <param name="elapsed">Zeayii 耗时。</param>
    /// <returns>Zeayii 格式化字符串。</returns>
    private static string FormatElapsed(TimeSpan elapsed) => elapsed.ToString(@"hh\:mm\:ss\.fff");

    /// <summary>
    /// Zeayii 判断是否应因译文产物已存在而跳过任务。
    /// </summary>
    /// <param name="taskContext">Zeayii 任务上下文。</param>
    /// <param name="cancellationToken">Zeayii 取消令牌。</param>
    /// <returns>Zeayii 是否跳过任务。</returns>
    private Task<bool> ShouldSkipByTranslatedArtifactAsync(TaskContext taskContext, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_options.Runtime.ArtifactOverwritePolicy == ArtifactOverwritePolicy.Overwrite)
        {
            return Task.FromResult(false);
        }

        var exists = _subtitleArtifactResolver.Exists(taskContext.InputPath, _options.Translation.Language.Name, _options.SubtitleFormatPolicy);
        return Task.FromResult(exists);
    }

    /// <summary>
    /// Zeayii 尝试预加载源语言字幕产物。
    /// </summary>
    /// <param name="taskContext">Zeayii 任务上下文。</param>
    /// <param name="cancellationToken">Zeayii 取消令牌。</param>
    private async Task TryPreloadSourceSubtitleAsync(TaskContext taskContext, CancellationToken cancellationToken)
    {
        if (_options.Runtime.ArtifactOverwritePolicy == ArtifactOverwritePolicy.Overwrite)
        {
            return;
        }

        var sourceLanguage = _options.Transcription.OutputLanguageTag;
        var sourceExists = _subtitleArtifactResolver.Exists(taskContext.InputPath, sourceLanguage, _options.SubtitleFormatPolicy);
        if (!sourceExists)
        {
            return;
        }

        var segments = await _subtitleArtifactResolver.ReadAsync(taskContext.InputPath, sourceLanguage, _options.SubtitleFormatPolicy, cancellationToken).ConfigureAwait(false);
        if (segments.Count == 0)
        {
            return;
        }

        taskContext.SubtitleSegments.Clear();
        taskContext.SubtitleSegments.AddRange(segments);
        taskContext.HasPreloadedSourceSubtitle = true;
        _globalContext.Log.Info("Pipeline", $"Source subtitle preloaded: {taskContext.InputPath}");
    }

    /// <summary>
    /// Zeayii 创建上下文有界通道。
    /// </summary>
    /// <param name="capacity">Zeayii 通道容量。</param>
    /// <returns>Zeayii 通道实例。</returns>
    private static Channel<TaskContext> CreateContextChannel(int capacity)
    {
        return Channel.CreateBounded<TaskContext>(new BoundedChannelOptions(Math.Max(1, capacity))
        {
            SingleWriter = false,
            SingleReader = false,
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    /// <summary>
    /// Zeayii 批量写入任务上下文。
    /// </summary>
    /// <param name="writer">Zeayii 通道写入器。</param>
    /// <param name="contexts">Zeayii 上下文集合。</param>
    /// <param name="cancellationToken">Zeayii 取消令牌。</param>
    private static async Task WriteInputsAsync(ChannelWriter<TaskContext> writer, IReadOnlyList<TaskContext> contexts, CancellationToken cancellationToken)
    {
        foreach (var context in contexts)
        {
            await writer.WriteAsync(context, cancellationToken).ConfigureAwait(false);
        }

        writer.TryComplete();
    }

    /// <summary>
    /// Zeayii 根据阶段并发计算通道容量。
    /// </summary>
    /// <param name="parallelism">Zeayii 阶段并发数量。</param>
    /// <returns>Zeayii 通道容量。</returns>
    private static int GetChannelCapacity(int parallelism) => Math.Max(1, parallelism * 2);

    /// <summary>
    /// Zeayii 计算最终工作线程数量。
    /// </summary>
    /// <param name="parallelism">Zeayii 阶段并发数量。</param>
    /// <returns>Zeayii 工作线程数量。</returns>
    private int GetWorkerCount(int parallelism)
        => Math.Max(1, Math.Min(Math.Max(1, parallelism), Math.Max(1, _options.Runtime.MaxDegreeOfParallelism)));

    /// <summary>
    /// Zeayii 启动工作线程集合。
    /// </summary>
    /// <param name="workerCount">Zeayii 工作线程数量。</param>
    /// <param name="cancellationToken">Zeayii 取消令牌。</param>
    /// <param name="worker">Zeayii 工作函数。</param>
    /// <returns>Zeayii 工作任务集合。</returns>
    private static IReadOnlyList<Task> StartWorkers(int workerCount, CancellationToken cancellationToken, Func<CancellationToken, Task> worker)
    {
        var tasks = new List<Task>(workerCount);
        for (var i = 0; i < workerCount; i++)
        {
            tasks.Add(Task.Run(() => worker(cancellationToken), cancellationToken));
        }

        return tasks;
    }

    /// <summary>
    /// Zeayii 在上游工作完成后结束写入器。
    /// </summary>
    /// <param name="writer">Zeayii 通道写入器。</param>
    /// <param name="upstreamTasks">Zeayii 上游任务集合。</param>
    private static async Task CompleteWriterAfterWorkersAsync(ChannelWriter<TaskContext> writer, IReadOnlyList<Task> upstreamTasks)
    {
        try
        {
            await Task.WhenAll(upstreamTasks).ConfigureAwait(false);
            writer.TryComplete();
        }
        catch (Exception ex)
        {
            writer.TryComplete(ex);
        }
    }

    /// <summary>
    /// Zeayii 执行重叠段处理。
    /// </summary>
    /// <param name="primarySegments">Zeayii 主 VAD 段。</param>
    /// <param name="sampleRate">Zeayii 采样率。</param>
    /// <returns>Zeayii 重建后的段列表。</returns>
    private List<AudioSegment> ResolveOverlapSegments(IReadOnlyList<AudioSegment> primarySegments, int sampleRate)
    {
        var merged = new List<AudioSegment>();
        var index = 1;

        foreach (var segment in primarySegments)
        {
            if (!_overlapDetector.HasOverlap(segment, sampleRate))
            {
                merged.Add(new AudioSegment
                    {
                        Index = index++,
                        StartSample = segment.StartSample,
                        EndSample = segment.EndSample,
                        Audio = segment.Audio,
                        AudioOffset = segment.AudioOffset,
                        AudioLength = segment.AudioLength,
                        Speaker = segment.Speaker,
                        IsSpeechOverlapped = false
                    }
                );
                continue;
            }

            var separated = _audioSeparator.Separate(segment);
            var speakerSegments = new List<AudioSegment>();
            for (var speaker = 0; speaker < separated.Count; speaker++)
            {
                var detected = _vadService.DetectSpeech(separated[speaker], sampleRate, new VadSettings
                    {
                        Threshold = _options.Vad.Threshold,
                        MinSilenceMs = _options.Vad.MinSilenceMs,
                        MinSpeechMs = _options.Separation.SeparatedVadMinSpeechMs,
                        MaxSpeechSeconds = _options.Separation.SeparatedVadMaxSpeechMs / 1000f,
                        SpeechPadMs = _options.Separation.SeparatedVadSpeechPadMs,
                        NegThreshold = _options.Separation.SeparatedVadNegThreshold,
                        MinSilenceAtMaxSpeechMs = _options.Separation.SeparatedVadMinSilenceAtMaxSpeechMs,
                        UseMaxPossibleSilenceAtMaxSpeech = _options.Separation.SeparatedVadUseMaxPossibleSilenceAtMaxSpeech
                    }
                );

                foreach (var child in detected)
                {
                    speakerSegments.Add(new AudioSegment
                    {
                        Index = 0,
                        StartSample = segment.StartSample + child.StartSample,
                        EndSample = segment.StartSample + child.EndSample,
                        Audio = child.Audio,
                        AudioOffset = child.AudioOffset,
                        AudioLength = child.AudioLength,
                        Speaker = speaker,
                        IsSpeechOverlapped = true
                    });
                }
            }

            speakerSegments.Sort(static (left, right) =>
            {
                var startCompare = left.StartSample.CompareTo(right.StartSample);
                if (startCompare != 0)
                {
                    return startCompare;
                }

                var endCompare = left.EndSample.CompareTo(right.EndSample);
                return endCompare != 0 ? endCompare : left.Speaker.CompareTo(right.Speaker);
            });

            foreach (var resolved in speakerSegments)
            {
                merged.Add(new AudioSegment
                {
                    Index = index++,
                    StartSample = resolved.StartSample,
                    EndSample = resolved.EndSample,
                    Audio = resolved.Audio,
                    AudioOffset = resolved.AudioOffset,
                    AudioLength = resolved.AudioLength,
                    Speaker = resolved.Speaker,
                    IsSpeechOverlapped = true
                });
            }
        }

        return merged;
    }

    /// <summary>
    /// Zeayii 运行阶段标识。
    /// </summary>
    private enum RuntimeStageKind : byte
    {
        /// <summary>
        /// Zeayii 前处理阶段。
        /// </summary>
        Preprocess = 1,

        /// <summary>
        /// Zeayii 转录阶段。
        /// </summary>
        Transcribe = 2,

        /// <summary>
        /// Zeayii 翻译阶段。
        /// </summary>
        Translate = 3
    }

    /// <summary>
    /// Zeayii GPU 冲突门控器。
    /// </summary>
    private sealed class GpuConflictGate
    {
        /// <summary>
        /// Zeayii 冲突策略。
        /// </summary>
        private readonly GpuConflictPolicy _policy;

        /// <summary>
        /// Zeayii 前处理与转录冲突锁。
        /// </summary>
        private readonly SemaphoreSlim _preprocessTranscribeSemaphore = new(1, 1);

        /// <summary>
        /// Zeayii 前处理与翻译冲突锁。
        /// </summary>
        private readonly SemaphoreSlim _preprocessTranslateSemaphore = new(1, 1);

        /// <summary>
        /// Zeayii 转录与翻译冲突锁。
        /// </summary>
        private readonly SemaphoreSlim _transcribeTranslateSemaphore = new(1, 1);

        /// <summary>
        /// Zeayii 初始化门控器。
        /// </summary>
        /// <param name="policy">Zeayii 冲突策略。</param>
        public GpuConflictGate(GpuConflictPolicy policy)
        {
            _policy = policy;
        }

        /// <summary>
        /// Zeayii 申请阶段冲突租约。
        /// </summary>
        /// <param name="stageKind">Zeayii 运行阶段。</param>
        /// <param name="cancellationToken">Zeayii 取消令牌。</param>
        /// <returns>Zeayii 租约对象。</returns>
        public async ValueTask<GpuConflictLease> AcquireAsync(RuntimeStageKind stageKind, CancellationToken cancellationToken)
        {
            var locks = ResolveLocks(stageKind);
            foreach (var gate in locks)
            {
                await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            return new GpuConflictLease(locks);
        }

        /// <summary>
        /// Zeayii 解析阶段所需冲突锁集合。
        /// </summary>
        /// <param name="stageKind">Zeayii 运行阶段。</param>
        /// <returns>Zeayii 冲突锁集合。</returns>
        private IReadOnlyList<SemaphoreSlim> ResolveLocks(RuntimeStageKind stageKind)
        {
            var gates = new List<SemaphoreSlim>(2);
            switch (stageKind)
            {
                case RuntimeStageKind.Preprocess:
                    if ((_policy & GpuConflictPolicy.PreprocessVsTranscribe) == GpuConflictPolicy.PreprocessVsTranscribe)
                    {
                        gates.Add(_preprocessTranscribeSemaphore);
                    }

                    if ((_policy & GpuConflictPolicy.PreprocessVsTranslate) == GpuConflictPolicy.PreprocessVsTranslate)
                    {
                        gates.Add(_preprocessTranslateSemaphore);
                    }

                    break;
                case RuntimeStageKind.Transcribe:
                    if ((_policy & GpuConflictPolicy.PreprocessVsTranscribe) == GpuConflictPolicy.PreprocessVsTranscribe)
                    {
                        gates.Add(_preprocessTranscribeSemaphore);
                    }

                    if ((_policy & GpuConflictPolicy.TranscribeVsTranslate) == GpuConflictPolicy.TranscribeVsTranslate)
                    {
                        gates.Add(_transcribeTranslateSemaphore);
                    }

                    break;
                case RuntimeStageKind.Translate:
                    if ((_policy & GpuConflictPolicy.PreprocessVsTranslate) == GpuConflictPolicy.PreprocessVsTranslate)
                    {
                        gates.Add(_preprocessTranslateSemaphore);
                    }

                    if ((_policy & GpuConflictPolicy.TranscribeVsTranslate) == GpuConflictPolicy.TranscribeVsTranslate)
                    {
                        gates.Add(_transcribeTranslateSemaphore);
                    }

                    break;
            }

            return gates;
        }
    }

    /// <summary>
    /// Zeayii GPU 冲突租约。
    /// </summary>
    private sealed class GpuConflictLease : IAsyncDisposable
    {
        /// <summary>
        /// Zeayii 冲突锁集合。
        /// </summary>
        private readonly IReadOnlyList<SemaphoreSlim> _locks;

        /// <summary>
        /// Zeayii 是否已释放。
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Zeayii 初始化租约。
        /// </summary>
        /// <param name="locks">Zeayii 冲突锁集合。</param>
        public GpuConflictLease(IReadOnlyList<SemaphoreSlim> locks)
        {
            _locks = locks;
        }

        /// <summary>
        /// Zeayii 释放冲突锁。
        /// </summary>
        public ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return ValueTask.CompletedTask;
            }

            for (var i = _locks.Count - 1; i >= 0; i--)
            {
                _locks[i].Release();
            }

            _disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
