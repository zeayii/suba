using Zeayii.Suba.Core.Configuration.Policies;
using Zeayii.Suba.Core.Contexts;
using Zeayii.Suba.Core.Orchestration;
using Zeayii.Suba.Core.Services;
using Zeayii.Suba.Execution.Tests.TestSupport;

namespace Zeayii.Suba.Execution.Tests;

/// <summary>
/// Zeayii 字幕写入时间戳格式测试。
/// </summary>
public sealed class SubtitleWriterTests
{
    /// <summary>
    /// Zeayii 验证 VTT 时间戳格式与说话人前缀。
    /// </summary>
    [Fact]
    public async Task WriteAsync_ShouldOutputExpectedVttTimestamp()
    {
        var workDirectory = Path.Combine(Path.GetTempPath(), $"suba-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workDirectory);
        var mediaPath = Path.Combine(workDirectory, "clip.wav");
        await File.WriteAllTextAsync(mediaPath, "placeholder");
        var context = CreateTaskContext(mediaPath);
        context.SubtitleSegments.Add(new SubtitleSegment
        {
            Index = 1,
            StartMs = 672,
            EndMs = 1984,
            Speaker = 0,
            OriginalText = "おめでとう",
            TranslatedText = "恭喜"
        });

        try
        {
            var options = TestSubaOptionsFactory.Create();
            var writer = new SubtitleWriter(options, new SubtitleArtifactResolver());
            await writer.WriteAsync(context, "ja", SubtitleFormatPolicy.Vtt, translated: false, partial: false, CancellationToken.None);
            var outputPath = Path.Combine(workDirectory, "clip.ja.vtt");
            var content = await File.ReadAllTextAsync(outputPath);

            Assert.Contains("00:00:00.672 --> 00:00:01.984", content);
            Assert.Contains("0: おめでとう", content);
        }
        finally
        {
            Directory.Delete(workDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Zeayii 创建测试任务上下文。
    /// </summary>
    /// <param name="inputPath">Zeayii 输入路径。</param>
    /// <returns>Zeayii 任务上下文。</returns>
    private static TaskContext CreateTaskContext(string inputPath)
    {
        var options = TestSubaOptionsFactory.Create();
        var global = new GlobalContext(options);
        return new TaskContext(global, inputPath, string.Empty, string.Empty);
    }
}
