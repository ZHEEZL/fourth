namespace SortingLab.Core;

/// <summary>Общее описание алгоритма сортировки для карточки в интерфейсе.</summary>
public interface ISorterInfo
{
    /// <summary>Название алгоритма.</summary>
    string Name { get; }

    /// <summary>Группа или вид алгоритма.</summary>
    string Group { get; }

    /// <summary>Идея алгоритма простыми словами (для шестиклассника).</summary>
    string Idea { get; }

    /// <summary>Оценка времени работы и дополнительной памяти.</summary>
    string Complexity { get; }
}