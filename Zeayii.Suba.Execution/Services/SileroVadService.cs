using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Zeayii.Suba.Core.Abstractions;
using Zeayii.Suba.Core.Configuration.Options;
using Zeayii.Suba.Core.Orchestration;

namespace Zeayii.Suba.Core.Services;

/// <summary>
/// Zeayii 基于 Silero ONNX 的 VAD 服务实现。
/// </summary>
/// <param name="options">Zeayii 运行配置。</param>
/// <param name="onnxSessionFactory">Zeayii ONNX 会话工厂。</param>
internal sealed class SileroVadService(SubaOptions options, OnnxSessionFactory onnxSessionFactory) : IVadService, IDisposable
{
    /// <summary>
    /// Zeayii ONNX 推理会话。
    /// </summary>
    private readonly InferenceSession _session = onnxSessionFactory.CreateSession(Path.Combine(options.ModelsRoot, "onnx-community", "silero-vad", "onnx", "model.onnx"), OnnxRuntimeStageKind.Preprocess);


    /// <summary>
    /// Zeayii 执行语音活动检测并输出语音段集合。
    /// </summary>
    /// <param name="audio">Zeayii 单声道音频。</param>
    /// <param name="sampleRate">Zeayii 采样率。</param>
    /// <param name="settings">Zeayii VAD 配置。</param>
    /// <returns>Zeayii 语音段集合。</returns>
    public IReadOnlyList<AudioSegment> DetectSpeech(float[] audio, int sampleRate, VadSettings settings)
    {
        const int windowSize = 512;
        var minSilenceSamples = settings.MinSilenceMs * sampleRate / 1000;
        var minSpeechSamples = settings.MinSpeechMs * sampleRate / 1000;
        var speechPadSamples = settings.SpeechPadMs * sampleRate / 1000;
        var maxSpeechSamples = (int)(settings.MaxSpeechSeconds * sampleRate);
        var minSilenceAtMaxSpeechSamples = (int)(settings.MinSilenceAtMaxSpeechMs * sampleRate / 1000f);
        var negThreshold = settings.NegThreshold;

        var segments = new List<AudioSegment>();
        var state = new DenseTensor<float>([2, 1, 128]);
        var srTensor = new DenseTensor<long>([1])
        {
            [0] = sampleRate
        };

        var inSpeech = false;
        var speechStart = 0;
        var potentialEnd = -1;
        var previousEnd = -1;
        var nextStart = -1;
        var index = 0;

        var inputBuffer = new float[windowSize];
        var inputTensor = new DenseTensor<float>(inputBuffer, [1, windowSize]);
        var inputNamedValue = NamedOnnxValue.CreateFromTensor("input", inputTensor);
        var stateNamedValue = NamedOnnxValue.CreateFromTensor("state", state);
        var sampleRateNamedValue = NamedOnnxValue.CreateFromTensor("sr", srTensor);
        var runInputs = new[] { inputNamedValue, stateNamedValue, sampleRateNamedValue };
        for (var offset = 0; offset < audio.Length; offset += windowSize)
        {
            var size = Math.Min(windowSize, audio.Length - offset);
            for (var i = 0; i < windowSize; i++)
            {
                inputTensor[0, i] = i < size ? audio[offset + i] : 0f;
            }

            using var results = _session.Run(runInputs);

            var scoreTensor = results.First(x => x.Name == "output").AsTensor<float>();
            var nextState = results.First(x => x.Name == "stateN").AsTensor<float>();
            Copy(nextState, state);

            var score = scoreTensor[0, 0];
            if (!inSpeech)
            {
                if (score >= settings.Threshold)
                {
                    inSpeech = true;
                    speechStart = offset;
                    potentialEnd = -1;
                }

                continue;
            }

            if (score < negThreshold)
            {
                potentialEnd = potentialEnd < 0 ? offset + size : potentialEnd;
                var silenceLen = offset + size - potentialEnd;
                if (silenceLen > minSilenceAtMaxSpeechSamples)
                {
                    previousEnd = potentialEnd;
                    if (nextStart < previousEnd)
                    {
                        nextStart = offset + size;
                    }
                }

                if (silenceLen < minSilenceSamples)
                {
                    continue;
                }

                EmitSegment(segments, audio, ref index, speechStart, potentialEnd, minSpeechSamples);
                inSpeech = false;
                potentialEnd = -1;
                previousEnd = -1;
                nextStart = -1;
            }
            else
            {
                potentialEnd = -1;
                if (offset + size - speechStart < maxSpeechSamples)
                {
                    continue;
                }

                if (previousEnd > speechStart)
                {
                    EmitSegment(segments, audio, ref index, speechStart, previousEnd, minSpeechSamples);
                    if (!settings.UseMaxPossibleSilenceAtMaxSpeech || nextStart <= previousEnd)
                    {
                        continue;
                    }

                    speechStart = nextStart;
                    previousEnd = -1;
                    nextStart = -1;
                }
                else
                {
                    EmitSegment(segments, audio, ref index, speechStart, offset + size, minSpeechSamples);
                    inSpeech = false;
                }
            }
        }

        if (inSpeech)
        {
            EmitSegment(segments, audio, ref index, speechStart, audio.Length - 1, minSpeechSamples);
        }

        ApplyPadding(segments, audio, speechPadSamples);
        return segments;
    }

