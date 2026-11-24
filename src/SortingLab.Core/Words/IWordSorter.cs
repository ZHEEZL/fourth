namespace SortingLab.Core.Words;

/// <summary>Алгоритм сортировки слов в лексикографическом порядке.</summary>
/// <remarks>
/// Слова сравниваются посимвольно по кодам символов, как в <see cref="string.CompareOrdinal(string, string)"/>.
/// <see cref="WordTokenizer"/> приводит слова к нижнему регистру, поэтому для латиницы
/// это обычный алфавитный порядок, а одинаковые слова оказываются рядом.
/// </remarks>
public interface IWordSorter
{
    /// <summary>Название алгоритма для списка и заголовка таблицы.</summary>
    string Name { get; }

    /// <summary>Сортирует массив слов на месте.</summary>
    void Sort(string[] words);
}
