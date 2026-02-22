using Tomlyn;
using Tomlyn.Model;
using Zeayii.Suba.CommandLine.Models;
using Zeayii.Suba.Core.Configuration.Options;

namespace Zeayii.Suba.CommandLine.Services;

/// <summary>
/// Zeayii TOML 参数解析器。
/// </summary>
internal sealed class SubaTomlArgumentsParser
{
    /// <summary>
    /// Zeayii 解析 TOML 文件并构建执行参数。
    /// </summary>
    /// <param name="tomlPath">Zeayii TOML 文件路径。</param>
    /// <param name="cancellationToken">Zeayii 取消令牌。</param>
    /// <returns>Zeayii 执行参数对象。</returns>
    public async Task<SubaArguments> ParseAsync(FileInfo tomlPath, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tomlPath);
        if (!tomlPath.Exists)
        {
            throw new FileNotFoundException("Arguments TOML file does not exist.", tomlPath.FullName);
        }

        var content = await File.ReadAllTextAsync(tomlPath.FullName, cancellationToken).ConfigureAwait(false);
        TomlTable table;
        try
        {
            var model = Toml.ToModel(content);
            table = model ?? throw new InvalidDataException("Arguments TOML root must be a table.");
        }
        catch (Exception ex) when (ex is TomlException or InvalidOperationException)
        {
            throw new InvalidDataException($"Invalid TOML format: {tomlPath.FullName}", ex);
        }

        var document = ParseDocument(table);

        return new SubaArguments
        {
            Inputs = document.Inputs.ToList(),
            Prompt = document.Prompt,
            FixPrompt = document.FixPrompt
        };
    }

    /// <summary>
    /// Zeayii 解析 TOML 文档字段。
    /// </summary>
    /// <param name="table">Zeayii TOML 根表。</param>
    /// <returns>Zeayii 文档对象。</returns>
    private static SubaTomlArgumentsDocument ParseDocument(TomlTable table)
    {
        var inputs = ReadInputs(table);
        var prompt = ReadRequiredString(table, "prompt");
        var fixPrompt = ReadRequiredString(table, "fix_prompt");

        return new SubaTomlArgumentsDocument
        {
            Inputs = inputs,
            Prompt = prompt,
            FixPrompt = fixPrompt
        };
    }

    /// <summary>
    /// Zeayii 读取输入媒体路径数组。
    /// </summary>
    /// <param name="table">Zeayii TOML 根表。</param>
    /// <returns>Zeayii 输入媒体路径集合。</returns>
    private static IReadOnlyList<string> ReadInputs(TomlTable table)
    {
        if (!table.TryGetValue("inputs", out var value) || value is not TomlArray array)
        {
            throw new InvalidDataException("TOML field 'inputs' is required and must be an array.");
        }

        var inputs = new List<string>(array.Count);
        foreach (var item in array)
        {
            if (item is not string text || string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidDataException("All entries in 'inputs' must be non-empty strings.");
            }

            inputs.Add(text);
        }

        return inputs.Count == 0 ? throw new InvalidDataException("TOML field 'inputs' cannot be empty.") : inputs;
    }

    /// <summary>
    /// Zeayii 读取必填字符串字段。
    /// </summary>
    /// <param name="table">Zeayii TOML 根表。</param>
    /// <param name="key">Zeayii 字段名。</param>
    /// <returns>Zeayii 字段值。</returns>
    private static string ReadRequiredString(TomlTable table, string key)
    {
        if (!table.TryGetValue(key, out var value) || value is not string text || string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidDataException($"TOML field '{key}' is required and must be a non-empty string.");
        }

        return text.Trim();
    }

}
