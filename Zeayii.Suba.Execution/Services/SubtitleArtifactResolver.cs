using System.Globalization;
using Zeayii.Suba.Core.Configuration.Policies;
using Zeayii.Suba.Core.Orchestration;

namespace Zeayii.Suba.Core.Services;

/// <summary>
/// Zeayii subtitle artifact path and loading resolver.
/// </summary>
internal sealed class SubtitleArtifactResolver
{
    /// <summary>
    /// Zeayii build subtitle artifact path from input media path.
    /// </summary>
    /// <param name="inputPath">Zeayii input media path.</param>
    /// <param name="language">Zeayii subtitle language tag.</param>
    /// <param name="formatPolicy">Zeayii subtitle format policy.</param>
    /// <returns>Zeayii subtitle file path.</returns>
    public string BuildOutputPath(string inputPath, string language, SubtitleFormatPolicy formatPolicy)
    {
        var directory = Path.GetDirectoryName(inputPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = Environment.CurrentDirectory;
        }

        var fileName = Path.GetFileNameWithoutExtension(inputPath);
        var extension = formatPolicy == SubtitleFormatPolicy.Srt ? "srt" : "vtt";
        var normalizedLanguage = NormalizeLanguageTag(language);
        return Path.Combine(directory, $"{fileName}.{normalizedLanguage}.{extension}");
    }

    /// <summary>
    /// Zeayii build partial subtitle artifact path from input media path.
    /// </summary>
    /// <param name="inputPath">Zeayii input media path.</param>
    /// <param name="language">Zeayii subtitle language tag.</param>
    /// <param name="formatPolicy">Zeayii subtitle format policy.</param>
    /// <returns>Zeayii partial subtitle file path.</returns>
    public string BuildPartialOutputPath(string inputPath, string language, SubtitleFormatPolicy formatPolicy)
    {
        var finalPath = BuildOutputPath(inputPath, language, formatPolicy);
        var directory = Path.GetDirectoryName(finalPath) ?? Environment.CurrentDirectory;
        var fileName = Path.GetFileNameWithoutExtension(finalPath);
        var extension = Path.GetExtension(finalPath);
        return Path.Combine(directory, $"{fileName}.partial{extension}");
    }

    /// <summary>
    /// Zeayii delete partial subtitle artifact if exists.
    /// </summary>
    /// <param name="inputPath">Zeayii input media path.</param>
    /// <param name="language">Zeayii subtitle language tag.</param>
    /// <param name="formatPolicy">Zeayii subtitle format policy.</param>
    public void DeletePartialIfExists(string inputPath, string language, SubtitleFormatPolicy formatPolicy)
    {
        var partialPath = BuildPartialOutputPath(inputPath, language, formatPolicy);
        if (File.Exists(partialPath))
        {
            File.Delete(partialPath);
        }
    }

    /// <summary>
    /// Zeayii check whether subtitle artifact already exists.
    /// </summary>
    /// <param name="inputPath">Zeayii input media path.</param>
    /// <param name="language">Zeayii subtitle language tag.</param>
    /// <param name="formatPolicy">Zeayii subtitle format policy.</param>
    /// <returns>Zeayii existence flag.</returns>
    public bool Exists(string inputPath, string language, SubtitleFormatPolicy formatPolicy)
    {
        var path = BuildOutputPath(inputPath, language, formatPolicy);
        return File.Exists(path);
    }

    /// <summary>
    /// Zeayii load source subtitle artifact as segments.
    /// </summary>
    /// <param name="inputPath">Zeayii input media path.</param>
    /// <param name="language">Zeayii subtitle language tag.</param>
    /// <param name="formatPolicy">Zeayii subtitle format policy.</param>
    /// <param name="cancellationToken">Zeayii cancellation token.</param>
    /// <returns>Zeayii loaded subtitle segments.</returns>
    public async Task<IReadOnlyList<SubtitleSegment>> ReadAsync(string inputPath, string language, SubtitleFormatPolicy formatPolicy, CancellationToken cancellationToken)
    {
        var path = BuildOutputPath(inputPath, language, formatPolicy);
        if (!File.Exists(path))
        {
            return [];
        }

        var content = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        return formatPolicy == SubtitleFormatPolicy.Srt ? ParseSrt(content) : ParseVtt(content);
    }

