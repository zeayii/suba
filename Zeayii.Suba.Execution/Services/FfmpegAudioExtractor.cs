using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Zeayii.Suba.Core.Abstractions;
using Zeayii.Suba.Core.Configuration.Options;
using Zeayii.Suba.Core.Contexts;
using Zeayii.Suba.Core.Orchestration;

namespace Zeayii.Suba.Core.Services;

/// <summary>
/// Zeayii 基于 FFmpeg 的音频提取服务。
/// </summary>
/// <param name="options">Zeayii 核心配置。</param>
/// <param name="globalContext">Zeayii 全局运行上下文。</param>
/// <param name="wavProbe">Zeayii WAV 探测器。</param>
/// <param name="logger">Zeayii 日志器。</param>
internal sealed class FfmpegAudioExtractor(SubaOptions options, GlobalContext globalContext, WavProbe wavProbe, ILogger<FfmpegAudioExtractor> logger) : IAudioExtractor
{
    /// <summary>
    /// Zeayii 音频提取开始日志委托。
    /// </summary>
    private static readonly Action<ILogger, string, Exception?> ExtractStartLogAction =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1001, nameof(ExtractMonoWavAsync)),
            "Extract wav start: {InputPath}");

    /// <summary>
    /// Zeayii 音频提取完成日志委托。
    /// </summary>
    private static readonly Action<ILogger, string, Exception?> ExtractDoneLogAction =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1002, nameof(ExtractMonoWavAsync)),
            "Extract wav done: {OutputPath}");

    /// <summary>
    /// Zeayii 音频提取旁路日志委托。
    /// </summary>
    private static readonly Action<ILogger, string, Exception?> ExtractBypassLogAction =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1003, nameof(ExtractMonoWavAsync)),
            "Extract wav bypassed: {InputPath}");

    /// <summary>
    /// Zeayii 运行配置。
    /// </summary>
    private readonly SubaOptions _options = options;

    /// <summary>
    /// Zeayii 全局运行上下文。
    /// </summary>
    private readonly GlobalContext _globalContext = globalContext;

    /// <summary>
    /// Zeayii WAV 探测器。
    /// </summary>
    private readonly WavProbe _wavProbe = wavProbe;

    /// <summary>
    /// Zeayii 日志器。
    /// </summary>
    private readonly ILogger<FfmpegAudioExtractor> _logger = logger;

    /// <summary>
    /// Zeayii 提取输入媒体为单声道 WAV。
    /// </summary>
    /// <param name="inputPath">Zeayii 输入媒体路径。</param>
    /// <param name="cancellationToken">Zeayii 取消令牌。</param>
    /// <returns>Zeayii 音频准备结果。</returns>
    public async Task<PreparedAudio> ExtractMonoWavAsync(string inputPath, CancellationToken cancellationToken)
    {
        var probeResult = _wavProbe.Probe(inputPath);
        if (probeResult.CanBypassExtraction)
        {
            ExtractBypassLogAction(_logger, inputPath, null);
            return new PreparedAudio
            {
                OutputPath = inputPath,
                IsExtractionBypassed = true
            };
        }

        var outputDirectory = _options.AudioExtract.CacheDirectory;
        Directory.CreateDirectory(outputDirectory);
        var outputPath = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(inputPath)}.wav");
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        var args = new[]
        {
            "-nostdin", "-y", "-hide_banner",
            "-i", inputPath,
            "-f", "wav",
            "-ar", _globalContext.TargetSampleRateHz.ToString(),
            "-ac", "1",
            "-acodec", "pcm_s16le",
            "-sample_fmt", "s16",
            "-map_metadata", "-1",
            "-loglevel", "quiet",
            outputPath
        };

        var startInfo = new ProcessStartInfo
        {
            FileName = _options.AudioExtract.FfmpegPath,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        foreach (var argument in args)
        {
            startInfo.ArgumentList.Add(argument);
        }

        ExtractStartLogAction(_logger, inputPath, null);
        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start ffmpeg process.");
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidOperationException($"ffmpeg failed: {stderr}");
        }

        ExtractDoneLogAction(_logger, outputPath, null);
        return new PreparedAudio
        {
            OutputPath = outputPath,
            IsExtractionBypassed = false
        };
    }
}
