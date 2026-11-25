using System.Text;

namespace SortingLab.Core.External;

/// <summary>
/// Рабочий файл внешней сортировки (основной или вспомогательный).
/// Реально записывает строки во временный CSV-файл на диске и хранит состояние для снимка в UI.
/// </summary>
internal sealed class TapeFile
{
    private readonly CsvTable _table;
    private readonly List<(TableRecord Record, int RunIndex)> _entries = [];

    public TapeFile(string name, string role, bool isAuxiliary, string directory, CsvTable table)
    {
        Name = name;
        Role = role;
        IsAuxiliary = isAuxiliary;
        FilePath = Path.Combine(directory, $"{name}.csv");
        _table = table;
        Clear();
    }

    public string Name { get; }
    public string Role { get; }
    public bool IsAuxiliary { get; }
    public string FilePath { get; }
    public int ReadPosition { get; set; }
    public int Count => _entries.Count;
    public int Remaining => Math.Max(0, _entries.Count - ReadPosition);
    public bool HasMore => ReadPosition < _entries.Count;

    public (TableRecord Record, int RunIndex) Peek() => _entries[ReadPosition];

    public (TableRecord Record, int RunIndex) Consume() => _entries[ReadPosition++];

    public void Clear()
    {
        _entries.Clear();
        ReadPosition = 0;
        File.WriteAllText(FilePath, "", Encoding.UTF8);
    }

    public void Load(IReadOnlyList<TableRecord> records, int runIndex = 1)
    {
        _entries.Clear();
        ReadPosition = 0;
        var sb = new StringBuilder();
        foreach (var record in records)
        {
            _entries.Add((record, runIndex));
            sb.AppendLine(_table.FormatRecordLine(record));
        }
        File.WriteAllText(FilePath, sb.ToString(), Encoding.UTF8);
    }

    public void Append(TableRecord record, int runIndex)
    {
        _entries.Add((record, runIndex));
        File.AppendAllText(FilePath, _table.FormatRecordLine(record) + Environment.NewLine, Encoding.UTF8);
    }

    public IReadOnlyList<TableRecord> GetRecords() => _entries.Select(e => e.Record).ToArray();

    public TapeSnapshot ToSnapshot(
        RecordKeyComparer comparer,
        int? comparingIndex = null,
        int? movedIndex = null,
        bool allSorted = false)
    {
        var items = new FileRecordSnapshot[_entries.Count];
        for (int i = 0; i < _entries.Count; i++)
        {
            var (rec, run) = _entries[i];
            var state = allSorted ? RecordVisualState.Sorted
                : i == movedIndex ? RecordVisualState.Moved
                : i == comparingIndex ? RecordVisualState.Comparing
                : i < ReadPosition ? RecordVisualState.Consumed
                : RecordVisualState.Normal;

            items[i] = new FileRecordSnapshot(
                KeyText: comparer.FormatKey(rec),
                Subtitle: _table.GetSubtitle(rec, comparer.KeyColumnIndex),
                Tooltip: _table.FormatTooltip(rec),
                RunIndex: run,
                State: state);
        }

        return new TapeSnapshot(Name, Role, IsAuxiliary, items);
    }

    public static string CreateWorkDirectory()
    {
        string dir = Path.Combine(Path.GetTempPath(), "SortingLab_External", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static void CleanupWorkDirectory(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
        catch
        {
            // Игнорируем временную блокировку папки системой.
        }
    }
}