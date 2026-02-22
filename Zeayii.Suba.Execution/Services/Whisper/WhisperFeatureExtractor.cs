using System.Buffers;

namespace Zeayii.Suba.Core.Services.Whisper;

/// <summary>
/// Zeayii Whisper 特征提取器，负责生成 log-mel 输入特征。
/// </summary>
internal static class WhisperFeatureExtractor
{
    /// <summary>
    /// Zeayii 目标采样率。
    /// </summary>
    private const int SampleRate = 16000;

    /// <summary>
    /// Zeayii FFT 窗口长度。
    /// </summary>
    private const int NFft = 400;

    /// <summary>
    /// Zeayii 帧移长度。
    /// </summary>
    private const int HopLength = 160;

    /// <summary>
    /// Zeayii Mel 频带数量。
    /// </summary>
    private const int MelBins = 128;

    /// <summary>
    /// Zeayii 固定特征帧数。
    /// </summary>
    private const int FeatureFrames = 3000;

    /// <summary>
    /// Zeayii 输入样本上限。
    /// </summary>
    private const int NSamples = 480000;

    /// <summary>
    /// Zeayii 频谱频点数量。
    /// </summary>
    private const int SpectrogramBins = NFft / 2 + 1;

    /// <summary>
    /// Zeayii Hann 窗函数。
    /// </summary>
    private static readonly float[] HannWindow = CreateHannWindow(NFft);

    /// <summary>
    /// Zeayii Mel 滤波器组。
    /// </summary>
    private static readonly float[,] MelFilters = CreateMelFilterBank(MelBins, SpectrogramBins, SampleRate, 0f, 8000f);

    /// <summary>
    /// Zeayii 余弦查表。
    /// </summary>
    private static readonly float[,] CosTable = CreateCosTable();

    /// <summary>
    /// Zeayii 正弦查表。
    /// </summary>
    private static readonly float[,] SinTable = CreateSinTable();

    /// <summary>
    /// Zeayii 特征长度。
    /// </summary>
    public const int OutputLength = MelBins * FeatureFrames;

