namespace SortingLab.Core.Words;

/// <summary>
/// Алгоритмы сортировки слов, доступные в программе.
/// Каждый алгоритм из списка появляется в выборе алгоритма и получает свой столбец в таблице замеров.
/// </summary>
public static class WordSorters
{
    public static IReadOnlyList<IWordSorter> All { get; } =
    [
        new QuickWordSorter(),
        new RadixWordSorter(),
    ];
}