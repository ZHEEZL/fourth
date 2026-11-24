namespace SortingLab.Core.Words;

/// <summary>
/// Быстрая сортировка Хоара для слов. Алгоритм тот же, что и в пошаговом показе,
/// но без вывода шагов: замеры времени должны отражать только саму сортировку.
/// </summary>
public sealed class QuickWordSorter : IWordSorter
{
    public string Name => "Быстрая сортировка Хоара";

    public void Sort(string[] words)
    {
        ArgumentNullException.ThrowIfNull(words);
        SortRange(words, 0, words.Length - 1);
    }

    /// <summary>Сортирует часть массива <c>a[lo..hi]</c>.</summary>
    private static void SortRange(string[] a, int lo, int hi)
    {
        // Рекурсивно сортируем меньшую часть, а большую обрабатываем в этом же цикле.
        // Так глубина рекурсии не превышает log2(n) даже на неудачных данных.
        while (lo < hi)
        {
            string pivot = a[lo + (hi - lo) / 2];
            int i = lo, j = hi;
            while (i <= j)
            {
                while (string.CompareOrdinal(a[i], pivot) < 0)
                    i++;
                while (string.CompareOrdinal(a[j], pivot) > 0)
                    j--;
                if (i <= j)
                {
                    (a[i], a[j]) = (a[j], a[i]);
                    i++;
                    j--;
                }
            }

            // Теперь a[lo..j] ≤ pivot ≤ a[i..hi].
            if (j - lo < hi - i)
            {
                SortRange(a, lo, j);
                lo = i;
            }
            else
            {
                SortRange(a, i, hi);
                hi = j;
            }
        }
    }
}
