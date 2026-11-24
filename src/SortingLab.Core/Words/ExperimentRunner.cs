using System.Diagnostics;

namespace SortingLab.Core.Words;

/// <summary>Результат замера для одного размера текста.</summary>
/// <param name="WordCount">Число слов в тексте.</param>
/// <param name="AverageMs">Среднее время сортировки в миллисекундах, по одному значению на алгоритм.</param>
public sealed record ExperimentRow(int WordCount, IReadOnlyList<double> AverageMs);

/// <summary>Замеряет время сортировки слов на текстах разной длины.</summary>
public static class ExperimentRunner
{
    /// <summary>Проводит замеры.</summary>
    /// <param name="sorters">Алгоритмы, порядок совпадает с порядком значений в <see cref="ExperimentRow.AverageMs"/>.</param>
    /// <param name="sizes">Размеры текстов в словах.</param>
    /// <param name="makeWords">Возвращает набор слов заданного размера.</param>
    /// <param name="repeats">Сколько раз сортировать каждый набор каждым алгоритмом.</param>
    /// <param name="progress">Сообщения о ходе работы.</param>
    /// <param name="cancellationToken">Отмена между размерами.</param>
    public static IReadOnlyList<ExperimentRow> Run(
        IReadOnlyList<IWordSorter> sorters,
        IReadOnlyList<int> sizes,
        Func<int, string[]> makeWords,
        int repeats,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sorters);
        ArgumentNullException.ThrowIfNull(sizes);
        ArgumentNullException.ThrowIfNull(makeWords);
        ArgumentOutOfRangeException.ThrowIfLessThan(repeats, 1);
        if (sizes.Count == 0)
            return [];

        // Прогрев: при первых вызовах методы компилируются JIT-компилятором, это время в замеры не входит.
        string[] warmUp = makeWords(Math.Min(sizes.Min(), 500));
        foreach (var sorter in sorters)
        {
            for (int i = 0; i < 3; i++)
                sorter.Sort((string[])warmUp.Clone());
        }

        var rows = new List<ExperimentRow>(sizes.Count);
        foreach (int size in sizes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report($"Замеряем набор из {size} {RussianPlural.Choose(size, "слова", "слов", "слов")}…");

            string[] words = makeWords(size);
            var times = new double[sorters.Count];
            for (int s = 0; s < sorters.Count; s++)
            {
                double total = 0;
                for (int r = 0; r < repeats; r++)
                {
                    // Каждый запуск получает свежую копию: со второго раза исходный массив был бы уже упорядочен.
                    string[] copy = (string[])words.Clone();
                    long start = Stopwatch.GetTimestamp();
                    sorters[s].Sort(copy);
                    total += Stopwatch.GetElapsedTime(start).TotalMilliseconds;

                    if (r == 0 && !IsSorted(copy))
                        throw new InvalidOperationException($"Алгоритм «{sorters[s].Name}» вернул неупорядоченный массив.");
                }
                times[s] = total / repeats;
            }
            rows.Add(new ExperimentRow(size, times));
        }
        return rows;
    }

    /// <summary>Проверяет, что слова идут в лексикографическом порядке.</summary>
    public static bool IsSorted(IReadOnlyList<string> words)
    {
        for (int i = 1; i < words.Count; i++)
        {
            if (string.CompareOrdinal(words[i - 1], words[i]) > 0)
                return false;
        }
        return true;
    }
}
