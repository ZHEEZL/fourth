namespace SortingLab.Core.Visual;

/// <summary>
/// Бинарная пирамидальная сортировка (HeapSort). Массив рассматривается как двоичная пирамида:
/// у элемента с индексом i потомки имеют индексы 2i+1 и 2i+2.
/// Сначала строится пирамида, в которой каждый родитель не меньше потомков,
/// затем максимум с вершины раз за разом переносится в конец массива.
/// </summary>
public sealed class HeapSorter : IVisualSorter
{
    public string Name => "Пирамидальная (HeapSort)";

    public string Group => "Усовершенствованные алгоритмы";

    public string Idea =>
        "Представим, что числа стоят пирамидой. У числа на позиции i два «ребёнка»: на позициях 2i+1 и 2i+2. " +
        "Сначала переставляем числа так, чтобы каждый родитель был не меньше своих детей. " +
        "Тогда на вершине, на позиции 0, окажется самое большое число. Меняем его с последним числом пирамиды, " +
        "и максимум встаёт на своё место в конце. Новое число на вершине опускаем вниз, пока пирамида снова не станет правильной. " +
        "Повторяем, пока пирамида не кончится.";

    public string Complexity => "Время: O(n log n) в любом случае. Дополнительная память: O(1).";

    public IEnumerable<SortStep> Sort(int[] array)
    {
        int[] a = array;
        int n = a.Length;
        if (n > 1)
        {
            yield return new SortStep
            {
                Kind = StepKind.Info,
                Log = "Этап 1: строим пирамиду",
                Explanation = "Этап 1. Строим пирамиду: переставляем числа так, чтобы каждый «родитель» был не меньше своих «детей». " +
                              "Начинаем с последнего родителя и движемся к началу массива.",
                RangeStart = 0,
                RangeEnd = n - 1,
            };

            // Элементы с индексами от n/2 и дальше детей не имеют, начинаем с последнего родителя.
            for (int i = n / 2 - 1; i >= 0; i--)
            {
                foreach (var step in SiftDown(a, i, n))
                    yield return step;
            }

            yield return new SortStep
            {
                Kind = StepKind.Info,
                Log = $"Этап 2: пирамида готова, максимум a[0]={a[0]}",
                Explanation = $"Пирамида готова: на вершине, на позиции 0, стоит самое большое число {a[0]}. " +
                              "Этап 2. Переносим вершину в конец и чиним пирамиду, пока она не кончится.",
                RangeStart = 0,
                RangeEnd = n - 1,
            };

            for (int end = n - 1; end > 0; end--)
            {
                int top = a[0], last = a[end];
                (a[0], a[end]) = (last, top);
                yield return new SortStep
                {
                    Kind = StepKind.Swap,
                    Log = $"Обмен a[0] ↔ a[{end}]: максимум {top} уходит в конец",
                    Explanation = $"Самое большое из оставшихся чисел, {top}, стоит на вершине. " +
                                  $"Меняем его с последним числом пирамиды, {last}. Теперь {top} на своём месте, а пирамида стала на одно число короче.",
                    First = 0,
                    Second = end,
                    NewlySorted = [end],
                    RangeStart = 0,
                    RangeEnd = end - 1,
                };

                foreach (var step in SiftDown(a, 0, end))
                    yield return step;
            }
        }

        yield return StepFactory.Done(n);
    }

    /// <summary>
    /// Просеивание: опускает элемент с индексом <paramref name="i"/> вниз по пирамиде из
    /// <paramref name="size"/> элементов, пока он не станет не меньше своих потомков.
    /// </summary>
    private static IEnumerable<SortStep> SiftDown(int[] a, int i, int size)
    {
        while (true)
        {
            int left = 2 * i + 1;
            if (left >= size)
                yield break; // детей нет

            int right = left + 1;
            int child = left;
            if (right < size)
            {
                bool rightBigger = a[right] > a[left];
                int bigger = rightBigger ? a[right] : a[left];
                yield return new SortStep
                {
                    Kind = StepKind.Compare,
                    Log = $"Сравнение детей a[{left}]={a[left]} и a[{right}]={a[right]}: {a[left]} {(rightBigger ? "<" : "≥")} {a[right]}",
                    Explanation = $"У числа {a[i]} на позиции {i} двое детей: {a[left]} и {a[right]}. Выбираем того, кто больше: {bigger}.",
                    First = left,
                    Second = right,
                    RangeStart = 0,
                    RangeEnd = size - 1,
                };
                if (rightBigger)
                    child = right;
            }

            int parent = a[i], kid = a[child];
            bool swap = kid > parent;
            yield return new SortStep
            {
                Kind = StepKind.Compare,
                Log = $"Сравнение a[{i}]={parent} и a[{child}]={kid}: {parent} {(swap ? "<" : "≥")} {kid}",
                Explanation = swap
                    ? $"Сравниваем родителя {parent} с ребёнком {kid}. Ребёнок больше родителя, а в пирамиде так быть не должно."
                    : $"Сравниваем родителя {parent} с ребёнком {kid}. Родитель не меньше ребёнка, здесь пирамида в порядке.",
                First = i,
                Second = child,
                RangeStart = 0,
                RangeEnd = size - 1,
            };
            if (!swap)
                yield break;

            (a[i], a[child]) = (kid, parent);
            yield return new SortStep
            {
                Kind = StepKind.Swap,
                Log = $"Обмен a[{i}] ↔ a[{child}]: теперь {kid}, {parent}",
                Explanation = $"Меняем родителя и ребёнка местами: {kid} поднимается наверх, {parent} опускается на позицию {child}. " +
                              $"Дальше проверяем {parent} на новом месте.",
                First = i,
                Second = child,
                RangeStart = 0,
                RangeEnd = size - 1,
            };
            i = child;
        }
    }
}
