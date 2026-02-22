using Zeayii.Suba.Core.Contexts;
using Zeayii.Suba.Core.Services;
using Zeayii.Suba.Execution.Tests.TestSupport;

namespace Zeayii.Suba.Execution.Tests;

/// <summary>
/// Zeayii WAV 探测测试。
/// </summary>
public sealed class WavProbeTests
{
    /// <summary>
    /// Zeayii 验证 16k/mono/pcm16 可旁路提取。
    /// </summary>
    [Fact]
    public void Probe_ShouldBypassForMono16kPcm16()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wav");
        TestWaveFileBuilder.WriteMonoPcm16(path, GlobalContext.DefaultTargetSampleRateHz, [0, 3276, -3276, 1000]);

        try
        {
            var probe = new WavProbe();
            var result = probe.Probe(path);
            Assert.True(result.IsWav);
            Assert.Equal(1, result.Channels);
            Assert.Equal(GlobalContext.DefaultTargetSampleRateHz, result.SampleRate);
            Assert.True(result.CanBypassExtraction);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Zeayii 验证非 16k 采样率不可旁路提取。
    /// </summary>
    [Fact]
    public void Probe_ShouldNotBypassFor8kPcm16()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.wav");
        TestWaveFileBuilder.WriteMonoPcm16(path, 8000, [0, 3276, -3276, 1000]);

        try
        {
            var probe = new WavProbe();
            var result = probe.Probe(path);
            Assert.True(result.IsWav);
            Assert.False(result.CanBypassExtraction);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
