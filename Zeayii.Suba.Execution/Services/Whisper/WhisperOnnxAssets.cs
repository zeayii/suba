using System.Text.Json;

namespace Zeayii.Suba.Core.Services.Whisper;

/// <summary>
/// Zeayii Whisper 模型资产加载器。
/// </summary>
internal sealed class WhisperOnnxAssets
{
    /// <summary>
    /// Zeayii 起始转写 token 编号。
    /// </summary>
    public required int StartOfTranscriptTokenId { get; init; }

    /// <summary>
    /// Zeayii 结束 token 编号。
    /// </summary>
    public required int EndOfTextTokenId { get; init; }

    /// <summary>
    /// Zeayii 转写任务 token 编号。
    /// </summary>
    public required int TranscribeTaskTokenId { get; init; }

    /// <summary>
    /// Zeayii 禁止时间戳 token 编号。
    /// </summary>
    public required int NoTimestampsTokenId { get; init; }

    /// <summary>
    /// Zeayii 无语音 token 编号。
    /// </summary>
    public required int NoSpeechTokenId { get; init; }

    /// <summary>
    /// Zeayii 抑制 token 集合。
    /// </summary>
    public required HashSet<int> SuppressTokenIds { get; init; }

    /// <summary>
    /// Zeayii 语言 token 映射。
    /// </summary>
    public required Dictionary<string, int> LanguageTokenIds { get; init; }

    /// <summary>
    /// Zeayii token 文本映射。
    /// </summary>
    public required Dictionary<int, string> TokenById { get; init; }

    /// <summary>
    /// Zeayii 时间戳 token 映射。
    /// </summary>
    public required Dictionary<int, float> TimestampById { get; init; }

    /// <summary>
    /// Zeayii 加载 Whisper 资产配置。
    /// </summary>
    /// <param name="whisperModelRoot">Zeayii Whisper 模型根目录。</param>
    /// <returns>Zeayii Whisper 资产对象。</returns>
    public static WhisperOnnxAssets Load(string whisperModelRoot)
    {
        var generationConfigPath = Path.Combine(whisperModelRoot, "generation_config.json");
        var tokenizerPath = Path.Combine(whisperModelRoot, "tokenizer.json");
        var addedTokensPath = Path.Combine(whisperModelRoot, "added_tokens.json");

        using var generationDoc = JsonDocument.Parse(File.ReadAllText(generationConfigPath));
        using var tokenizerDoc = JsonDocument.Parse(File.ReadAllText(tokenizerPath));
        using var addedTokensDoc = JsonDocument.Parse(File.ReadAllText(addedTokensPath));

        var generation = generationDoc.RootElement;
        var taskToId = generation.GetProperty("task_to_id");
        var langToId = generation.GetProperty("lang_to_id");

        var languageTokenIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var lang in langToId.EnumerateObject())
        {
            var key = lang.Name.Trim();
            if (key.StartsWith("<|", StringComparison.Ordinal) && key.EndsWith("|>", StringComparison.Ordinal))
            {
                key = key.Substring(2, key.Length - 4);
            }

            languageTokenIds[key] = lang.Value.GetInt32();
        }

        var suppressTokenIds = new HashSet<int>();
        foreach (var item in generation.GetProperty("suppress_tokens").EnumerateArray())
        {
            suppressTokenIds.Add(item.GetInt32());
        }

        var tokenById = new Dictionary<int, string>();
        var vocab = tokenizerDoc.RootElement.GetProperty("model").GetProperty("vocab");
        foreach (var token in vocab.EnumerateObject())
        {
            tokenById[token.Value.GetInt32()] = token.Name;
        }

        var timestampById = new Dictionary<int, float>();
        foreach (var token in addedTokensDoc.RootElement.EnumerateObject())
        {
            var raw = token.Name;
            if (!raw.StartsWith("<|", StringComparison.Ordinal) || !raw.EndsWith("|>", StringComparison.Ordinal))
            {
                continue;
            }

            var body = raw.Substring(2, raw.Length - 4);
            if (!float.TryParse(body, out var seconds))
            {
                continue;
            }

            timestampById[token.Value.GetInt32()] = seconds;
            tokenById[token.Value.GetInt32()] = raw;
        }

        return new WhisperOnnxAssets
        {
            StartOfTranscriptTokenId = generation.GetProperty("decoder_start_token_id").GetInt32(),
            EndOfTextTokenId = generation.GetProperty("eos_token_id").GetInt32(),
            TranscribeTaskTokenId = taskToId.GetProperty("transcribe").GetInt32(),
            NoTimestampsTokenId = generation.GetProperty("no_timestamps_token_id").GetInt32(),
            NoSpeechTokenId = tokenById.Where(x => string.Equals(x.Value, "<|nospeech|>", StringComparison.Ordinal)).Select(x => x.Key).DefaultIfEmpty(-1).First(),
            SuppressTokenIds = suppressTokenIds,
            LanguageTokenIds = languageTokenIds,
            TokenById = tokenById,
            TimestampById = timestampById
        };
    }
}