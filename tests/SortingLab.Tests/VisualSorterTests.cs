using SortingLab.Core.Visual;

namespace SortingLab.Tests;

public class VisualSorterTests
{
    public static IEnumerable<object[]> SorterNames => VisualSorters.All.Select(s => new object[] { s.Name });

    [Theory]
    [MemberData(nameof(SorterNames))]
    public void SortsRandomArraysWithDuplicates(string name)
    {
        var sorter = Find(name);
        var random = new Random(42);
        for (int n = 0; n <= 40; n++)
        {
            for (int attempt = 0; attempt < 25; attempt++)
            {
                // Небольшой диапазон значений даёт много повторов.
                int[] array = Enumerable.Range(0, n).Select(_ => random.Next(0, 5 + n)).ToArray();
                CheckSorts(sorter, array);
            }
        }
    }

    [Theory]
    [MemberData(nameof(SorterNames))]
    public void SortsSpecialArrays(string name)
    {
        var sorter = Find(name);
        CheckSorts(sorter, []);
        CheckSorts(sorter, [7]);
        CheckSorts(sorter, [2, 1]);
        CheckSorts(sorter, [1, 2, 3, 4, 5, 6, 7, 8]);
        CheckSorts(sorter, [8, 7, 6, 5, 4, 3, 2, 1]);
        CheckSorts(sorter, [5, 5, 5, 5, 5, 5]);
        CheckSorts(sorter, [1, 99, 1, 99, 1, 99, 1]);
    }

    [Theory]
    [MemberData(nameof(SorterNames))]
    public void StepsPointInsideArrayAndExplainThemselves(string name)
    {
        var sorter = Find(name);
        int[] array = [42, 17, 8, 99, 23, 4, 15, 16, 61, 3, 50];
        int n = array.Length;
        var steps = sorter.Sort(array).ToList();

        Assert.All(steps, step =>
        {
            Assert.False(string.IsNullOrWhiteSpace(step.Explanation));
            Assert.InRange(step.First, -1, n - 1);
            Assert.InRange(step.Second, -1, n - 1);
            Assert.InRange(step.Pivot, -1, n - 1);
            Assert.All(step.NewlySorted, i => Assert.InRange(i, 0, n - 1));
            if (step.Kind is StepKind.Compare or StepKind.Swap)
            {
                Assert.NotEqual(-1, step.First);
                Assert.NotEqual(-1, step.Second);
                Assert.False(string.IsNullOrWhiteSpace(step.Log));
            }
        });
        Assert.Equal(StepKind.Done, steps[^1].Kind);
    }

    [Fact]
    public void BubbleSortStopsAfterOnePassOnSortedArray()
    {
        var steps = new BubbleSorter().Sort([1, 2, 3, 4, 5]).ToList();

        Assert.Equal(4, steps.Count(s => s.Kind == StepKind.Compare));
        Assert.DoesNotContain(steps, s => s.Kind == StepKind.Swap);
    }

    [Fact]
    public void BubbleSortSkipsTailAfterLastSwap()
    {
        // После первого прохода последний обмен был на позиции 0, поэтому второй проход не нужен.
        var steps = new BubbleSorter().Sort([2, 1, 3, 4, 5]).ToList();

        Assert.Equal(4, steps.Count(s => s.Kind == StepKind.Compare));
        Assert.Single(steps, s => s.Kind == StepKind.Swap);
    }

    [Fact]
    public void InsertionSortDoesNotMoveSortedArray()
    {
        var steps = new InsertionSorter().Sort([1, 2, 3, 4, 5]).ToList();

        Assert.Equal(4, steps.Count(s => s.Kind == StepKind.Compare));
        Assert.DoesNotContain(steps, s => s.Kind == StepKind.Move);
    }

    [Fact]
    public void InsertionSortShiftsAndInserts()
    {
        // 3 остаётся на месте после одного сравнения.
        // 1 сравнивается с 3 и 2 и сдвигает их, затем вставляется в начало: 2 сдвига и 1 вставка.
        var steps = new InsertionSorter().Sort([2, 3, 1]).ToList();

        Assert.Equal(3, steps.Count(s => s.Kind == StepKind.Compare));
        Assert.Equal(3, steps.Count(s => s.Kind == StepKind.Move));
    }

    [Fact]
    public void QuickSortTracksPivotIndex()
    {
        int[] array = [9, 1, 8, 2, 5, 7, 3, 6, 4, 5, 1];
        int pivotValue = -1;
        foreach (var step in new QuickSorter().Sort(array))
        {
            if (step.Pivot < 0)
                continue;

            // Начало разделения: запоминаем опорное значение.
            if (step.Kind == StepKind.Info)
                pivotValue = array[step.Pivot];
            // Пока идёт разделение, по индексу опорного лежит именно оно, даже после обменов.
            else
                Assert.Equal(pivotValue, array[step.Pivot]);
        }
    }

    private static IVisualSorter Find(string name) => VisualSorters.All.Single(s => s.Name == name);

    private static void CheckSorts(IVisualSorter sorter, int[] array)
    {
        int[] expected = array.Order().ToArray();
        var steps = sorter.Sort(array).ToList();

        Assert.Equal(expected, array);
        Assert.Equal(StepKind.Done, steps[^1].Kind);
    }
}
