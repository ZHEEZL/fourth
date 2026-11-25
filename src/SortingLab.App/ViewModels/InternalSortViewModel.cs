using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SortingLab.App.Controls;
using SortingLab.Core;
using SortingLab.Core.Visual;

namespace SortingLab.App.ViewModels;

/// <summary>Модель вкладки с пошаговым показом внутренней сортировки.</summary>
public partial class InternalSortViewModel : StepPlaybackViewModelBase
{
    private const int MinLength = 2;
    private const int MaxLength = 40;
    private const int MaxValue = 999;

    private readonly Random _random = new();
    private readonly HashSet<int> _sorted = [];
    private int[] _initial = [];
    private int[] _work = [];
    private IEnumerator<SortStep>? _steps;

    [ObservableProperty]
    private IVisualSorter _selectedAlgorithm;

    [ObservableProperty]
    private decimal _arraySize = 16;

    [ObservableProperty]
    private string _customArray = "";

    [ObservableProperty]
    private ChartFrame _frame = ChartFrame.Initial([]);

    public InternalSortViewModel()
    {
        _selectedAlgorithm = Algorithms[0];
        GenerateRandom();
    }

    public IReadOnlyList<IVisualSorter> Algorithms { get; } = VisualSorters.All;

    protected override ISorterInfo CurrentAlgorithmInfo => SelectedAlgorithm;

    partial void OnSelectedAlgorithmChanged(IVisualSorter value)
    {
        NotifyAlgorithmChanged();
        ResetPlayback();
    }

    [RelayCommand]
    private void GenerateRandom()
    {
        int n = Math.Clamp((int)ArraySize, MinLength, MaxLength);
        _initial = new int[n];
        for (int i = 0; i < n; i++)
            _initial[i] = _random.Next(1, 100);

        CustomArray = string.Join(" ", _initial);
        ErrorMessage = null;
        ResetPlayback();
    }

    [RelayCommand]
    private void ApplyCustom()
    {
        var parts = CustomArray.Split([' ', ',', ';', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var values = new List<int>(parts.Length);
        foreach (string part in parts)
        {
            if (!int.TryParse(part, out int value) || value < 0 || value > MaxValue)
            {
                ErrorMessage = $"«{part}» не подходит: нужны целые числа от 0 до {MaxValue}.";
                return;
            }
            values.Add(value);
        }

        if (values.Count < MinLength || values.Count > MaxLength)
        {
            ErrorMessage = $"Нужно от {MinLength} до {MaxLength} чисел, а введено {values.Count}.";
            return;
        }

        _initial = values.ToArray();
        ArraySize = values.Count;
        ErrorMessage = null;
        ResetPlayback();
    }

    protected override bool AdvanceStep()
    {
        _steps ??= SelectedAlgorithm.Sort(_work).GetEnumerator();
        if (!_steps.MoveNext())
        {
            IsFinished = true;
            return false;
        }

        var step = _steps.Current;
        foreach (int index in step.NewlySorted)
            _sorted.Add(index);

        RecordStep(step.Kind, step.Log, step.Explanation);

        Frame = new ChartFrame(
            (int[])_work.Clone(),
            step.Kind,
            step.First,
            step.Second,
            new HashSet<int>(_sorted),
            step.Pivot,
            step.RangeStart,
            step.RangeEnd,
            step.Hole,
            step.Held);

        if (step.Kind == StepKind.Done)
        {
            IsFinished = true;
            return false;
        }
        return true;
    }

    protected override void ResetPlayback()
    {
        ResetBaseState();
        _steps?.Dispose();
        _steps = null;
        _work = (int[])_initial.Clone();
        _sorted.Clear();
        Frame = ChartFrame.Initial((int[])_work.Clone());
    }
}