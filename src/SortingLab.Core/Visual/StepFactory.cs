namespace SortingLab.Core.Visual;

/// <summary>Общие заготовки для шагов сортировки.</summary>
internal static class StepFactory
{
    /// <summary>Индексы от <paramref name="from"/> до <paramref name="to"/> включительно.</summary>
    public static int[] Indices(int from, int to) =>
        from > to ? [] : Enumerable.Range(from, to - from + 1).ToArray();

    /// <summary>Последний шаг: все элементы на своих местах.</summary>
    public static SortStep Done(int length) => new()
    {
        Kind = StepKind.Done,
        Log = "Сортировка закончена",
        Explanation = "Все числа стоят по возрастанию.",
        NewlySorted = Indices(0, length - 1),
    };
}
