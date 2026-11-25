namespace SortingLab.Core;

//Общее описание алгоритма сортировки для карточки в интерфейсе.
public interface ISorterInfo
{
    //Название алгоритма.
    string Name { get; }

    //Группа или вид алгоритма.
    string Group { get; }

    //Идея алгоритма простыми словами.
    string Idea { get; }

    //Оценка времени работы и дополнительной памяти.
    string Complexity { get; }
}