    /// <summary>
    /// Zeayii 追加单个语音段。
    /// </summary>
    /// <param name="segments">Zeayii 结果集合。</param>
    /// <param name="audio">Zeayii 原始音频。</param>
    /// <param name="index">Zeayii 段序号引用。</param>
    /// <param name="start">Zeayii 起始采样点。</param>
    /// <param name="end">Zeayii 结束采样点。</param>
    /// <param name="minSpeechSamples">Zeayii 最小语音长度。</param>
    private static void EmitSegment(List<AudioSegment> segments, float[] audio, ref int index, int start, int end, int minSpeechSamples)
    {
        if (end <= start || end - start < minSpeechSamples)
        {
            return;
        }

        var length = end - start + 1;
        segments.Add(new AudioSegment
        {
            Index = ++index,
            StartSample = start,
            EndSample = end,
            Audio = audio,
            AudioOffset = start,
            AudioLength = length
        });
    }

    /// <summary>
    /// Zeayii 对语音段应用边界补偿并避免相邻段重叠。
    /// </summary>
    /// <param name="segments">Zeayii 语音段集合。</param>
    /// <param name="audio">Zeayii 原始音频。</param>
    /// <param name="speechPadSamples">Zeayii 补偿采样点。</param>
    private static void ApplyPadding(List<AudioSegment> segments, float[] audio, int speechPadSamples)
    {
        if (speechPadSamples <= 0 || segments.Count == 0)
        {
            return;
        }

        var paddedSegments = new List<AudioSegment>(segments.Count);
        for (var i = 0; i < segments.Count; i++)
        {
            var current = segments[i];
            var paddedStart = Math.Max(current.StartSample - speechPadSamples, 0);
            var paddedEnd = Math.Min(current.EndSample + speechPadSamples, audio.Length - 1);

            if (i < segments.Count - 1)
            {
                var next = segments[i + 1];
                var nextStart = Math.Max(next.StartSample - speechPadSamples, 0);
                if (paddedEnd >= nextStart)
                {
                    var midpoint = (paddedEnd + nextStart) / 2;
                    paddedEnd = midpoint;
                }
            }

            paddedSegments.Add(new AudioSegment
            {
                Index = current.Index,
                StartSample = paddedStart,
                EndSample = paddedEnd,
                Audio = current.Audio,
                AudioOffset = paddedStart,
                AudioLength = paddedEnd - paddedStart + 1,
                Speaker = current.Speaker,
                IsSpeechOverlapped = current.IsSpeechOverlapped
            });
        }

        segments.Clear();
        segments.AddRange(paddedSegments);
    }

    /// <summary>
    /// Zeayii 复制 ONNX 状态张量。
    /// </summary>
    /// <param name="source">Zeayii 源张量。</param>
    /// <param name="destination">Zeayii 目标张量。</param>
    private static void Copy(Tensor<float> source, DenseTensor<float> destination)
    {
        if (source is DenseTensor<float> denseTensor)
        {
            denseTensor.Buffer.Span.CopyTo(destination.Buffer.Span);
            return;
        }

        source.ToArray().AsSpan().CopyTo(destination.Buffer.Span);
    }

    /// <summary>
    /// Zeayii 释放 ONNX 会话资源。
    /// </summary>
    public void Dispose() => _session.Dispose();
}