    /// <summary>
    /// Zeayii parse SRT subtitle text to segments.
    /// </summary>
    /// <param name="content">Zeayii SRT content.</param>
    /// <returns>Zeayii parsed segments.</returns>
    private static IReadOnlyList<SubtitleSegment> ParseSrt(string content)
    {
        var blocks = content.Replace("\r\n", "\n", StringComparison.Ordinal).Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var segments = new List<SubtitleSegment>(blocks.Length);
        foreach (var block in blocks)
        {
            var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 3)
            {
                continue;
            }

            var timelineLine = lines[1].Trim();
            var times = timelineLine.Split(" --> ", StringSplitOptions.TrimEntries);
            if (times.Length != 2)
            {
                continue;
            }

            if (!TryParseSrtTime(times[0], out var startMs) || !TryParseSrtTime(times[1], out var endMs))
            {
                continue;
            }

            var text = string.Join('\n', lines.Skip(2)).Trim();
            var (speaker, value) = ParseSpeakerPrefix(text);
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            segments.Add(new SubtitleSegment
            {
                Index = segments.Count + 1,
                StartMs = startMs,
                EndMs = endMs,
                Speaker = speaker,
                OriginalText = value
            });
        }

        return segments;
    }

    /// <summary>
    /// Zeayii parse VTT subtitle text to segments.
    /// </summary>
    /// <param name="content">Zeayii VTT content.</param>
    /// <returns>Zeayii parsed segments.</returns>
    private static IReadOnlyList<SubtitleSegment> ParseVtt(string content)
    {
        var lines = content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var segments = new List<SubtitleSegment>();
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("WEBVTT", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!line.Contains("-->", StringComparison.Ordinal))
            {
                continue;
            }

            var times = line.Split(" --> ", StringSplitOptions.TrimEntries);
            if (times.Length != 2)
            {
                continue;
            }

            if (!TryParseVttTime(times[0], out var startMs) || !TryParseVttTime(times[1], out var endMs))
            {
                continue;
            }

            var textLines = new List<string>();
            for (var j = i + 1; j < lines.Length; j++)
            {
                var textLine = lines[j];
                if (string.IsNullOrWhiteSpace(textLine))
                {
                    i = j;
                    break;
                }

                textLines.Add(textLine.TrimEnd());
                if (j == lines.Length - 1)
                {
                    i = j;
                }
            }

            var text = string.Join('\n', textLines).Trim();
            var (speaker, value) = ParseSpeakerPrefix(text);
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            segments.Add(new SubtitleSegment
            {
                Index = segments.Count + 1,
                StartMs = startMs,
                EndMs = endMs,
                Speaker = speaker,
                OriginalText = value
            });
        }

        return segments;
    }

    /// <summary>
    /// Zeayii parse optional speaker prefix in subtitle text.
    /// </summary>
    /// <param name="text">Zeayii subtitle text.</param>
    /// <returns>Zeayii speaker id and cleaned text.</returns>
    private static (int Speaker, string Text) ParseSpeakerPrefix(string text)
    {
        var firstLine = text.Split('\n', 2)[0];
        var separatorIndex = firstLine.IndexOf(':');
        if (separatorIndex <= 0)
        {
            return (-1, text);
        }

        var speakerToken = firstLine[..separatorIndex].Trim();
        if (!int.TryParse(speakerToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out var speaker))
        {
            return (-1, text);
        }

        var firstLineValue = firstLine[(separatorIndex + 1)..].TrimStart();
        if (!text.Contains('\n'))
        {
            return (speaker, firstLineValue);
        }

        var tail = text[(firstLine.Length + 1)..];
        return (speaker, $"{firstLineValue}\n{tail}");
    }

    /// <summary>
    /// Zeayii parse SRT time string to milliseconds.
    /// </summary>
    /// <param name="value">Zeayii SRT time string.</param>
    /// <param name="milliseconds">Zeayii parsed milliseconds.</param>
    /// <returns>Zeayii parse success flag.</returns>
    private static bool TryParseSrtTime(string value, out int milliseconds)
    {
        if (!TimeSpan.TryParseExact(value, @"hh\:mm\:ss\,fff", CultureInfo.InvariantCulture, out var time))
        {
            milliseconds = 0;
            return false;
        }

        milliseconds = (int)time.TotalMilliseconds;
        return true;
    }

    /// <summary>
    /// Zeayii parse VTT time string to milliseconds.
    /// </summary>
    /// <param name="value">Zeayii VTT time string.</param>
    /// <param name="milliseconds">Zeayii parsed milliseconds.</param>
    /// <returns>Zeayii parse success flag.</returns>
    private static bool TryParseVttTime(string value, out int milliseconds)
    {
        if (!TimeSpan.TryParseExact(value, @"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture, out var time))
        {
            milliseconds = 0;
            return false;
        }

        milliseconds = (int)time.TotalMilliseconds;
        return true;
    }

    /// <summary>
    /// Zeayii normalize language tag for subtitle file names.
    /// </summary>
    /// <param name="language">Zeayii original language tag.</param>
    /// <returns>Zeayii normalized language tag.</returns>
    private static string NormalizeLanguageTag(string language) => string.IsNullOrWhiteSpace(language) ? "und" : language;
}