    /// <summary>
    /// Zeayii 从原始音频提取 Whisper 模型输入特征。
    /// </summary>
    /// <param name="audio">Zeayii 单声道音频采样。</param>
    /// <param name="output">Zeayii 输出特征向量缓冲区。</param>
    public static void Extract(ReadOnlySpan<float> audio, Span<float> output)
    {
        if (output.Length < OutputLength)
        {
            throw new ArgumentException($"Output buffer must be at least {OutputLength}.", nameof(output));
        }

        var pool = ArrayPool<float>.Shared;
        var padded = pool.Rent(NSamples);
        var spectrogram = pool.Rent(SpectrogramBins * FeatureFrames);
        var mel = pool.Rent(MelBins * FeatureFrames);
        var frame = pool.Rent(NFft);

        try
        {
            Array.Clear(padded, 0, NSamples);
            var copyLength = Math.Min(audio.Length, NSamples);
            audio[..copyLength].CopyTo(padded);
            Array.Clear(spectrogram, 0, SpectrogramBins * FeatureFrames);
            Array.Clear(mel, 0, MelBins * FeatureFrames);

            var frameCount = Math.Clamp((copyLength - NFft) / HopLength + 1, 1, FeatureFrames);
            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                var start = frameIndex * HopLength;
                for (var i = 0; i < NFft; i++)
                {
                    var sourceIndex = start + i;
                    frame[i] = sourceIndex < copyLength ? padded[sourceIndex] * HannWindow[i] : 0f;
                }

                FillPowerSpectrum(frame, spectrogram, frameIndex);
            }

            var max = float.NegativeInfinity;
            for (var m = 0; m < MelBins; m++)
            {
                for (var t = 0; t < FeatureFrames; t++)
                {
                    var value = 0f;
                    for (var k = 0; k < SpectrogramBins; k++)
                    {
                        value += MelFilters[m, k] * spectrogram[k * FeatureFrames + t];
                    }

                    value = MathF.Log10(MathF.Max(value, 1e-10f));
                    mel[m * FeatureFrames + t] = value;
                    if (value > max)
                    {
                        max = value;
                    }
                }
            }

            var floor = max - 8f;
            for (var m = 0; m < MelBins; m++)
            {
                for (var t = 0; t < FeatureFrames; t++)
                {
                    var value = MathF.Max(mel[m * FeatureFrames + t], floor);
                    output[m * FeatureFrames + t] = (value + 4f) / 4f;
                }
            }
        }
        finally
        {
            pool.Return(frame);
            pool.Return(mel);
            pool.Return(spectrogram);
            pool.Return(padded);
        }
    }

    /// <summary>
    /// Zeayii 填充指定帧功率谱。
    /// </summary>
    /// <param name="input">Zeayii 输入帧。</param>
    /// <param name="target">Zeayii 功率谱矩阵。</param>
    /// <param name="frameIndex">Zeayii 帧索引。</param>
    private static void FillPowerSpectrum(float[] input, float[] target, int frameIndex)
    {
        for (var k = 0; k < SpectrogramBins; k++)
        {
            var real = 0f;
            var imag = 0f;
            for (var t = 0; t < NFft; t++)
            {
                var sample = input[t];
                real += sample * CosTable[k, t];
                imag += sample * SinTable[k, t];
            }

            target[k * FeatureFrames + frameIndex] = real * real + imag * imag;
        }
    }

    /// <summary>
    /// Zeayii 构造余弦查表。
    /// </summary>
    /// <returns>Zeayii 余弦矩阵。</returns>
    private static float[,] CreateCosTable()
    {
        var table = new float[SpectrogramBins, NFft];
        for (var k = 0; k < SpectrogramBins; k++)
        {
            for (var t = 0; t < NFft; t++)
            {
                var angle = 2f * MathF.PI * t * k / NFft;
                table[k, t] = MathF.Cos(angle);
            }
        }

        return table;
    }

    /// <summary>
    /// Zeayii 构造正弦查表。
    /// </summary>
    /// <returns>Zeayii 正弦矩阵。</returns>
    private static float[,] CreateSinTable()
    {
        var table = new float[SpectrogramBins, NFft];
        for (var k = 0; k < SpectrogramBins; k++)
        {
            for (var t = 0; t < NFft; t++)
            {
                var angle = -2f * MathF.PI * t * k / NFft;
                table[k, t] = MathF.Sin(angle);
            }
        }

        return table;
    }

    /// <summary>
    /// Zeayii 构造 Hann 窗。
    /// </summary>
    /// <param name="length">Zeayii 窗口长度。</param>
    /// <returns>Zeayii Hann 窗数组。</returns>
    private static float[] CreateHannWindow(int length)
    {
        var window = new float[length];
        for (var i = 0; i < length; i++)
        {
            window[i] = 0.5f * (1f - MathF.Cos(2f * MathF.PI * i / (length - 1)));
        }

        return window;
    }

    /// <summary>
    /// Zeayii 构造 Mel 滤波器组。
    /// </summary>
    /// <param name="melBins">Zeayii Mel 频带数。</param>
    /// <param name="fftBins">Zeayii FFT 频点数。</param>
    /// <param name="sampleRate">Zeayii 采样率。</param>
    /// <param name="fMin">Zeayii 最小频率。</param>
    /// <param name="fMax">Zeayii 最大频率。</param>
    /// <returns>Zeayii Mel 滤波器矩阵。</returns>
    private static float[,] CreateMelFilterBank(int melBins, int fftBins, int sampleRate, float fMin, float fMax)
    {
        var filters = new float[melBins, fftBins];
        var melMin = HzToMel(fMin);
        var melMax = HzToMel(fMax);

        var melPoints = new float[melBins + 2];
        for (var i = 0; i < melPoints.Length; i++)
        {
            melPoints[i] = melMin + (melMax - melMin) * i / (melBins + 1);
        }

        var hzPoints = new float[melPoints.Length];
        for (var i = 0; i < melPoints.Length; i++)
        {
            hzPoints[i] = MelToHz(melPoints[i]);
        }

        var bins = new int[hzPoints.Length];
        for (var i = 0; i < hzPoints.Length; i++)
        {
            bins[i] = (int)MathF.Floor((NFft + 1) * hzPoints[i] / sampleRate);
        }

        for (var m = 1; m <= melBins; m++)
        {
            var left = bins[m - 1];
            var center = bins[m];
            var right = bins[m + 1];

            for (var k = left; k < center && k < fftBins; k++)
            {
                filters[m - 1, k] = (k - left) / (float)Math.Max(center - left, 1);
            }

            for (var k = center; k < right && k < fftBins; k++)
            {
                filters[m - 1, k] = (right - k) / (float)Math.Max(right - center, 1);
            }
        }

        return filters;
    }

    /// <summary>
    /// Zeayii 将 Hz 频率转换为 Mel 频率。
    /// </summary>
    /// <param name="hz">Zeayii Hz 频率。</param>
    /// <returns>Zeayii Mel 频率。</returns>
    private static float HzToMel(float hz) => 2595f * MathF.Log10(1f + hz / 700f);

    /// <summary>
    /// Zeayii 将 Mel 频率转换为 Hz 频率。
    /// </summary>
    /// <param name="mel">Zeayii Mel 频率。</param>
    /// <returns>Zeayii Hz 频率。</returns>
    private static float MelToHz(float mel) => 700f * (MathF.Pow(10f, mel / 2595f) - 1f);
}
