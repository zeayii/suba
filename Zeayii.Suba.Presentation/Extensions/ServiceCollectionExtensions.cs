using Microsoft.Extensions.DependencyInjection;
using Zeayii.Suba.Core.Abstractions;
using Zeayii.Suba.Presentation.Configuration;
using Zeayii.Suba.Presentation.Core.Logging;
using Zeayii.Suba.Presentation.Core.Progress;
using Zeayii.Suba.Presentation.Services;

namespace Zeayii.Suba.Presentation.Extensions;

/// <summary>
/// Zeayii Presentation 服务注册扩展。
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Zeayii 注册窗口呈现服务。
    /// </summary>
    /// <param name="services">Zeayii 依赖注入容器。</param>
    /// <returns>Zeayii 依赖注入容器。</returns>
    public static IServiceCollection AddSubaPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton(new PresentationOptions());
        services.AddSingleton<LogStore>();
        services.AddSingleton<TaskStore>();
        services.AddSingleton<IPresentationManager, ConsolePresentationManager>();
        return services;
    }
}