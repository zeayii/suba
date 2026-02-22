using System.Buffers;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Zeayii.Suba.Core.Abstractions;
using Zeayii.Suba.Core.Configuration.Options;
using Zeayii.Suba.Core.Configuration.Policies;
using Zeayii.Suba.Core.Orchestration;
using Zeayii.Suba.Core.Services.Whisper;

namespace Zeayii.Suba.Core.Services;

/// <summary>
/// Zeayii 基于 Whisper ONNX 的转写服务实现。
/// </summary>
internal sealed class WhisperOnnxTranscriptionService : ITranscriptionService, IDisposable
{
    /// <summary>
    /// Zeayii 转写完成日志委托。
    /// </summary>
    private static readonly Action<ILogger, int, Exception?> TranscribeDoneLogAction =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(1201, nameof(TranscribeAsync)), "Whisper ONNX transcribe done. Segments={Count}");

    /// <summary>
    /// Zeayii Decoder 层数。
    /// </summary>
    private const int NumLayers = 2;

    /// <summary>
    /// Zeayii Whisper 特征长度。
    /// </summary>
    private const int FeatureLength = WhisperFeatureExtractor.OutputLength;

    /// <summary>
    /// Zeayii 编码器会话。
    /// </summary>
    private readonly InferenceSession _encoderSession;

    /// <summary>
    /// Zeayii 首轮解码会话。
    /// </summary>
    private readonly InferenceSession _decoderSession;

    /// <summary>
    /// Zeayii 带缓存增量解码会话。
    /// </summary>
    private readonly InferenceSession _decoderWithPastSession;

    /// <summary>
    /// Zeayii Whisper 资产。
    /// </summary>
    private readonly WhisperOnnxAssets _assets;

    /// <summary>
    /// Zeayii token 解码器。
    /// </summary>
    private readonly WhisperTokenDecoder _tokenDecoder;

    /// <summary>
    /// Zeayii 额外抑制 token 集合。
    /// </summary>
    private readonly HashSet<int> _customSuppressTokenIds;

    /// <summary>
    /// Zeayii 运行配置。
    /// </summary>
    private readonly SubaOptions _options;

    /// <summary>
    /// Zeayii 日志器。
    /// </summary>
    private readonly ILogger<WhisperOnnxTranscriptionService> _logger;

    /// <summary>
    /// Zeayii 构造 Whisper 转写服务。
    /// </summary>
    /// <param name="options">Zeayii 运行配置。</param>
    /// <param name="onnxSessionFactory">Zeayii ONNX 会话工厂。</param>
    /// <param name="logger">Zeayii 日志器。</param>
    public WhisperOnnxTranscriptionService(SubaOptions options, OnnxSessionFactory onnxSessionFactory, ILogger<WhisperOnnxTranscriptionService> logger)
    {
        _options = options;
        _logger = logger;

        var onnxDirectory = Path.Combine(options.WhisperModelRoot, "onnx");
        var encoderPath = Path.Combine(onnxDirectory, "encoder_model.onnx");
        var decoderPath = Path.Combine(onnxDirectory, "decoder_model.onnx");
        var decoderWithPastPath = Path.Combine(onnxDirectory, "decoder_with_past_model.onnx");

        _encoderSession = onnxSessionFactory.CreateSession(encoderPath, OnnxRuntimeStageKind.Transcribe);
        _decoderSession = onnxSessionFactory.CreateSession(decoderPath, OnnxRuntimeStageKind.Transcribe);
        _decoderWithPastSession = onnxSessionFactory.CreateSession(decoderWithPastPath, OnnxRuntimeStageKind.Transcribe);
        _assets = WhisperOnnxAssets.Load(options.WhisperModelRoot);
        _tokenDecoder = new WhisperTokenDecoder(_assets.TokenById);
        _customSuppressTokenIds = options.Transcription.SuppressTokens.Where(token => token >= 0).ToHashSet();
    }

    /// <summary>
    /// Zeayii 对音频段集合执行转写。
    /// </summary>
    /// <param name="segments">Zeayii 音频段集合。</param>
    /// <param name="sampleRate">Zeayii 采样率。</param>
    /// <param name="cancellationToken">Zeayii 取消令牌。</param>
    /// <returns>Zeayii 字幕段集合。</returns>
    public Task<IReadOnlyList<SubtitleSegment>> TranscribeAsync(IReadOnlyList<AudioSegment> segments, int sampleRate, CancellationToken cancellationToken)
    {
        var subtitles = new List<SubtitleSegment>(segments.Count);

        foreach (var segment in segments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var decoded = DecodeSegment(segment);
            if (decoded.Count == 0)
            {
                continue;
            }

            var segmentBaseMs = (int)(segment.StartSample * 1000L / sampleRate);
            var segmentEndMs = (int)(segment.EndSample * 1000L / sampleRate);
            foreach (var item in decoded)
            {
                var startMs = Math.Clamp(segmentBaseMs + (int)(item.StartSeconds * 1000f), segmentBaseMs, segmentEndMs);
                var minEnd = Math.Min(startMs + 1, segmentEndMs);
                var endMs = Math.Clamp(segmentBaseMs + (int)(item.EndSeconds * 1000f), minEnd, segmentEndMs);
                subtitles.Add(new SubtitleSegment
                {
                    Index = subtitles.Count + 1,
                    StartMs = startMs,
                    EndMs = endMs,
                    Speaker = segment.Speaker,
                    OriginalText = item.Text.Trim()
                });
            }
        }

        TranscribeDoneLogAction(_logger, subtitles.Count, null);
        return Task.FromResult<IReadOnlyList<SubtitleSegment>>(subtitles);
    }

    /// <summary>
    /// Zeayii 解码单个音频段为时间戳文本块。
    /// </summary>
    /// <param name="segment">Zeayii 音频段。</param>
    /// <returns>Zeayii 解码结果块集合。</returns>
    private IReadOnlyList<DecodedChunk> DecodeSegment(AudioSegment segment)
    {
        var featureBuffer = ArrayPool<float>.Shared.Rent(FeatureLength);
        try
        {
            WhisperFeatureExtractor.Extract(segment.GetAudioSpan(), featureBuffer.AsSpan(0, FeatureLength));
            var featureTensor = new DenseTensor<float>(featureBuffer.AsMemory(0, FeatureLength), [1, 128, 3000]);

            DenseTensor<float> encoderHidden;
            using (var encoderOutputs = _encoderSession.Run([NamedOnnxValue.CreateFromTensor("input_features", featureTensor)]))
            {
                encoderHidden = encoderOutputs[0].AsTensor<float>().ToDenseTensor();
            }

            int[] prompt;
            if (_options.Transcription.LanguagePolicy == LanguagePolicy.Fixed && !string.IsNullOrWhiteSpace(_options.Transcription.ModelLanguageCode))
            {
                var languageTokenId = ResolveLanguageTokenId(_options.Transcription.ModelLanguageCode);
                prompt =
                [
                    _assets.StartOfTranscriptTokenId,
                    languageTokenId,
                    _assets.TranscribeTaskTokenId
                ];
            }
            else
            {
                prompt =
                [
                    _assets.StartOfTranscriptTokenId,
                    _assets.TranscribeTaskTokenId
                ];
            }

            var first = DecodeFirstPass(prompt, encoderHidden);
            var logits = first.Logits;
            var pastCache = first.PastCache;

            var noSpeechProbability = CalculateTokenProbability(first.Logits, _assets.NoSpeechTokenId);
            if (noSpeechProbability >= _options.Transcription.NoSpeechThreshold)
            {
                return [];
            }

            var generated = new List<int>(_options.Transcription.MaxNewTokens);
            for (var step = 0; step < _options.Transcription.MaxNewTokens; step++)
            {
                var nextToken = SelectNextToken(logits, suppressSpecial: step == 0);
                if (nextToken == _assets.EndOfTextTokenId)
                {
                    break;
                }

                generated.Add(nextToken);
                (logits, pastCache) = DecodeNext(nextToken, pastCache);
            }

            if (_options.Transcription.WithoutTimestamps)
            {
                var text = _tokenDecoder.Decode(generated);
                return string.IsNullOrWhiteSpace(text) ? [] : [new DecodedChunk(0f, 30f, text)];
            }

            var chunks = ParseTimestampChunks(generated);
            if (chunks.Count > 0)
            {
                return chunks;
            }

            var fallbackText = _tokenDecoder.Decode(generated);
            if (string.IsNullOrWhiteSpace(fallbackText))
            {
                return [];
            }

            return [new DecodedChunk(0f, 30f, fallbackText)];
        }
        finally
        {
            ArrayPool<float>.Shared.Return(featureBuffer);
        }
    }

    /// <summary>
    /// Zeayii 执行首轮解码并初始化 KV 缓存。
    /// </summary>
    /// <param name="promptTokens">Zeayii Prompt token 序列。</param>
    /// <param name="encoderHidden">Zeayii 编码器隐藏状态。</param>
    /// <returns>Zeayii 首轮 logits 与缓存。</returns>
    private (DenseTensor<float> Logits, DecoderPastCache PastCache) DecodeFirstPass(IReadOnlyList<int> promptTokens, DenseTensor<float> encoderHidden)
    {
        var inputIds = new DenseTensor<long>(new[] { 1, promptTokens.Count });
        for (var i = 0; i < promptTokens.Count; i++)
        {
            inputIds[0, i] = promptTokens[i];
        }

        using var outputs = _decoderSession.Run([
            NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
            NamedOnnxValue.CreateFromTensor("encoder_hidden_states", encoderHidden)
        ]);
        var decoderKeys = new DenseTensor<float>[NumLayers];
        var decoderValues = new DenseTensor<float>[NumLayers];
        var encoderKeys = new DenseTensor<float>[NumLayers];
        var encoderValues = new DenseTensor<float>[NumLayers];
        DenseTensor<float>? logits = null;
        foreach (var output in outputs)
        {
            switch (output.Name)
            {
                case "logits":
                    logits = ExtractLastTokenLogits(output.AsTensor<float>());
                    break;
                case "present.0.decoder.key":
                    decoderKeys[0] = output.AsTensor<float>().ToDenseTensor();
                    break;
                case "present.0.decoder.value":
                    decoderValues[0] = output.AsTensor<float>().ToDenseTensor();
                    break;
                case "present.0.encoder.key":
                    encoderKeys[0] = output.AsTensor<float>().ToDenseTensor();
                    break;
                case "present.0.encoder.value":
                    encoderValues[0] = output.AsTensor<float>().ToDenseTensor();
                    break;
                case "present.1.decoder.key":
                    decoderKeys[1] = output.AsTensor<float>().ToDenseTensor();
                    break;
                case "present.1.decoder.value":
                    decoderValues[1] = output.AsTensor<float>().ToDenseTensor();
                    break;
                case "present.1.encoder.key":
                    encoderKeys[1] = output.AsTensor<float>().ToDenseTensor();
                    break;
                case "present.1.encoder.value":
                    encoderValues[1] = output.AsTensor<float>().ToDenseTensor();
                    break;
            }
        }

        return (logits ?? throw new InvalidOperationException("Whisper decoder outputs do not contain logits."), new DecoderPastCache(decoderKeys, decoderValues, encoderKeys, encoderValues));
    }

    /// <summary>
    /// Zeayii 执行增量解码一步。
    /// </summary>
    /// <param name="inputTokenId">Zeayii 输入 token。</param>
    /// <param name="pastCache">Zeayii 旧缓存。</param>
    /// <returns>Zeayii 新 logits 与缓存。</returns>
    private (DenseTensor<float> Logits, DecoderPastCache PastCache) DecodeNext(int inputTokenId, DecoderPastCache pastCache)
    {
        var inputIds = new DenseTensor<long>([1, 1])
        {
            [0, 0] = inputTokenId
        };

        var inputs = new List<NamedOnnxValue>(1 + NumLayers * 4)
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputIds)
        };

        for (var i = 0; i < NumLayers; i++)
        {
            inputs.Add(NamedOnnxValue.CreateFromTensor($"past_key_values.{i}.decoder.key", pastCache.DecoderKeys[i]));
            inputs.Add(NamedOnnxValue.CreateFromTensor($"past_key_values.{i}.decoder.value", pastCache.DecoderValues[i]));
            inputs.Add(NamedOnnxValue.CreateFromTensor($"past_key_values.{i}.encoder.key", pastCache.EncoderKeys[i]));
            inputs.Add(NamedOnnxValue.CreateFromTensor($"past_key_values.{i}.encoder.value", pastCache.EncoderValues[i]));
        }

        using var outputs = _decoderWithPastSession.Run(inputs);
        var newKeys = new DenseTensor<float>[NumLayers];
        var newValues = new DenseTensor<float>[NumLayers];
        DenseTensor<float>? logits = null;
        foreach (var output in outputs)
        {
            switch (output.Name)
            {
                case "logits":
                    logits = ExtractLastTokenLogits(output.AsTensor<float>());
                    break;
                case "present.0.decoder.key":
                    newKeys[0] = output.AsTensor<float>().ToDenseTensor();
                    break;
                case "present.0.decoder.value":
                    newValues[0] = output.AsTensor<float>().ToDenseTensor();
                    break;
                case "present.1.decoder.key":
                    newKeys[1] = output.AsTensor<float>().ToDenseTensor();
                    break;
                case "present.1.decoder.value":
                    newValues[1] = output.AsTensor<float>().ToDenseTensor();
                    break;
            }
        }

        return (logits ?? throw new InvalidOperationException("Whisper decoder outputs do not contain logits."), new DecoderPastCache(newKeys, newValues, pastCache.EncoderKeys, pastCache.EncoderValues));
    }

    /// <summary>
    /// Zeayii 从 logits 中选择最大概率 token，可选抑制特殊 token。
    /// </summary>
    /// <param name="logits">Zeayii 当前 logits。</param>
    /// <param name="suppressSpecial">Zeayii 是否抑制特殊 token。</param>
    /// <returns>Zeayii 选中的 token 编号。</returns>
    private int SelectNextToken(Tensor<float> logits, bool suppressSpecial)
    {
        var size = logits.Dimensions[2];

        if (_options.Transcription.Temperature <= 0f)
        {
            var best = int.MinValue;
            var bestValue = float.NegativeInfinity;
            for (var token = 0; token < size; token++)
            {
                if (IsSuppressed(token, suppressSpecial))
                {
                    continue;
                }

                var value = logits[0, 0, token];
                if (value > bestValue)
                {
                    bestValue = value;
                    best = token;
                }
            }

            return best > 0 ? best : _assets.EndOfTextTokenId;
        }

        var max = float.NegativeInfinity;
        for (var i = 0; i < size; i++)
        {
            if (IsSuppressed(i, suppressSpecial))
            {
                continue;
            }

            var value = logits[0, 0, i] / _options.Transcription.Temperature;
            if (value > max)
            {
                max = value;
            }
        }

        var probs = new float[size];
        var sum = 0f;
        for (var i = 0; i < size; i++)
        {
            if (IsSuppressed(i, suppressSpecial))
            {
                continue;
            }

            var p = MathF.Exp(logits[0, 0, i] / _options.Transcription.Temperature - max);
            probs[i] = p;
            sum += p;
        }

        if (sum <= 0f)
        {
            return _assets.EndOfTextTokenId;
        }

        var sample = Random.Shared.NextSingle() * sum;
        var cumulative = 0f;
        for (var i = 0; i < size; i++)
        {
            cumulative += probs[i];
            if (sample <= cumulative)
            {
                return i;
            }
        }

        return _assets.EndOfTextTokenId;
    }

    /// <summary>
    /// Zeayii 判断 token 是否应被抑制。
    /// </summary>
    /// <param name="token">Zeayii token 编号。</param>
    /// <param name="suppressSpecial">Zeayii 是否抑制特殊 token。</param>
    /// <returns>Zeayii 是否抑制。</returns>
    private bool IsSuppressed(int token, bool suppressSpecial)
    {
        if (suppressSpecial && _assets.SuppressTokenIds.Contains(token))
        {
            return true;
        }

        if (_customSuppressTokenIds.Contains(token))
        {
            return true;
        }

        return _options.Transcription.SuppressBlank && token == _assets.StartOfTranscriptTokenId;
    }

    /// <summary>
    /// Zeayii 提取最后一个时间步的 logits。
    /// </summary>
    /// <param name="logits">Zeayii 原始 logits。</param>
    /// <returns>Zeayii 单步 logits。</returns>
    private static DenseTensor<float> ExtractLastTokenLogits(Tensor<float> logits)
    {
        var vocab = logits.Dimensions[2];
        var extracted = new DenseTensor<float>(new[] { 1, 1, vocab });
        var timestep = Math.Max(logits.Dimensions[1] - 1, 0);
        for (var i = 0; i < vocab; i++)
        {
            extracted[0, 0, i] = logits[0, timestep, i];
        }

        return extracted;
    }

    /// <summary>
    /// Zeayii 计算指定 token 的概率。
    /// </summary>
    /// <param name="logits">Zeayii 模型 logits。</param>
    /// <param name="tokenId">Zeayii token 编号。</param>
    /// <returns>Zeayii token 概率。</returns>
    private static float CalculateTokenProbability(Tensor<float> logits, int tokenId)
    {
        if (tokenId < 0 || tokenId >= logits.Dimensions[2])
        {
            return 0f;
        }

        var max = float.NegativeInfinity;
        for (var i = 0; i < logits.Dimensions[2]; i++)
        {
            var value = logits[0, 0, i];
            if (value > max)
            {
                max = value;
            }
        }

        var denom = 0f;
        for (var i = 0; i < logits.Dimensions[2]; i++)
        {
            denom += MathF.Exp(logits[0, 0, i] - max);
        }

        if (denom <= 0f)
        {
            return 0f;
        }

        return MathF.Exp(logits[0, 0, tokenId] - max) / denom;
    }

    /// <summary>
    /// Zeayii 解析语言标识对应的 token。
    /// </summary>
    /// <param name="language">Zeayii 语言短码。</param>
    /// <returns>Zeayii 语言 token 编号。</returns>
    private int ResolveLanguageTokenId(string language)
    {
        if (_assets.LanguageTokenIds.TryGetValue(language, out var tokenId))
        {
            return tokenId;
        }

        if (_assets.LanguageTokenIds.TryGetValue(language.ToLowerInvariant(), out tokenId))
        {
            return tokenId;
        }

        return _assets.StartOfTranscriptTokenId;
    }

    /// <summary>
    /// Zeayii 释放 ONNX 会话资源。
    /// </summary>
    public void Dispose()
    {
        _encoderSession.Dispose();
        _decoderSession.Dispose();
        _decoderWithPastSession.Dispose();
    }

    /// <summary>
    /// Zeayii Decoder 历史缓存。
    /// </summary>
    /// <param name="DecoderKeys">Zeayii Decoder Key 缓存。</param>
    /// <param name="DecoderValues">Zeayii Decoder Value 缓存。</param>
    /// <param name="EncoderKeys">Zeayii Encoder Key 缓存。</param>
    /// <param name="EncoderValues">Zeayii Encoder Value 缓存。</param>
    private sealed record DecoderPastCache(DenseTensor<float>[] DecoderKeys, DenseTensor<float>[] DecoderValues, DenseTensor<float>[] EncoderKeys, DenseTensor<float>[] EncoderValues);

    /// <summary>
    /// Zeayii 从 token 序列解析时间戳块。
    /// </summary>
    /// <param name="tokenIds">Zeayii token 序列。</param>
    /// <returns>Zeayii 时间戳文本块集合。</returns>
    private List<DecodedChunk> ParseTimestampChunks(IReadOnlyList<int> tokenIds)
    {
        var chunks = new List<DecodedChunk>();
        var currentStartSeconds = -1f;
        var textTokens = new List<int>();

        foreach (var tokenId in tokenIds)
        {
            if (!_assets.TimestampById.TryGetValue(tokenId, out var timestampSeconds))
            {
                textTokens.Add(tokenId);
                continue;
            }

            if (currentStartSeconds < 0f)
            {
                currentStartSeconds = timestampSeconds;
                textTokens.Clear();
                continue;
            }

            var text = _tokenDecoder.Decode(textTokens);
            if (!string.IsNullOrWhiteSpace(text) && timestampSeconds > currentStartSeconds)
            {
                chunks.Add(new DecodedChunk(currentStartSeconds, timestampSeconds, text));
            }

            currentStartSeconds = timestampSeconds;
            textTokens.Clear();
        }

        return chunks;
    }

    /// <summary>
    /// Zeayii 解码后的时间戳文本块。
    /// </summary>
    /// <param name="StartSeconds">Zeayii 片段起始秒。</param>
    /// <param name="EndSeconds">Zeayii 片段结束秒。</param>
    /// <param name="Text">Zeayii 文本内容。</param>
    private sealed record DecodedChunk(float StartSeconds, float EndSeconds, string Text);
}
