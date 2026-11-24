namespace SortingLab.Core.Visual;

/// <summary>
/// Сортировка пузырьком с двумя улучшениями.
/// <list type="number">
/// <item>Запоминается место последнего обмена. Правее него массив уже упорядочен,
/// поэтому следующий проход заканчивается раньше.</item>
/// <item>Если за проход не было ни одного обмена, сортировка останавливается.</item>
/// </list>
/// </summary>
public sealed class BubbleSorter : IVisualSorter
{
    public string Name => "Пузырьком (с улучшениями)";

    public string Group => "Квадратичные алгоритмы";

    public string Idea =>
        "Идём по ряду слева направо и сравниваем соседей. Если левое число больше правого, меняем их местами. " +
        "За один проход самое большое число доезжает до конца ряда, как пузырёк воздуха поднимается со дна стакана. " +
        "Первое улучшение: правее места последнего обмена числа уже стоят по порядку, туда больше не заглядываем. " +
        "Второе: если за весь проход не было ни одного обмена, сортировка закончена.";

    public string Complexity =>
        "Время: O(n²) в среднем и в худшем случае, O(n) на уже упорядоченном массиве. Дополнительная память: O(1).";

    public IEnumerable<SortStep> Sort(int[] array)
    {
        int[] a = array;
        int bound = a.Length - 1; // последний индекс неупорядоченной части
        int pass = 0;

        while (bound > 0)
        {
            pass++;
            yield return new SortStep
            {
                Kind = StepKind.Info,
                Log = $"Проход {pass}: a[0]..a[{bound}]",
                Explanation = $"Проход {pass}. Сравниваем соседей слева направо до позиции {bound}. " +
                              "Самое большое число из этой части приедет в её конец.",
                RangeStart = 0,
                RangeEnd = bound,
            };

            int lastSwap = -1; // левый индекс последнего обмена; -1, если обменов не было
            for (int j = 0; j < bound; j++)
            {
                int left = a[j], right = a[j + 1];
                bool needSwap = left > right;
                yield return new SortStep
                {
                    Kind = StepKind.Compare,
                    Log = $"Сравнение a[{j}]={left} и a[{j + 1}]={right}: {left} {(needSwap ? ">" : "≤")} {right}",
                    Explanation = needSwap
                        ? $"Сравниваем соседей {left} и {right}. Левое больше правого, значит, они стоят не по порядку."
                        : $"Сравниваем соседей {left} и {right}. Левое не больше правого, всё в порядке, идём дальше.",
                    First = j,
                    Second = j + 1,
                    RangeStart = 0,
                    RangeEnd = bound,
                };
                if (!needSwap)
                    continue;

                (a[j], a[j + 1]) = (right, left);
                lastSwap = j;
                yield return new SortStep
                {
                    Kind = StepKind.Swap,
                    Log = $"Обмен a[{j}] ↔ a[{j + 1}]: теперь {right}, {left}",
                    Explanation = $"Меняем {left} и {right} местами. Большее число, {left}, сдвинулось на шаг вправо.",
                    First = j,
                    Second = j + 1,
                    RangeStart = 0,
                    RangeEnd = bound,
                };
            }

            if (lastSwap < 0)
            {
                // Улучшение 2: проход без обменов означает, что массив уже упорядочен.
                yield return new SortStep
                {
                    Kind = StepKind.Info,
                    Log = $"Проход {pass} без обменов, массив упорядочен",
                    Explanation = "За весь проход не понадобилось ни одного обмена. " +
                                  "Значит, все числа уже стоят по порядку, и можно закончить раньше.",
                    NewlySorted = StepFactory.Indices(0, bound),
                };
                break;
            }

            // Улучшение 1: правее последнего обмена все элементы уже на своих местах.
            yield return new SortStep
            {
                Kind = StepKind.Info,
                Log = lastSwap + 1 == bound
                    ? $"a[{bound}] на своём месте"
                    : $"a[{lastSwap + 1}]..a[{bound}] на своих местах",
                Explanation = lastSwap + 1 == bound
                    ? $"Проход закончен. Число {a[bound]} встало на своё место, следующий проход до него не дойдёт."
                    : $"Проход закончен. Последний обмен был на позиции {lastSwap}, а правее обменов не было. " +
                      $"Значит, числа с позиции {lastSwap + 1} по {bound} уже на своих местах, и следующий проход будет короче.",
                NewlySorted = StepFactory.Indices(lastSwap + 1, bound),
            };
            bound = lastSwap;
        }

        yield return StepFactory.Done(a.Length);
    }
}
