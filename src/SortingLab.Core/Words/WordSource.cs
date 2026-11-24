namespace SortingLab.Core.Words;

/// <summary>Готовит наборы слов заданной длины для замеров.</summary>
public static class WordSource
{
    /// <summary>
    /// Первые <paramref name="count"/> слов. Если слов меньше, список повторяется с начала.
    /// </summary>
    public static string[] TakeCyclic(IReadOnlyList<string> words, int count)
    {
        CheckArguments(words, count);
        var result = new string[count];
        for (int i = 0; i < count; i++)
            result[i] = words[i % words.Count];
        return result;
    }

    /// <summary>
    /// <paramref name="count"/> случайных слов из списка (с повторениями).
    /// Частые в тексте слова попадаются в выборку чаще, как и в настоящем тексте.
    /// При одинаковом <paramref name="seed"/> результат всегда один и тот же.
    /// </summary>
    public static string[] RandomSample(IReadOnlyList<string> words, int count, int seed)
    {
        CheckArguments(words, count);
        var random = new Random(seed);
        var result = new string[count];
        for (int i = 0; i < count; i++)
            result[i] = words[random.Next(words.Count)];
        return result;
    }

    private static void CheckArguments(IReadOnlyList<string> words, int count)
    {
        ArgumentNullException.ThrowIfNull(words);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (words.Count == 0)
            throw new ArgumentException("Список слов пуст.", nameof(words));
    }
}
