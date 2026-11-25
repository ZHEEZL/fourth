using SortingLab.Core.Visual;

namespace SortingLab.Core.External;

/// <summary>Визуальное состояние одной записи внутри файла на текущем шаге.</summary>
public enum RecordVisualState
{
    Normal,
    Consumed,
    Comparing,
    Moved,
    Sorted,
}

/// <summary>Снимок одной записи в файле для показа в интерфейсе.</summary>
public sealed record FileRecordSnapshot(
    string KeyText,
    string Subtitle,
    string Tooltip,
    int RunIndex,
    RecordVisualState State);

/// <summary>Снимок содержимого одного файла (основного или вспомогательного).</summary>
public sealed record TapeSnapshot(
    string Name,
    string Role,
    bool IsAuxiliary,
    IReadOnlyList<FileRecordSnapshot> Items);

/// <summary>Один шаг пошагового показа внешней сортировки.</summary>
public sealed record ExternalSortStep
{
    public required StepKind Kind { get; init; }
    public string? Log { get; init; }
    public required string Explanation { get; init; }
    public required IReadOnlyList<TapeSnapshot> Tapes { get; init; }

    /// <summary>Итоговые отсортированные записи (заполняется на шаге <see cref="StepKind.Done"/>).</summary>
    public IReadOnlyList<TableRecord>? Result { get; init; }
}