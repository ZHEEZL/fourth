namespace SortingLab.Core.Visual;

/// <summary>Вид шага сортировки.</summary>
public enum StepKind
{
    /// <summary>Пояснение, массив не меняется.</summary>
    Info,

    /// <summary>Сравнение двух элементов.</summary>
    Compare,

    /// <summary>Обмен двух элементов местами.</summary>
    Swap,

    /// <summary>Перенос одного значения на другое место: сдвиг или вставка.</summary>
    Move,

    /// <summary>Сортировка закончена.</summary>
    Done,
}

/// <summary>Один шаг сортировки для пошагового показа.</summary>
/// <remarks>
/// Алгоритм сначала выполняет действие над массивом и только потом возвращает шаг,
/// поэтому в момент получения шага массив уже показывает его результат.
/// Сравнение массив не меняет: его шаг возвращается до того, как алгоритм примет решение.
/// </remarks>
public sealed record SortStep
{
    /// <summary>Вид шага.</summary>
    public required StepKind Kind { get; init; }

    /// <summary>Строка для журнала или <c>null</c>, если шаг в журнал не пишется.</summary>
    public string? Log { get; init; }

    /// <summary>Пояснение простыми словами.</summary>
    public required string Explanation { get; init; }

    /// <summary>Индекс первого элемента, участвующего в шаге, или -1.</summary>
    public int First { get; init; } = -1;

    /// <summary>Индекс второго элемента, участвующего в шаге, или -1.</summary>
    public int Second { get; init; } = -1;

    /// <summary>Индексы элементов, которые на этом шаге встали на окончательные места.</summary>
    public IReadOnlyList<int> NewlySorted { get; init; } = [];

    /// <summary>Индекс опорного элемента (быстрая сортировка) или -1.</summary>
    public int Pivot { get; init; } = -1;

    /// <summary>Начало части массива, с которой сейчас работает алгоритм, или -1, если работа идёт со всем массивом.</summary>
    public int RangeStart { get; init; } = -1;

    /// <summary>Конец рабочей части массива (включительно) или -1.</summary>
    public int RangeEnd { get; init; } = -1;

    /// <summary>Индекс свободного места (сортировка вставками) или -1.</summary>
    public int Hole { get; init; } = -1;

    /// <summary>Значение, которое алгоритм держит «в руке», пока ищет ему место.</summary>
    public int? Held { get; init; }
}
