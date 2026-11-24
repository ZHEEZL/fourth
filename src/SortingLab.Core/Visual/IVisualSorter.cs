namespace SortingLab.Core.Visual;

/// <summary>Алгоритм внутренней сортировки, который показывает свою работу по шагам.</summary>
public interface IVisualSorter
{
    /// <summary>Название алгоритма.</summary>
    string Name { get; }

    /// <summary>Группа алгоритма: квадратичные или усовершенствованные.</summary>
    string Group { get; }

    /// <summary>Идея алгоритма простыми словами.</summary>
    string Idea { get; }

    /// <summary>Оценка времени работы и дополнительной памяти.</summary>
    string Complexity { get; }

    /// <summary>
    /// Сортирует массив по возрастанию на месте и по ходу работы возвращает шаги.
    /// Сортировка продвигается только тогда, когда вызывающий код запрашивает следующий шаг.
    /// </summary>
    IEnumerable<SortStep> Sort(int[] array);
}
