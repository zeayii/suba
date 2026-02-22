using System.Text;

namespace Zeayii.Suba.Core.Services.Whisper;

/// <summary>
/// Zeayii Whisper token 解码器。
/// </summary>
internal sealed class WhisperTokenDecoder
{
    /// <summary>
    /// Zeayii token 与文本映射。
    /// </summary>
    private readonly Dictionary<int, string> _tokenById;

    /// <summary>
    /// Zeayii 字节回映射表。
    /// </summary>
    private readonly Dictionary<char, byte> _byteDecoder;

    /// <summary>
    /// Zeayii 构造 token 解码器。
    /// </summary>
    /// <param name="tokenById">Zeayii token 到字符串映射。</param>
    public WhisperTokenDecoder(Dictionary<int, string> tokenById)
    {
        _tokenById = tokenById;
        _byteDecoder = BuildByteDecoder();
    }

    /// <summary>
    /// Zeayii 将 token 序列解码为文本。
    /// </summary>
    /// <param name="tokenIds">Zeayii token 序列。</param>
    /// <returns>Zeayii 解码文本。</returns>
    public string Decode(IReadOnlyList<int> tokenIds)
    {
        var tokenText = new StringBuilder();
        foreach (var taTokenId in tokenIds)
        {
            if (!_tokenById.TryGetValue(taTokenId, out var token))
            {
                continue;
            }

            if (token.StartsWith("<|", StringComparison.Ordinal) && token.EndsWith("|>", StringComparison.Ordinal))
            {
                continue;
            }

            tokenText.Append(token);
        }

        var bytes = new List<byte>(tokenText.Length);
        foreach (var ch in tokenText.ToString())
        {
            if (_byteDecoder.TryGetValue(ch, out var b))
            {
                bytes.Add(b);
            }
        }

        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    /// <summary>
    /// Zeayii 构造 Whisper 字节反向映射表。
    /// </summary>
    /// <returns>Zeayii 字节反向映射字典。</returns>
    private static Dictionary<char, byte> BuildByteDecoder()
    {
        var bs = new List<int>();
        bs.AddRange(Enumerable.Range('!', '~' - '!' + 1));
        bs.AddRange(Enumerable.Range('¡', '¬' - '¡' + 1));
        bs.AddRange(Enumerable.Range('®', 'ÿ' - '®' + 1));

        var cs = new List<int>(bs);
        var n = 0;
        for (var b = 0; b < 256; b++)
        {
            if (bs.Contains(b))
            {
                continue;
            }

            bs.Add(b);
            cs.Add(256 + n);
            n++;
        }

        var byteDecoder = new Dictionary<char, byte>();
        for (var i = 0; i < bs.Count; i++)
        {
            var ch = (char)cs[i];
            byteDecoder[ch] = (byte)bs[i];
        }

        return byteDecoder;
    }
}
