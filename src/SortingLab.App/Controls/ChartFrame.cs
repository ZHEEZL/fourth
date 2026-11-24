using SortingLab.Core.Visual;

namespace SortingLab.App.Controls;

/// <summary>Снимок массива и подсветки для одного кадра графика.</summary>
/// <param name="Values">Копия массива на момент шага.</param>
/// <param name="Kind">Вид шага: от него зависит цвет выделенных столбиков.</param>
/// <param name="First">Первый выделенный индекс или -1.</param>
/// <param name="Second">Второй выделенный индекс или -1.</param>
/// <param name="Sorted">Индексы элементов, которые уже стоят на окончательных местах.</param>
/// <param name="Pivot">Индекс опорного элемента или -1.</param>
/// <param name="RangeStart">Начало рабочей части или -1.</param>
/// <param name="RangeEnd">Конец рабочей части или -1.</param>
/// <param name="Hole">Индекс свободного места или -1.</param>
/// <param name="Held">Значение «в руке», которое рисуется на месте <paramref name="Hole"/>.</param>
public sealed record ChartFrame(
    int[] Values,
    StepKind Kind,
    int First,
    int Second,
    IReadOnlySet<int> Sorted,
    int Pivot,
    int RangeStart,
    int RangeEnd,
    int Hole,
    int? Held)
{
    /// <summary>Кадр без подсветки.</summary>
    public static ChartFrame Initial(int[] values) =>
        new(values, StepKind.Info, -1, -1, new HashSet<int>(), -1, -1, -1, -1, null);
}
