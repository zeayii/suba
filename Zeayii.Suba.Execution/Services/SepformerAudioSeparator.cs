using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Zeayii.Suba.Core.Abstractions;
using Zeayii.Suba.Core.Configuration.Options;
using Zeayii.Suba.Core.Orchestration;

namespace Zeayii.Suba.Core.Services;

/// <summary>
/// Zeayii 基于 SepFormer 的双人语音分离器。
/// </summary>
/// <param name="options">Zeayii 运行配置。</param>
/// <param name="onnxSessionFactory">Zeayii ONNX 会话工厂。</param>
internal sealed class SepformerAudioSeparator(SubaOptions options, OnnxSessionFactory onnxSessionFactory) : IAudioSeparator, IDisposable
{
    /// <summary>
    /// Zeayii ONNX 推理会话。
    /// </summary>
    private readonly InferenceSession _session = onnxSessionFactory.CreateSession(options.SepformerModelPath, OnnxRuntimeStageKind.Preprocess);


    /// <summary>
    /// Zeayii 对输入语音段执行分离。
    /// </summary>
    /// <param name="segment">Zeayii 语音段。</param>
    /// <returns>Zeayii 双路分离音频。</returns>
    public IReadOnlyList<float[]> Separate(AudioSegment segment)
    {
        var audioSpan = segment.GetAudioSpan();
        var input = new DenseTensor<float>(new[] { 1, audioSpan.Length });
        for (var i = 0; i < audioSpan.Length; i++)
        {
            input[0, i] = audioSpan[i];
        }

        using var results = _session.Run([NamedOnnxValue.CreateFromTensor("input", input)]);
        var output = results.First().AsTensor<float>().ToDenseTensor();

        if (output.Dimensions.Length != 3)
        {
            throw new InvalidDataException($"Unexpected sepformer output shape: {string.Join(",", output.Dimensions.ToArray())}");
        }

        var dim0 = output.Dimensions[0];
        var dim1 = output.Dimensions[1];
        var dim2 = output.Dimensions[2];

        if (dim0 != 1)
        {
            throw new InvalidDataException("Sepformer batch must be 1.");
        }

        if (dim2 == 2)
        {
            var speakerA = new float[dim1];
            var speakerB = new float[dim1];
            for (var t = 0; t < dim1; t++)
            {
                speakerA[t] = output[0, t, 0];
                speakerB[t] = output[0, t, 1];
            }

            if (options.Sepformer.NormalizeOutput)
            {
                Normalize(speakerA, speakerB);
            }
            return [speakerA, speakerB];
        }

        if (dim1 != 2)
        {
            throw new InvalidDataException($"Cannot infer sepformer source axis: {string.Join(",", output.Dimensions.ToArray())}");
        }

        {
            var speakerA = new float[dim2];
            var speakerB = new float[dim2];
            for (var t = 0; t < dim2; t++)
            {
                speakerA[t] = output[0, 0, t];
                speakerB[t] = output[0, 1, t];
            }

            if (options.Sepformer.NormalizeOutput)
            {
                Normalize(speakerA, speakerB);
            }
            return [speakerA, speakerB];
        }
    }

    /// <summary>
    /// Zeayii 对双路音频执行峰值归一化。
    /// </summary>
    /// <param name="a">Zeayii 说话人 A 音频。</param>
    /// <param name="b">Zeayii 说话人 B 音频。</param>
    private static void Normalize(float[] a, float[] b)
    {
        var peak = Math.Max(MaxAbs(a), MaxAbs(b));
        if (peak <= 1f)
        {
            return;
        }

        for (var i = 0; i < a.Length; i++)
        {
            a[i] /= peak;
        }

        for (var i = 0; i < b.Length; i++)
        {
            b[i] /= peak;
        }
    }

    /// <summary>
    /// Zeayii 计算数组绝对值峰值。
    /// </summary>
    /// <param name="values">Zeayii 输入音频数组。</param>
    /// <returns>Zeayii 峰值。</returns>
    private static float MaxAbs(float[] values)
    {
        var max = 0f;
        for (var i = 0; i < values.Length; i++)
        {
            var abs = MathF.Abs(values[i]);
            if (abs > max)
            {
                max = abs;
            }
        }

        return max;
    }

    /// <summary>
    /// Zeayii 释放 ONNX 会话资源。
    /// </summary>
    public void Dispose() => _session.Dispose();
}
