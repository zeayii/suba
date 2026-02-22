using Zeayii.Suba.Core.Abstractions;
using Zeayii.Suba.Core.Configuration.Options;
using Zeayii.Suba.Core.Configuration.Policies;
using Zeayii.Suba.Core.Contexts;
using Zeayii.Suba.Core.Orchestration;
using Zeayii.Suba.Core.Services;
using Zeayii.Suba.Execution.Tests.TestSupport;

namespace Zeayii.Suba.Execution.Tests;

/// <summary>
/// Zeayii 流水线阶段与参数传递测试。
/// </summary>
public sealed class SubaPipelineTests
{
    /// <summary>
    /// Zeayii 验证二次 VAD 参数来自 Separation 配置。
    /// </summary>
    [Fact]
    public async Task RunAsync_ShouldUseSeparatedVadOptionsForOverlapBranches()
    {
        var wavPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wav");
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"suba-out-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        TestWaveFileBuilder.WriteMonoPcm16(wavPath, 16000, Enumerable.Repeat((short)512, 4096).ToArray());

        try
        {
            var options = CreateOptions(StageSwitchPolicy.Enabled);
            var global = new GlobalContext(options);
            var extractor = new FakeAudioExtractor(wavPath);
            var vad = new FakeVadService();
            var overlap = new FakeOverlapDetector(alwaysOverlap: true);
            var separator = new FakeAudioSeparator();
            var transcription = new FakeTranscriptionService();
            var translation = new FakeTranslationService();
            var writer = new FakeSubtitleWriter();
            var pipeline = new SubaPipeline(options, global, extractor, vad, overlap, separator, transcription, translation, writer, new SubtitleArtifactResolver());

            var arguments = new SubaArguments
            {
                Inputs = [wavPath],
                Prompt = "提示词",
                FixPrompt = "修复提示词"
            };

            await pipeline.RunAsync(arguments, CancellationToken.None);

            Assert.True(vad.SettingsHistory.Count >= 3);
            var primary = vad.SettingsHistory[0];
            var secondary = vad.SettingsHistory[1];
            Assert.Equal(options.Vad.MinSpeechMs, primary.MinSpeechMs);
            Assert.Equal(options.Separation.SeparatedVadMinSpeechMs, secondary.MinSpeechMs);
            Assert.Equal(options.Separation.SeparatedVadMaxSpeechMs / 1000f, secondary.MaxSpeechSeconds, 3);
            Assert.Equal(options.Separation.SeparatedVadNegThreshold, secondary.NegThreshold, 3);
            Assert.Equal(options.Separation.SeparatedVadMinSilenceAtMaxSpeechMs, secondary.MinSilenceAtMaxSpeechMs, 3);
            Assert.Equal(options.Separation.SeparatedVadUseMaxPossibleSilenceAtMaxSpeech, secondary.UseMaxPossibleSilenceAtMaxSpeech);
            Assert.Equal(2, writer.Calls.Count);
            Assert.Contains(writer.Calls, call => call is { translated: true, language: "zh-CN" });
        }
        finally
        {
            File.Delete(wavPath);
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    /// <summary>
    /// Zeayii 验证关闭重叠检测策略时不触发分离。
    /// </summary>
    [Fact]
    public async Task RunAsync_WhenOverlapDisabled_ShouldSkipSeparatorAndSecondaryVad()
    {
        var wavPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wav");
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"suba-out-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        TestWaveFileBuilder.WriteMonoPcm16(wavPath, 16000, Enumerable.Repeat((short)256, 4096).ToArray());

        try
        {
            var options = CreateOptions(StageSwitchPolicy.Disabled);
            var global = new GlobalContext(options);
            var extractor = new FakeAudioExtractor(wavPath);
            var vad = new FakeVadService();
            var overlap = new FakeOverlapDetector(alwaysOverlap: true);
            var separator = new FakeAudioSeparator();
            var transcription = new FakeTranscriptionService();
            var translation = new FakeTranslationService();
            var writer = new FakeSubtitleWriter();
            var pipeline = new SubaPipeline(options, global, extractor, vad, overlap, separator, transcription, translation, writer, new SubtitleArtifactResolver());

            var arguments = new SubaArguments
            {
                Inputs = [wavPath],
                Prompt = "提示词",
                FixPrompt = "修复提示词"
            };

            await pipeline.RunAsync(arguments, CancellationToken.None);

            Assert.Single(vad.SettingsHistory);
            Assert.Equal(0, separator.CallCount);
            Assert.Equal(2, writer.Calls.Count);
        }
        finally
        {
            File.Delete(wavPath);
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    /// <summary>
    /// Zeayii 验证批量翻译模式会在全部转录完成后再执行翻译。
    /// </summary>
    [Fact]
    public async Task RunAsync_WhenBatchAfterTranscription_ShouldTranslateAfterAllTranscribed()
    {
        var wavPathA = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wav");
        var wavPathB = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wav");
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"suba-out-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        TestWaveFileBuilder.WriteMonoPcm16(wavPathA, 16000, Enumerable.Repeat((short)256, 4096).ToArray());
        TestWaveFileBuilder.WriteMonoPcm16(wavPathB, 16000, Enumerable.Repeat((short)512, 4096).ToArray());

        try
        {
            var options = CreateOptions(
                StageSwitchPolicy.Disabled,
                TranslationExecutionModePolicy.BatchAfterTranscription,
                runtimeOverride: runtime => new RuntimeOptions
                {
                    MaxDegreeOfParallelism = 3,
                    CommandTimeoutSeconds = runtime.CommandTimeoutSeconds,
                    Preprocess = runtime.Preprocess,
                    Transcribe = runtime.Transcribe,
                    Translate = runtime.Translate,
                    TranslationExecutionMode = TranslationExecutionModePolicy.BatchAfterTranscription,
                    GpuConflictPolicy = runtime.GpuConflictPolicy,
                    ArtifactOverwritePolicy = runtime.ArtifactOverwritePolicy
                });
            var global = new GlobalContext(options);
            var extractor = new QueueAudioExtractor([wavPathA, wavPathB]);
            var vad = new FakeVadService();
            var overlap = new FakeOverlapDetector(alwaysOverlap: false);
            var separator = new FakeAudioSeparator();
            var transcription = new CountingTranscriptionService(60);
            var translation = new OrderedTranslationService(() => transcription.CompletedCount, expectedCompletedCount: 2);
            var writer = new FakeSubtitleWriter();
            var pipeline = new SubaPipeline(options, global, extractor, vad, overlap, separator, transcription, translation, writer, new SubtitleArtifactResolver());
            var arguments = new SubaArguments
            {
                Inputs = [wavPathA, wavPathB],
                Prompt = "提示词",
                FixPrompt = "修复提示词"
            };

            await pipeline.RunAsync(arguments, CancellationToken.None);

            Assert.Equal(2, transcription.CompletedCount);
            Assert.Equal(2, translation.CallCount);
            Assert.True(translation.AllCallsAfterAllTranscribed);
            Assert.Equal(4, writer.Calls.Count);
        }
        finally
        {
            File.Delete(wavPathA);
            File.Delete(wavPathB);
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    /// <summary>
    /// Zeayii 验证任务内翻译模式会在全部转录完成前提前执行翻译。
    /// </summary>
    [Fact]
    public async Task RunAsync_WhenPerTaskMode_ShouldTranslateBeforeAllTranscribed()
    {
        var wavPathA = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wav");
        var wavPathB = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wav");
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"suba-out-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        TestWaveFileBuilder.WriteMonoPcm16(wavPathA, 16000, Enumerable.Repeat((short)256, 4096).ToArray());
        TestWaveFileBuilder.WriteMonoPcm16(wavPathB, 16000, Enumerable.Repeat((short)512, 4096).ToArray());

        try
        {
            var options = CreateOptions(
                StageSwitchPolicy.Disabled,
                runtimeOverride: runtime => new RuntimeOptions
                {
                    MaxDegreeOfParallelism = 3,
                    CommandTimeoutSeconds = runtime.CommandTimeoutSeconds,
                    Preprocess = runtime.Preprocess,
                    Transcribe = runtime.Transcribe,
                    Translate = runtime.Translate,
                    TranslationExecutionMode = TranslationExecutionModePolicy.PerTask,
                    GpuConflictPolicy = runtime.GpuConflictPolicy,
                    ArtifactOverwritePolicy = runtime.ArtifactOverwritePolicy
                });
            var global = new GlobalContext(options);
            var extractor = new QueueAudioExtractor([wavPathA, wavPathB]);
            var vad = new FakeVadService();
            var overlap = new FakeOverlapDetector(alwaysOverlap: false);
            var separator = new FakeAudioSeparator();
            var transcription = new CountingTranscriptionService(120);
            var translation = new OrderedTranslationService(() => transcription.CompletedCount, expectedCompletedCount: 2);
            var writer = new FakeSubtitleWriter();
            var pipeline = new SubaPipeline(options, global, extractor, vad, overlap, separator, transcription, translation, writer, new SubtitleArtifactResolver());
            var arguments = new SubaArguments
            {
                Inputs = [wavPathA, wavPathB],
                Prompt = "提示词",
                FixPrompt = "修复提示词"
            };

            await pipeline.RunAsync(arguments, CancellationToken.None);

            Assert.Equal(2, transcription.CompletedCount);
            Assert.Equal(2, translation.CallCount);
            Assert.False(translation.AllCallsAfterAllTranscribed);
            Assert.Equal(4, writer.Calls.Count);
        }
        finally
        {
            File.Delete(wavPathA);
            File.Delete(wavPathB);
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    /// <summary>
    /// Zeayii 验证 GPU 冲突策略生效时转录与翻译不会重叠执行。
    /// </summary>
    [Fact]
    public async Task RunAsync_WhenTranscribeTranslateConflictEnabled_ShouldAvoidOverlap()
    {
        var wavPathA = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wav");
        var wavPathB = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wav");
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"suba-out-{Guid.NewGuid():N}");
        Directory.CreateDirectory(outputDirectory);
        TestWaveFileBuilder.WriteMonoPcm16(wavPathA, 16000, Enumerable.Repeat((short)300, 4096).ToArray());
        TestWaveFileBuilder.WriteMonoPcm16(wavPathB, 16000, Enumerable.Repeat((short)600, 4096).ToArray());

        try
        {
            var options = CreateOptions(
                StageSwitchPolicy.Disabled,
                runtimeOverride: runtime => new RuntimeOptions
                {
                    MaxDegreeOfParallelism = 4,
                    CommandTimeoutSeconds = runtime.CommandTimeoutSeconds,
                    Preprocess = new StageExecutionOptions
                    {
                        Device = ExecutionDevicePolicy.Gpu,
                        Parallelism = 2
                    },
                    Transcribe = new StageExecutionOptions
                    {
                        Device = ExecutionDevicePolicy.Gpu,
                        Parallelism = 2
                    },
                    Translate = new StageExecutionOptions
                    {
                        Device = ExecutionDevicePolicy.Gpu,
                        Parallelism = 2
                    },
                    TranslationExecutionMode = TranslationExecutionModePolicy.PerTask,
                    GpuConflictPolicy = GpuConflictPolicy.TranscribeVsTranslate,
                    ArtifactOverwritePolicy = runtime.ArtifactOverwritePolicy
                });

            var global = new GlobalContext(options);
            var extractor = new QueueAudioExtractor([wavPathA, wavPathB]);
            var vad = new FakeVadService();
            var overlap = new FakeOverlapDetector(alwaysOverlap: false);
            var separator = new FakeAudioSeparator();
            var probe = new StageOverlapProbe();
            var transcription = new ProbeTranscriptionService(probe);
            var translation = new ProbeTranslationService(probe);
            var writer = new FakeSubtitleWriter();
            var pipeline = new SubaPipeline(options, global, extractor, vad, overlap, separator, transcription, translation, writer, new SubtitleArtifactResolver());
            var arguments = new SubaArguments
            {
                Inputs = [wavPathA, wavPathB],
                Prompt = "提示词",
                FixPrompt = "修复提示词"
            };

            await pipeline.RunAsync(arguments, CancellationToken.None);

            Assert.False(probe.HasOverlap);
        }
        finally
        {
            File.Delete(wavPathA);
            File.Delete(wavPathB);
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    /// <summary>
    /// Zeayii 创建测试配置。
    /// </summary>
    /// <param name="policy">Zeayii 重叠检测策略。</param>
    /// <param name="translationExecutionMode">翻译执行模式</param>
    /// <param name="runtimeOverride">运行时重写。</param>
    /// <returns>Zeayii 核心配置。</returns>
    private static SubaOptions CreateOptions(
        StageSwitchPolicy policy,
        TranslationExecutionModePolicy translationExecutionMode = TranslationExecutionModePolicy.PerTask,
        Func<RuntimeOptions, RuntimeOptions>? runtimeOverride = null)
    {
        var options = TestSubaOptionsFactory.Create();
        return new SubaOptions
        {
            ModelsRoot = options.ModelsRoot,
            SegmentationModelPath = options.SegmentationModelPath,
            SepformerModelPath = options.SepformerModelPath,
            WhisperModelRoot = options.WhisperModelRoot,
            SubtitleFormatPolicy = options.SubtitleFormatPolicy,
            AudioExtract = options.AudioExtract,
            Vad = options.Vad,
            Overlap = new OverlapOptions
            {
                DetectionPolicy = policy,
                Onset = options.Overlap.Onset,
                Offset = options.Overlap.Offset,
                MinDurationOnSeconds = options.Overlap.MinDurationOnSeconds,
                MinDurationOffSeconds = options.Overlap.MinDurationOffSeconds
            },
            Separation = options.Separation,
            Sepformer = options.Sepformer,
            Transcription = options.Transcription,
            Translation = options.Translation,
            Logging = options.Logging,
            Runtime = runtimeOverride?.Invoke(new RuntimeOptions
            {
                MaxDegreeOfParallelism = options.Runtime.MaxDegreeOfParallelism,
                CommandTimeoutSeconds = options.Runtime.CommandTimeoutSeconds,
                Preprocess = new StageExecutionOptions
                {
                    Device = options.Runtime.Preprocess.Device,
                    Parallelism = options.Runtime.Preprocess.Parallelism
                },
                Transcribe = new StageExecutionOptions
                {
                    Device = options.Runtime.Transcribe.Device,
                    Parallelism = options.Runtime.Transcribe.Parallelism
                },
                Translate = new StageExecutionOptions
                {
                    Device = options.Runtime.Translate.Device,
                    Parallelism = options.Runtime.Translate.Parallelism
                },
                TranslationExecutionMode = translationExecutionMode,
                GpuConflictPolicy = options.Runtime.GpuConflictPolicy,
                ArtifactOverwritePolicy = options.Runtime.ArtifactOverwritePolicy
            }) ?? new RuntimeOptions
            {
                MaxDegreeOfParallelism = options.Runtime.MaxDegreeOfParallelism,
                CommandTimeoutSeconds = options.Runtime.CommandTimeoutSeconds,
                Preprocess = new StageExecutionOptions
                {
                    Device = options.Runtime.Preprocess.Device,
                    Parallelism = options.Runtime.Preprocess.Parallelism
                },
                Transcribe = new StageExecutionOptions
                {
                    Device = options.Runtime.Transcribe.Device,
                    Parallelism = options.Runtime.Transcribe.Parallelism
                },
                Translate = new StageExecutionOptions
                {
                    Device = options.Runtime.Translate.Device,
                    Parallelism = options.Runtime.Translate.Parallelism
                },
                TranslationExecutionMode = translationExecutionMode,
                GpuConflictPolicy = options.Runtime.GpuConflictPolicy,
                ArtifactOverwritePolicy = options.Runtime.ArtifactOverwritePolicy
            }
        };
    }

    /// <summary>
    /// Zeayii 假音频提取器。
    /// </summary>
    /// <param name="wavPath">Zeayii WAV 路径。</param>
    private sealed class FakeAudioExtractor(string wavPath) : IAudioExtractor
    {
        /// <summary>
        /// Zeayii 返回固定音频路径。
        /// </summary>
        /// <param name="inputPath">Zeayii 输入媒体路径。</param>
        /// <param name="cancellationToken">Zeayii 取消令牌。</param>
        /// <returns>Zeayii 已准备音频信息。</returns>
        public Task<PreparedAudio> ExtractMonoWavAsync(string inputPath, CancellationToken cancellationToken)
            => Task.FromResult(new PreparedAudio { OutputPath = wavPath, IsExtractionBypassed = true });
    }

    /// <summary>
    /// Zeayii 队列音频提取器。
    /// </summary>
    /// <param name="wavPaths">Zeayii 音频路径队列。</param>
    private sealed class QueueAudioExtractor(IReadOnlyList<string> wavPaths) : IAudioExtractor
    {
        /// <summary>
        /// Zeayii 当前索引。
        /// </summary>
        private int _index;

        /// <summary>
        /// Zeayii 按顺序返回音频路径。
        /// </summary>
        /// <param name="inputPath">Zeayii 输入媒体路径。</param>
        /// <param name="cancellationToken">Zeayii 取消令牌。</param>
        /// <returns>Zeayii 已准备音频信息。</returns>
        public Task<PreparedAudio> ExtractMonoWavAsync(string inputPath, CancellationToken cancellationToken)
        {
            var current = Math.Min(Interlocked.Increment(ref _index) - 1, wavPaths.Count - 1);
            return Task.FromResult(new PreparedAudio
            {
                OutputPath = wavPaths[current],
                IsExtractionBypassed = true
            });
        }
    }

    /// <summary>
    /// Zeayii 假 VAD 服务。
    /// </summary>
    private sealed class FakeVadService : IVadService
    {
        /// <summary>
        /// Zeayii 设置入参历史。
        /// </summary>
        public List<VadSettings> SettingsHistory { get; } = [];

        /// <summary>
        /// Zeayii 生成固定语音段。
        /// </summary>
        /// <param name="audio">Zeayii 音频采样数据。</param>
        /// <param name="sampleRate">Zeayii 采样率。</param>
        /// <param name="settings">Zeayii VAD 参数。</param>
        /// <returns>Zeayii 语音段集合。</returns>
        public IReadOnlyList<AudioSegment> DetectSpeech(float[] audio, int sampleRate, VadSettings settings)
        {
            SettingsHistory.Add(settings);
            if (SettingsHistory.Count == 1)
            {
                return
                [
                    new AudioSegment
                    {
                        Index = 1,
                        StartSample = 0,
                        EndSample = Math.Min(audio.Length - 1, 3000),
                        Audio = audio.Take(Math.Min(audio.Length, 3001)).ToArray(),
                        Speaker = -1
                    }
                ];
            }

            return
            [
                new AudioSegment
                {
                    Index = 1,
                    StartSample = 0,
                    EndSample = 900,
                    Audio = new float[901],
                    Speaker = -1
                }
            ];
        }
    }

    /// <summary>
    /// Zeayii 假重叠检测器。
    /// </summary>
    /// <param name="alwaysOverlap">Zeayii 是否总返回重叠。</param>
    private sealed class FakeOverlapDetector(bool alwaysOverlap) : IOverlapDetector
    {
        /// <summary>
        /// Zeayii 返回固定重叠判断。
        /// </summary>
        /// <param name="segment">Zeayii 输入语音段。</param>
        /// <param name="sampleRate">Zeayii 采样率。</param>
        /// <returns>Zeayii 是否重叠。</returns>
        public bool HasOverlap(AudioSegment segment, int sampleRate) => alwaysOverlap;
    }

    /// <summary>
    /// Zeayii 假分离器。
    /// </summary>
    private sealed class FakeAudioSeparator : IAudioSeparator
    {
        /// <summary>
        /// Zeayii 分离调用次数。
        /// </summary>
        public int CallCount { get; private set; }

        /// <summary>
        /// Zeayii 返回两路固定音频。
        /// </summary>
        /// <param name="segment">Zeayii 输入语音段。</param>
        /// <returns>Zeayii 分离后的音频通道集合。</returns>
        public IReadOnlyList<float[]> Separate(AudioSegment segment)
        {
            CallCount++;
            return [new float[1200], new float[1400]];
        }
    }

    /// <summary>
    /// Zeayii 假转写服务。
    /// </summary>
    private sealed class FakeTranscriptionService : ITranscriptionService
    {
        /// <summary>
        /// Zeayii 生成固定字幕段。
        /// </summary>
        /// <param name="segments">Zeayii 语音段集合。</param>
        /// <param name="sampleRate">Zeayii 采样率。</param>
        /// <param name="cancellationToken">Zeayii 取消令牌。</param>
        /// <returns>Zeayii 字幕段集合。</returns>
        public Task<IReadOnlyList<SubtitleSegment>> TranscribeAsync(IReadOnlyList<AudioSegment> segments, int sampleRate, CancellationToken cancellationToken)
        {
            var subtitles = segments.Select((segment, index) => new SubtitleSegment
            {
                Index = index + 1,
                StartMs = (int)(segment.StartSample * 1000L / sampleRate),
                EndMs = (int)(segment.EndSample * 1000L / sampleRate),
                Speaker = segment.Speaker,
                OriginalText = $"seg-{index + 1}"
            }).ToList();
            return Task.FromResult<IReadOnlyList<SubtitleSegment>>(subtitles);
        }
    }

    /// <summary>
    /// Zeayii 带计数转录服务。
    /// </summary>
    private sealed class CountingTranscriptionService(int delayMs) : ITranscriptionService
    {
        /// <summary>
        /// Zeayii 已完成转录数。
        /// </summary>
        public int CompletedCount => _completedCount;

        /// <summary>
        /// Zeayii 内部完成计数。
        /// </summary>
        private int _completedCount;

        /// <summary>
        /// Zeayii 执行转录并计数。
        /// </summary>
        /// <param name="segments">Zeayii 语音段集合。</param>
        /// <param name="sampleRate">Zeayii 采样率。</param>
        /// <param name="cancellationToken">Zeayii 取消令牌。</param>
        /// <returns>Zeayii 字幕段集合。</returns>
        public async Task<IReadOnlyList<SubtitleSegment>> TranscribeAsync(IReadOnlyList<AudioSegment> segments, int sampleRate, CancellationToken cancellationToken)
        {
            if (delayMs > 0)
            {
                await Task.Delay(delayMs, cancellationToken);
            }

            var subtitles = segments.Select((segment, index) => new SubtitleSegment
            {
                Index = index + 1,
                StartMs = (int)(segment.StartSample * 1000L / sampleRate),
                EndMs = (int)(segment.EndSample * 1000L / sampleRate),
                Speaker = segment.Speaker,
                OriginalText = $"seg-{index + 1}"
            }).ToList();
            Interlocked.Increment(ref _completedCount);
            return subtitles;
        }
    }

    /// <summary>
    /// Zeayii 假翻译服务。
    /// </summary>
    private sealed class FakeTranslationService : ITranslationService
    {
        /// <summary>
        /// Zeayii 回填译文文本。
        /// </summary>
        /// <param name="segments">Zeayii 字幕段集合。</param>
        /// <param name="prompt">Zeayii 主提示词。</param>
        /// <param name="fixPrompt">Zeayii 修复提示词。</param>
        /// <param name="progressCallback">Zeayii 进度回调。</param>
        /// <param name="cancellationToken">Zeayii 取消令牌。</param>
        /// <returns>Zeayii 异步任务。</returns>
        public Task TranslateAsync(IReadOnlyList<SubtitleSegment> segments, string prompt, string fixPrompt, Func<int, int, CancellationToken, Task>? progressCallback, CancellationToken cancellationToken)
        {
            foreach (var segment in segments)
            {
                segment.TranslatedText = $"译:{segment.OriginalText}";
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Zeayii 顺序校验翻译服务。
    /// </summary>
    /// <param name="getTranscribedCount">Zeayii 获取已转录数量。</param>
    /// <param name="expectedCompletedCount">Zeayii 预期转录完成总数。</param>
    private sealed class OrderedTranslationService(Func<int> getTranscribedCount, int expectedCompletedCount) : ITranslationService
    {
        /// <summary>
        /// Zeayii 调用次数。
        /// </summary>
        public int CallCount { get; private set; }

        /// <summary>
        /// Zeayii 是否全部在转录完成后调用。
        /// </summary>
        public bool AllCallsAfterAllTranscribed { get; private set; } = true;

        /// <summary>
        /// Zeayii 翻译并记录调用顺序。
        /// </summary>
        /// <param name="segments">Zeayii 字幕段集合。</param>
        /// <param name="prompt">Zeayii 主提示词。</param>
        /// <param name="fixPrompt">Zeayii 修复提示词。</param>
        /// <param name="progressCallback">Zeayii 进度回调。</param>
        /// <param name="cancellationToken">Zeayii 取消令牌。</param>
        /// <returns>Zeayii 异步任务。</returns>
        public Task TranslateAsync(IReadOnlyList<SubtitleSegment> segments, string prompt, string fixPrompt, Func<int, int, CancellationToken, Task>? progressCallback, CancellationToken cancellationToken)
        {
            CallCount++;
            if (getTranscribedCount() < expectedCompletedCount)
            {
                AllCallsAfterAllTranscribed = false;
            }

            foreach (var segment in segments)
            {
                segment.TranslatedText = $"译:{segment.OriginalText}";
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Zeayii 阶段重叠探针。
    /// </summary>
    private sealed class StageOverlapProbe
    {
        /// <summary>
        /// Zeayii 转录阶段活动数。
        /// </summary>
        private int _activeTranscribe;

        /// <summary>
        /// Zeayii 翻译阶段活动数。
        /// </summary>
        private int _activeTranslate;

        /// <summary>
        /// Zeayii 是否发生重叠。
        /// </summary>
        public bool HasOverlap { get; private set; }

        /// <summary>
        /// Zeayii 进入转录阶段。
        /// </summary>
        public void EnterTranscribe()
        {
            if (Interlocked.Increment(ref _activeTranscribe) > 0 && Volatile.Read(ref _activeTranslate) > 0)
            {
                HasOverlap = true;
            }
        }

        /// <summary>
        /// Zeayii 离开转录阶段。
        /// </summary>
        public void ExitTranscribe() => Interlocked.Decrement(ref _activeTranscribe);

        /// <summary>
        /// Zeayii 进入翻译阶段。
        /// </summary>
        public void EnterTranslate()
        {
            if (Interlocked.Increment(ref _activeTranslate) > 0 && Volatile.Read(ref _activeTranscribe) > 0)
            {
                HasOverlap = true;
            }
        }

        /// <summary>
        /// Zeayii 离开翻译阶段。
        /// </summary>
        public void ExitTranslate() => Interlocked.Decrement(ref _activeTranslate);
    }

    /// <summary>
    /// Zeayii 转录探针服务。
    /// </summary>
    /// <param name="probe">Zeayii 重叠探针。</param>
    private sealed class ProbeTranscriptionService(StageOverlapProbe probe) : ITranscriptionService
    {
        /// <summary>
        /// Zeayii 记录转录阶段活动窗口。
        /// </summary>
        /// <param name="segments">Zeayii 语音段集合。</param>
        /// <param name="sampleRate">Zeayii 采样率。</param>
        /// <param name="cancellationToken">Zeayii 取消令牌。</param>
        /// <returns>Zeayii 字幕段集合。</returns>
        public async Task<IReadOnlyList<SubtitleSegment>> TranscribeAsync(IReadOnlyList<AudioSegment> segments, int sampleRate, CancellationToken cancellationToken)
        {
            probe.EnterTranscribe();
            try
            {
                await Task.Delay(80, cancellationToken);
                return segments.Select((segment, index) => new SubtitleSegment
                {
                    Index = index + 1,
                    StartMs = (int)(segment.StartSample * 1000L / sampleRate),
                    EndMs = (int)(segment.EndSample * 1000L / sampleRate),
                    Speaker = segment.Speaker,
                    OriginalText = $"seg-{index + 1}"
                }).ToArray();
            }
            finally
            {
                probe.ExitTranscribe();
            }
        }
    }

    /// <summary>
    /// Zeayii 翻译探针服务。
    /// </summary>
    /// <param name="probe">Zeayii 重叠探针。</param>
    private sealed class ProbeTranslationService(StageOverlapProbe probe) : ITranslationService
    {
        /// <summary>
        /// Zeayii 记录翻译阶段活动窗口。
        /// </summary>
        /// <param name="segments">Zeayii 字幕段集合。</param>
        /// <param name="prompt">Zeayii 主提示词。</param>
        /// <param name="fixPrompt">Zeayii 修复提示词。</param>
        /// <param name="progressCallback">Zeayii 进度回调。</param>
        /// <param name="cancellationToken">Zeayii 取消令牌。</param>
        /// <returns>Zeayii 异步任务。</returns>
        public async Task TranslateAsync(IReadOnlyList<SubtitleSegment> segments, string prompt, string fixPrompt, Func<int, int, CancellationToken, Task>? progressCallback, CancellationToken cancellationToken)
        {
            probe.EnterTranslate();
            try
            {
                await Task.Delay(80, cancellationToken);
                foreach (var segment in segments)
                {
                    segment.TranslatedText = $"译:{segment.OriginalText}";
                }
            }
            finally
            {
                probe.ExitTranslate();
            }
        }
    }

    /// <summary>
    /// Zeayii 假字幕写出器。
    /// </summary>
    private sealed class FakeSubtitleWriter : ISubtitleWriter
    {
        /// <summary>
        /// Zeayii 写出调用记录。
        /// </summary>
        public List<(string language, bool translated)> Calls { get; } = [];

        /// <summary>
        /// Zeayii 记录写出调用。
        /// </summary>
        /// <param name="taskContext">Zeayii 任务上下文。</param>
        /// <param name="language">Zeayii 字幕语言标签。</param>
        /// <param name="formatPolicy">Zeayii 字幕格式策略。</param>
        /// <param name="translated">Zeayii 是否译文字幕。</param>
        /// <param name="partial">Zeayii 是否中间产物写出。</param>
        /// <param name="cancellationToken">Zeayii 取消令牌。</param>
        /// <returns>Zeayii 异步任务。</returns>
        public Task WriteAsync(TaskContext taskContext, string language, SubtitleFormatPolicy formatPolicy, bool translated, bool partial, CancellationToken cancellationToken)
        {
            Calls.Add((language, translated));
            return Task.CompletedTask;
        }
    }
}


