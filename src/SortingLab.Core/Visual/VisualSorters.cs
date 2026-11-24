namespace SortingLab.Core.Visual;

/// <summary>Алгоритмы, доступные для пошагового показа.</summary>
public static class VisualSorters
{
    /// <summary>Две квадратичные и две усовершенствованные сортировки.</summary>
    public static IReadOnlyList<IVisualSorter> All { get; } =
    [
        new BubbleSorter(),
        new InsertionSorter(),
        new QuickSorter(),
        new HeapSorter(),
    ];
}
