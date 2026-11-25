namespace SortingLab.Core.External;

/// <summary>Алгоритм внешней сортировки таблицы с пошаговым выводом содержимого файлов.</summary>
public interface IExternalSorter : ISorterInfo
{
    /// <summary>
    /// Сортирует выбранные записи таблицы с помощью временных вспомогательных файлов.
    /// Если задан <paramref name="outputPath"/>, итоговый результат сохраняется в этот CSV-файл.
    /// </summary>
    IEnumerable<ExternalSortStep> Sort(
        CsvTable table,
        IReadOnlyList<TableRecord> records,
        RecordKeyComparer comparer,
        string? outputPath = null);
}