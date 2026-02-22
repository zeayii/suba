using System.CommandLine;
using System.Text.Json.Serialization;
using Zeayii.Suba.Core.Configuration.Options;

namespace Zeayii.Suba.CommandLine.Extensions;

/// <summary>
/// Zeayii root command extensions.
/// </summary>
internal static class RootCommandExtensions
{
    /// <summary>
    /// Zeayii register init command for arguments template generation.
    /// </summary>
    /// <param name="root">Zeayii root command.</param>
    public static void AddInitCommand(this RootCommand root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var initCommand = new Command("init", "Generate an arguments TOML template in current directory.");
        var templateOutputOption = new Option<FileInfo>("--output")
        {
            Description = "Template output path.",
            Required = false,
            AllowMultipleArgumentsPerToken = false,
            Arity = ArgumentArity.ZeroOrOne,
            DefaultValueFactory = _ => new FileInfo(Path.Combine(Environment.CurrentDirectory, "arguments.template.toml"))
        }.AcceptLegalFilePathsOnly();
        var templateOverwriteOption = new Option<bool>("--overwrite")
        {
            Description = "Overwrite target file if it already exists.",
            Required = false,
            AllowMultipleArgumentsPerToken = false,
            Arity = ArgumentArity.ZeroOrOne,
            DefaultValueFactory = _ => false
        };

        initCommand.Options.Add(templateOutputOption);
        initCommand.Options.Add(templateOverwriteOption);
        initCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var output = parseResult.GetValue(templateOutputOption)!;
            var overwrite = parseResult.GetValue(templateOverwriteOption);
            var outputPath = Path.GetFullPath(output.FullName);

            try
            {
                if (File.Exists(outputPath) && !overwrite)
                {
                    await Console.Error.WriteLineAsync($"Template already exists: {outputPath}. Use --overwrite to replace it.");
                    return 2;
                }

                var directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var template = CreateTemplateTomlText();
                await File.WriteAllTextAsync(outputPath, template, cancellationToken).ConfigureAwait(false);

                await Console.Out.WriteLineAsync($"Template generated successfully: {outputPath}");
                return 0;
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Failed to generate template: {outputPath}");
                await Console.Error.WriteLineAsync(ex.Message);
                return 1;
            }
        });

        root.Subcommands.Add(initCommand);
    }

    /// <summary>
    /// Zeayii 创建 TOML 模板文本。
    /// </summary>
    /// <returns>Zeayii TOML 模板文本。</returns>
    private static string CreateTemplateTomlText()
    {
        return """"
               # =============================================================================
               # Suba Arguments Template (TOML)
               #
               # 说明：
               # - 本文件只负责定义任务输入与提示词文件路径
               # - 运行参数（设备、并发、模型、日志等）仍通过命令行选项传入
               # - prompt_file 与 fix_prompt_file 推荐使用相对路径（相对当前 TOML 文件）
               # - 不建议直接采用提示词，应结合业务实际场景使用提示词
               # - 注意：尤其是开启上下文参考时，避免上下文污染
               # =============================================================================
               
               # 媒体输入文件列表（支持多个）
               # 建议采用单引号输入路径避免\转义错误
               # 示例：
               inputs = ['D:\Media\example.mp4']
               
               # 翻译提示词
               # 示例：
               prompt = """
               你是一个专业的字幕翻译助手。
               只输出翻译后的文本，不要添加解释、标签或额外格式。
               """
               
               # 翻译修复提示词（针对翻译有困惑的语句会二次润色所需要的提示词）
               # 示例：
               fix_prompt = """
               你是翻译修复助手。
               请将给定文本修正为一句最自然、最简洁的目标语言字幕，仅输出结果。
               """

               """";
    }
}

/// <summary>
/// Zeayii command-line JSON source generation context.
/// </summary>
[JsonSerializable(typeof(SubaArguments))]
[JsonSourceGenerationOptions(WriteIndented =  true, IndentSize = 4)]
internal sealed partial class SubaCommandLineJsonSerializerContext : JsonSerializerContext;
