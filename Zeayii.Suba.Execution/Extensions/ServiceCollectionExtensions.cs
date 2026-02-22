using Microsoft.Extensions.DependencyInjection;
using Zeayii.Suba.Core.Abstractions;
using Zeayii.Suba.Core.Configuration.Options;
using Zeayii.Suba.Core.Contexts;
using Zeayii.Suba.Core.Logging;
using Zeayii.Suba.Core.Orchestration;
using Zeayii.Suba.Core.Services;

namespace Zeayii.Suba.Core.Extensions;

/// <summary>
/// Zeayii Core 服务注册扩展。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Zeayii 注册核心模块所需服务。
    /// </summary>
    /// <param name="services">Zeayii 依赖注入容器。</param>
    /// <param name="options">Zeayii 核心配置。</param>
    /// <returns>Zeayii 依赖注入容器。</returns>
    public static IServiceCollection AddSubaCore(this IServiceCollection services, SubaOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.AddSingleton(options);
        services.AddSingleton<IPresentationManager, NullPresentationManager>();
        services.AddSingleton<IConsoleLogOutput, ConsoleLogOutput>();
        services.AddSingleton<IFileLogOutput, FileLogOutput>();
        services.AddSingleton<IDualLogOutput, DualLogOutput>();
        services.AddSingleton<GlobalContext>();
        services.AddHttpClient("ollama", client =>
        {
            client.BaseAddress = new Uri(options.Translation.OllamaBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.Runtime.CommandTimeoutSeconds);
        });
        services.AddHttpClient("openai", client =>
        {
            client.BaseAddress = new Uri(options.Translation.OpenAiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.Runtime.CommandTimeoutSeconds);
        });

        services.AddSingleton<LanguageTagResolver>();
        services.AddSingleton<OnnxSessionFactory>();
        services.AddSingleton<WavProbe>();
        services.AddSingleton<SubtitleArtifactResolver>();
        services.AddSingleton<IAudioExtractor, FfmpegAudioExtractor>();
        services.AddSingleton<IVadService, SileroVadService>();
        services.AddSingleton<IOverlapDetector, PyannoteOverlapDetector>();
        services.AddSingleton<IAudioSeparator, SepformerAudioSeparator>();
        services.AddSingleton<ITranscriptionService, WhisperOnnxTranscriptionService>();
        services.AddSingleton<OllamaTranslationService>();
        services.AddSingleton<OpenAiTranslationService>();
        services.AddSingleton<ITranslationService, TranslationServiceRouter>();
        services.AddSingleton<ISubtitleWriter, SubtitleWriter>();
        services.AddSingleton<ISubaPipeline, SubaPipeline>();

        return services;
    }
}
