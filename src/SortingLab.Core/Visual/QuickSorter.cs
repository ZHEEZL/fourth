using System.Text;

namespace SortingLab.Core.Visual;

/// <summary>
/// Быстрая сортировка Хоара. Опорный элемент берётся из середины части.
/// Два индекса идут навстречу друг другу и меняют местами элементы,
/// которые стоят не по свою сторону от опорного. Затем обе части сортируются тем же способом.
/// </summary>
public sealed class QuickSorter : IVisualSorter
{
    public string Name => "Быстрая сортировка Хоара";

    public string Group => "Усовершенствованные алгоритмы";

    public string Idea =>
        "Берём число из середины и называем его опорным. Двумя указателями идём с краёв навстречу друг другу. " +
        "Левый останавливается на числе, которое не меньше опорного, правый на числе, которое не больше. " +
        "Эти два числа меняем местами и идём дальше. Когда указатели разойдутся, слева окажутся числа не больше опорного, " +
        "справа не меньше. Потом так же сортируем левую и правую части по отдельности.";

    public string Complexity =>
        "Время: O(n log n) в среднем, O(n²) в худшем случае. Дополнительная память: O(log n) в среднем на рекурсию.";

    public IEnumerable<SortStep> Sort(int[] array)
    {
        foreach (var step in SortRange(array, 0, array.Length - 1))
            yield return step;
        yield return StepFactory.Done(array.Length);
    }

    /// <summary>Сортирует часть массива <c>a[lo..hi]</c>.</summary>
    private static IEnumerable<SortStep> SortRange(int[] a, int lo, int hi)
    {
        if (lo > hi)
            yield break;

        if (lo == hi)
        {
            yield return new SortStep
            {
                Kind = StepKind.Info,
                Log = $"Часть a[{lo}] из одного элемента",
                Explanation = $"В этой части одно число, {a[lo]}. Сравнивать его не с чем, оно уже на своём месте.",
                NewlySorted = [lo],
                RangeStart = lo,
                RangeEnd = hi,
            };
            yield break;
        }

        int pivotIndex = lo + (hi - lo) / 2;
        int p = a[pivotIndex];
        yield return new SortStep
        {
            Kind = StepKind.Info,
            Log = $"Часть a[{lo}]..a[{hi}], опорный a[{pivotIndex}]={p}",
            Explanation = $"Разбираем часть с позиции {lo} по {hi}. Опорным берём число из середины: {p}. " +
                          $"Числа меньше {p} должны оказаться слева, числа больше {p} справа.",
            Pivot = pivotIndex,
            RangeStart = lo,
            RangeEnd = hi,
        };

        int i = lo, j = hi;
        while (i <= j)
        {
            // Левый указатель идёт вправо, пока числа меньше опорного.
            while (true)
            {
                int v = a[i];
                bool less = v < p;
                yield return new SortStep
                {
                    Kind = StepKind.Compare,
                    Log = $"Сравнение a[{i}]={v} и опорного {p}: {v} {(less ? "<" : "≥")} {p}",
                    Explanation = less
                        ? $"Левый указатель: {v} меньше опорного {p}, оно и так слева. Шагаем дальше вправо."
                        : $"Левый указатель: {v} не меньше опорного {p}. Останавливаемся, это число надо отправить направо.",
                    First = i,
                    Second = pivotIndex,
                    Pivot = pivotIndex,
                    RangeStart = lo,
                    RangeEnd = hi,
                };
                if (!less)
                    break;
                i++;
            }

            // Правый указатель идёт влево, пока числа больше опорного.
            while (true)
            {
                int v = a[j];
                bool greater = v > p;
                yield return new SortStep
                {
                    Kind = StepKind.Compare,
                    Log = $"Сравнение a[{j}]={v} и опорного {p}: {v} {(greater ? ">" : "≤")} {p}",
                    Explanation = greater
                        ? $"Правый указатель: {v} больше опорного {p}, оно и так справа. Шагаем дальше влево."
                        : $"Правый указатель: {v} не больше опорного {p}. Останавливаемся, это число надо отправить налево.",
                    First = j,
                    Second = pivotIndex,
                    Pivot = pivotIndex,
                    RangeStart = lo,
                    RangeEnd = hi,
                };
                if (!greater)
                    break;
                j--;
            }

            if (i > j)
                break;

            // Если указатели встретились на одном элементе, он равен опорному и обмен не нужен.
            if (i < j)
            {
                int left = a[i], right = a[j];
                (a[i], a[j]) = (right, left);

                // Опорный элемент мог переехать вместе с обменом, следим за его индексом.
                if (pivotIndex == i)
                    pivotIndex = j;
                else if (pivotIndex == j)
                    pivotIndex = i;

                yield return new SortStep
                {
                    Kind = StepKind.Swap,
                    Log = $"Обмен a[{i}] ↔ a[{j}]: теперь {right}, {left}",
                    Explanation = left == right
                        ? $"Оба числа равны опорному {p}. Алгоритм Хоара всё равно меняет их местами и идёт дальше, так части выходят примерно поровну."
                        : $"Меняем местами {left} и {right}: меньшее уходит налево, большее направо.",
                    First = i,
                    Second = j,
                    Pivot = pivotIndex,
                    RangeStart = lo,
                    RangeEnd = hi,
                };
            }
            i++;
            j--;
        }

        // Между j и i могут остаться элементы, равные опорному: они уже на своих местах.
        int[] middle = StepFactory.Indices(j + 1, i - 1);
        yield return new SortStep
        {
            Kind = StepKind.Info,
            Log = DescribePartitionForLog(lo, hi, i, j, p),
            Explanation = DescribePartition(lo, hi, i, j, p, middle.Length),
            NewlySorted = middle,
            RangeStart = lo,
            RangeEnd = hi,
        };

        foreach (var step in SortRange(a, lo, j))
            yield return step;
        foreach (var step in SortRange(a, i, hi))
            yield return step;
    }

    /// <summary>«позиции 3» или «позициях 3–7»: продолжение для предлога «на».</summary>
    private static string Positions(int from, int to) =>
        from == to ? $"позиции {from}" : $"позициях {from}–{to}";

    private static string DescribePartition(int lo, int hi, int i, int j, int p, int middleCount)
    {
        var text = new StringBuilder("Разделение закончено.");
        if (j >= lo)
            text.Append($" На {Positions(lo, j)} теперь только числа не больше {p}.");
        if (i <= hi)
            text.Append($" На {Positions(i, hi)} только числа не меньше {p}.");
        if (middleCount == 1)
            text.Append($" Число {p} на позиции {j + 1} стоит между ними и уже на своём месте.");
        else if (middleCount > 1)
            text.Append($" Числа на {Positions(j + 1, i - 1)} равны {p} и уже на своих местах.");
        text.Append(j >= lo || i <= hi
            ? " Дальше так же разбираем каждую часть отдельно."
            : " Эта часть готова.");
        return text.ToString();
    }

    private static string DescribePartitionForLog(int lo, int hi, int i, int j, int p)
    {
        var parts = new List<string>();
        if (j >= lo)
            parts.Add($"a[{lo}]..a[{j}] ≤ {p}");
        if (j + 1 <= i - 1)
            parts.Add($"a[{j + 1}]..a[{i - 1}] = {p}");
        if (i <= hi)
            parts.Add($"a[{i}]..a[{hi}] ≥ {p}");
        return "Разделение: " + string.Join("; ", parts);
    }
}
