namespace SortingLab.Core.Words;

/// <summary>Слово и количество его вхождений в тексте.</summary>
/// <param name="Word">Слово в нижнем регистре.</param>
/// <param name="Count">Сколько раз встретилось.</param>
public sealed record WordFrequency(string Word, int Count);

/// <summary>Подсчёт частоты слов за один проход по отсортированному массиву.</summary>
public static class WordFrequencyCounter
{
    /// <summary>
    /// Проходит по уже отсортированному списку слов, где одинаковые слова стоят подряд,
    /// и возвращает список пар «слово — количество повторений».
    /// </summary>
    public static IReadOnlyList<WordFrequency> CountSorted(IReadOnlyList<string> sortedWords)
    {
        ArgumentNullException.ThrowIfNull(sortedWords);
        if (sortedWords.Count == 0)
            return [];

        var result = new List<WordFrequency>();
        string current = sortedWords[0];
        int count = 1;

        for (int i = 1; i < sortedWords.Count; i++)
        {
            if (string.Equals(sortedWords[i], current, StringComparison.Ordinal))
            {
                count++;
            }
            else
            {
                result.Add(new WordFrequency(current, count));
                current = sortedWords[i];
                count = 1;
            }
        }

        result.Add(new WordFrequency(current, count));
        return result;
    }
}