using System.Text.RegularExpressions;

namespace SortingLab.Core.Words;

/// <summary>Разбивает текст на слова.</summary>
public static partial class WordTokenizer
{
    // Слово: подряд идущие буквы, внутри которых может стоять апостроф (don't, o'clock).
    // Цифры, знаки препинания и пробелы служат разделителями.
    [GeneratedRegex(@"\p{L}+(?:['’]\p{L}+)*")]
    private static partial Regex WordPattern();

    /// <summary>
    /// Возвращает слова текста в исходном порядке, приведённые к нижнему регистру.
    /// Типографский апостроф заменяется обычным, чтобы «don’t» и «don't» считались одним словом.
    /// </summary>
    public static string[] Split(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var matches = WordPattern().Matches(text);
        var words = new string[matches.Count];
        for (int i = 0; i < matches.Count; i++)
            words[i] = matches[i].Value.Replace('’', '\'').ToLowerInvariant();
        return words;
    }
}
