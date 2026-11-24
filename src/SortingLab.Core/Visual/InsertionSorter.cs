namespace SortingLab.Core.Visual;

/// <summary>
/// Сортировка простыми вставками. Левая часть массива всегда упорядочена.
/// Очередной элемент берётся «в руку», большие элементы левой части сдвигаются
/// на одну позицию вправо, а он кладётся на освободившееся место.
/// </summary>
public sealed class InsertionSorter : IVisualSorter
{
    public string Name => "Простыми вставками";

    public string Group => "Квадратичные алгоритмы";

    public string Idea =>
        "Так раскладывают карты в руке: слева лежат карты, которые уже идут по порядку. " +
        "Берём следующую карту, и её место освобождается. Пока слева от свободного места лежит карта больше нашей, сдвигаем её на одно место вправо. " +
        "Когда слева окажется карта не больше нашей или карты слева кончатся, кладём нашу карту на свободное место.";

    public string Complexity =>
        "Время: O(n²) в среднем и в худшем случае, O(n) на почти упорядоченном массиве. Дополнительная память: O(1).";

    public IEnumerable<SortStep> Sort(int[] array)
    {
        int[] a = array;
        for (int i = 1; i < a.Length; i++)
        {
            int x = a[i];  // число «в руке»
            int hole = i;  // свободное место, куда можно положить x
            yield return new SortStep
            {
                Kind = StepKind.Info,
                Log = $"Берём x = a[{i}] = {x}",
                Explanation = $"Берём в руку число {x}. Числа левее него уже стоят по порядку. " +
                              $"Ищем, куда вставить {x}, чтобы порядок не сломался.",
                Hole = hole,
                Held = x,
                RangeStart = 0,
                RangeEnd = i,
            };

            while (hole > 0)
            {
                int y = a[hole - 1];
                bool shift = y > x;
                yield return new SortStep
                {
                    Kind = StepKind.Compare,
                    Log = $"Сравнение a[{hole - 1}]={y} и x={x}: {y} {(shift ? ">" : "≤")} {x}",
                    Explanation = shift
                        ? $"Сравниваем {y} с числом в руке. {y} больше {x}, значит, {x} должно стоять левее, а {y} надо подвинуть."
                        : $"Сравниваем {y} с числом в руке. {y} не больше {x}, значит, место найдено: сразу справа от {y}.",
                    First = hole - 1,
                    Second = hole,
                    Hole = hole,
                    Held = x,
                    RangeStart = 0,
                    RangeEnd = i,
                };
                if (!shift)
                    break;

                // Сдвиг: большее число переезжает на свободное место, а свободное место смещается влево.
                a[hole] = y;
                hole--;
                yield return new SortStep
                {
                    Kind = StepKind.Move,
                    Log = $"Сдвиг a[{hole}]={y} → a[{hole + 1}]",
                    Explanation = $"Сдвигаем {y} на одно место вправо. Свободное место переехало на позицию {hole}.",
                    First = hole + 1,
                    Hole = hole,
                    Held = x,
                    RangeStart = 0,
                    RangeEnd = i,
                };
            }

            if (hole == i)
            {
                yield return new SortStep
                {
                    Kind = StepKind.Info,
                    Log = $"x={x} остаётся на месте a[{i}]",
                    Explanation = $"Двигать ничего не пришлось: {x} уже на своём месте. Теперь числа с позиции 0 по {i} стоят по порядку.",
                    RangeStart = 0,
                    RangeEnd = i,
                };
                continue;
            }

            a[hole] = x;
            yield return new SortStep
            {
                Kind = StepKind.Move,
                Log = $"Вставка x={x} в a[{hole}]",
                Explanation = hole == 0
                    ? $"Левее чисел не осталось, значит, {x} меньше всех просмотренных. Кладём его в начало. Теперь числа с позиции 0 по {i} стоят по порядку."
                    : $"Кладём {x} на свободное место, позицию {hole}. Теперь числа с позиции 0 по {i} стоят по порядку.",
                First = hole,
                RangeStart = 0,
                RangeEnd = i,
            };
        }

        yield return StepFactory.Done(a.Length);
    }
}
