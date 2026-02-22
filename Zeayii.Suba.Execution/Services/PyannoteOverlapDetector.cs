using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Zeayii.Suba.Core.Abstractions;
using Zeayii.Suba.Core.Configuration.Options;
using Zeayii.Suba.Core.Orchestration;

namespace Zeayii.Suba.Core.Services;

/// <summary>
/// Zeayii 基于 Pyannote 的重叠语音检测器。
/// </summary>
/// <param name="options">Zeayii 运行配置。</param>
/// <param name="onnxSessionFactory">Zeayii ONNX 会话工厂。</param>
internal sealed class PyannoteOverlapDetector(SubaOptions options, OnnxSessionFactory onnxSessionFactory) : IOverlapDetector, IDisposable
{
    /// <summary>
    /// Zeayii ONNX 推理会话。
    /// </summary>
    private readonly InferenceSession _session = onnxSessionFactory.CreateSession(options.SegmentationModelPath, OnnxRuntimeStageKind.Preprocess);

    /// <summary>
    /// Zeayii 判断语音段是否存在重叠说话。
    /// </summary>
    /// <param name="segment">Zeayii 语音段。</param>
    /// <param name="sampleRate">Zeayii 采样率。</param>
    /// <returns>Zeayii 是否重叠。</returns>
    public bool HasOverlap(AudioSegment segment, int sampleRate)
    {
        var audioSpan = segment.GetAudioSpan();
        var tensor = new DenseTensor<float>(new[] { 1, 1, audioSpan.Length });
        for (var i = 0; i < audioSpan.Length; i++)
        {
            tensor[0, 0, i] = audioSpan[i];
        }

        using var results = _session.Run([NamedOnnxValue.CreateFromTensor("input_values", tensor)]);
        var logits = results.First(x => x.Name == "logits").AsTensor<float>().ToDenseTensor();

        var frames = logits.Dimensions[1];
        var classes = logits.Dimensions[2];
        if (classes <= 1)
        {
            return false;
        }

        var overlapClass = Math.Min(6, classes - 1);
        var onset = options.Overlap.Onset;
        var offset = options.Overlap.Offset;
        var segmentSeconds = Math.Max((float)audioSpan.Length / sampleRate, 0.001f);
        var minOnFrames = Math.Max(1, (int)Math.Ceiling(options.Overlap.MinDurationOnSeconds * frames / segmentSeconds));
        var minOffFrames = Math.Max(1, (int)Math.Ceiling(options.Overlap.MinDurationOffSeconds * frames / segmentSeconds));

        var inOverlap = false;
        var overlapCount = 0;
        var silenceCount = 0;
        for (var f = 0; f < frames; f++)
        {
            var score = Sigmoid(logits[0, f, overlapClass]);
            if (!inOverlap)
            {
                if (score >= onset)
                {
                    overlapCount++;
                    if (overlapCount >= minOnFrames)
                    {
                        inOverlap = true;
                        silenceCount = 0;
                    }
                }
                else
                {
                    overlapCount = 0;
                }
                continue;
            }

            if (score < offset)
            {
                silenceCount++;
                if (silenceCount >= minOffFrames)
                {
                    return true;
                }
            }
            else
            {
                silenceCount = 0;
            }
        }

        return inOverlap;
    }

    /// <summary>
    /// Zeayii Sigmoid 激活函数。
    /// </summary>
    /// <param name="value">Zeayii 输入值。</param>
    /// <returns>Zeayii 概率值。</returns>
    private static float Sigmoid(float value) => 1f / (1f + MathF.Exp(-value));

    /// <summary>
    /// Zeayii 释放 ONNX 会话资源。
    /// </summary>
    public void Dispose() => _session.Dispose();
}
