using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Zeayii.Suba.CommandLine.Extensions;
using Zeayii.Suba.CommandLine.Options;
using Zeayii.Suba.CommandLine.Services;
using Zeayii.Suba.Core.Abstractions;
using Zeayii.Suba.Core.Configuration.Options;
using Zeayii.Suba.Core.Contexts;
using Zeayii.Suba.Core.Extensions;
using Zeayii.Suba.Presentation.Extensions;

var root = new RootCommand("Suba - Subtitle Generator for Media.");
root.AddInitCommand();

var commandOptions = root.AddSubaOptions();
root.SetAction(async (parseResult, cancellationToken) =>
{
    var appOptions = parseResult.BuildApplicationOptions(commandOptions);
    var argumentPath = appOptions.ArgumentsTomlPath;
    if (!argumentPath.Exists)
    {
        await Console.Error.WriteLineAsync("Arguments TOML path does not exist.");
        return 2;
    }

    SubaArguments arguments;
    try
    {
        var parser = new SubaTomlArgumentsParser();
        arguments = await parser.ParseAsync(argumentPath, cancellationToken).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        await Console.Error.WriteLineAsync($"Invalid arguments TOML: {ex.Message}");
        return 2;
    }

    var subaOptions = OptionsBuilder.BuildSubaOptions(appOptions);
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddSubaCore(subaOptions);
    services.AddSubaPresentation();

    await using var provider = services.BuildServiceProvider();
    var global = provider.GetRequiredService<GlobalContext>();
    global.Presentation.SetLogLevels(subaOptions.Logging.ConsoleLogLevel, subaOptions.Logging.FileLogLevel);

    using var presentationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    var presentationTask = global.Presentation.RunAsync(presentationCts.Token);

    try
    {
        var pipeline = provider.GetRequiredService<ISubaPipeline>();
        await pipeline.RunAsync(arguments, cancellationToken).ConfigureAwait(false);
        global.Log.Info("Execute", "Suba completed.");
        return 0;
    }
    catch (Exception ex)
    {
        global.Log.Error("Execute", "Suba failed.", ex);
        return 1;
    }
    finally
    {
        await presentationCts.CancelAsync();
        await global.Presentation.StopAsync().ConfigureAwait(false);
        try
        {
            await presentationTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // ignore
        }
    }
});

return await root.Parse(args).InvokeAsync().ConfigureAwait(false);
