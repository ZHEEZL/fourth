namespace SortingLab.Core.External;

/// <summary>Доступные алгоритмы внешней сортировки.</summary>
public static class ExternalSorters
{
    public static IReadOnlyList<IExternalSorter> All { get; } =
    [
        new DirectMergeSorter(),
        new NaturalMergeSorter(),
        new MultiwayMergeSorter(),
    ];
}