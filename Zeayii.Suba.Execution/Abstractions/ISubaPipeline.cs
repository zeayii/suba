using Zeayii.Suba.Core.Configuration.Options;

namespace Zeayii.Suba.Core.Abstractions;

/// <summary>
/// Zeayii 处理流水线接口。
/// </summary>
public interface ISubaPipeline
{
    /// <summary>
    /// Zeayii 执行整批媒体任务。
    /// </summary>
    /// <param name="arguments">Zeayii 任务参数。</param>
    /// <param name="cancellationToken">Zeayii 取消令牌。</param>
    /// <returns>Zeayii 异步任务。</returns>
    Task RunAsync(SubaArguments arguments, CancellationToken cancellationToken);
}