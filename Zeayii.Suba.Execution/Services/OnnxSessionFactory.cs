using Microsoft.ML.OnnxRuntime;
using Zeayii.Suba.Core.Configuration.Options;
using Zeayii.Suba.Core.Configuration.Policies;

namespace Zeayii.Suba.Core.Services;

/// <summary>
/// Zeayii ONNX 会话工厂。
/// </summary>
/// <param name="options">Zeayii 核心配置。</param>
internal sealed class OnnxSessionFactory(SubaOptions options)
{
    /// <summary>
    /// Zeayii ONNX 日志严重级别（仅输出 Error/Fatal）。
    /// </summary>
    private const OrtLoggingLevel OrtLogSeverityError = OrtLoggingLevel.ORT_LOGGING_LEVEL_ERROR;

    /// <summary>
    /// Zeayii 核心配置。
    /// </summary>
    private readonly SubaOptions _options = options;

    /// <summary>
    /// Zeayii 创建 ONNX 推理会话。
    /// </summary>
    /// <param name="modelPath">Zeayii 模型路径。</param>
    /// <param name="stageKind">Zeayii 运行阶段。</param>
    /// <returns>Zeayii 推理会话。</returns>
    public InferenceSession CreateSession(string modelPath, OnnxRuntimeStageKind stageKind)
    {
        var devicePolicy = ResolveDevicePolicy(stageKind);
        var sessionOptions = CreateSessionOptions();
        if (devicePolicy != ExecutionDevicePolicy.Gpu)
        {
            return new InferenceSession(modelPath, sessionOptions);
        }

        try
        {
            sessionOptions.AppendExecutionProvider_CUDA();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Stage {stageKind} is configured as GPU, but CUDA provider is unavailable.", ex);
        }

        return new InferenceSession(modelPath, sessionOptions);
    }

    /// <summary>
    /// Zeayii 创建会话配置并统一日志级别。
    /// </summary>
    /// <returns>Zeayii 会话配置。</returns>
    private static SessionOptions CreateSessionOptions()
    {
        return new SessionOptions
        {
            LogSeverityLevel = OrtLogSeverityError
        };
    }

    /// <summary>
    /// Zeayii 解析阶段设备策略。
    /// </summary>
    /// <param name="stageKind">Zeayii 运行阶段。</param>
    /// <returns>Zeayii 设备策略。</returns>
    private ExecutionDevicePolicy ResolveDevicePolicy(OnnxRuntimeStageKind stageKind)
    {
        return stageKind switch
        {
            OnnxRuntimeStageKind.Preprocess => _options.Runtime.Preprocess.Device,
            OnnxRuntimeStageKind.Transcribe => _options.Runtime.Transcribe.Device,
            _ => ExecutionDevicePolicy.Cpu
        };
    }
}

/// <summary>
/// Zeayii ONNX 运行阶段标识。
/// </summary>
internal enum OnnxRuntimeStageKind : byte
{
    /// <summary>
    /// Zeayii 前处理阶段。
    /// </summary>
    Preprocess = 1,

    /// <summary>
    /// Zeayii 转录阶段。
    /// </summary>
    Transcribe = 2
}
