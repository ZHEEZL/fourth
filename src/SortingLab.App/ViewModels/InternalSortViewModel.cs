using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SortingLab.App.Controls;
using SortingLab.Core.Visual;

namespace SortingLab.App.ViewModels;

/// <summary>Модель вкладки с пошаговым показом внутренней сортировки.</summary>
public partial class InternalSortViewModel : ObservableObject
{
    private const int MinLength = 2;
    private const int MaxLength = 40;
    private const int MaxValue = 999;

    private const string StartTitle = "Начало";

    private const string StartHint =
        "Нажмите «Старт», и сортировка пойдёт сама, с задержкой между шагами. " +
        "Кнопка «Шаг» выполняет одно действие и останавливается.";

    private readonly Random _random = new();
    private readonly HashSet<int> _sorted = [];
    private int[] _initial = [];
    private int[] _work = [];
    private IEnumerator<SortStep>? _steps;
    private CancellationTokenSource? _runCts;
    private int _logNumber;

    [ObservableProperty]
    private IVisualSorter _selectedAlgorithm;

    [ObservableProperty]
    private decimal _arraySize = 16;

    [ObservableProperty]
    private string _customArray = "";

    [ObservableProperty]
    private string? _inputError;

    [ObservableProperty]
    private double _delayMs = 600;

    [ObservableProperty]
    private ChartFrame _frame = ChartFrame.Initial([]);

    [ObservableProperty]
    private string _explanation = StartHint;

    /// <summary>Вид текущего шага словами: «Сравнение», «Обмен»…</summary>
    [ObservableProperty]
    private string _stepTitle = StartTitle;

    /// <summary>Цвет текущего шага, тот же, что у столбиков на графике.</summary>
    [ObservableProperty]
    private IBrush _stepBrush = BarChart.NormalBrush;

    [ObservableProperty]
    private int _comparisons;

    [ObservableProperty]
    private int _moves;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(PauseCommand), nameof(StepCommand))]
    private bool _isRunning;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(StepCommand))]
    private bool _isFinished;

    public InternalSortViewModel()
    {
        _selectedAlgorithm = Algorithms[0];
        GenerateRandom();
    }

    public IReadOnlyList<IVisualSorter> Algorithms { get; } = VisualSorters.All;

    /// <summary>Журнал сравнений и перестановок.</summary>
    public ObservableCollection<string> Log { get; } = [];

    partial void OnSelectedAlgorithmChanged(IVisualSorter value) => ResetRun();

    /// <summary>Новый случайный массив заданного размера.</summary>
    [RelayCommand]
    private void GenerateRandom()
    {
        int n = Math.Clamp((int)ArraySize, MinLength, MaxLength);
        _initial = new int[n];
        for (int i = 0; i < n; i++)
            _initial[i] = _random.Next(1, 100);

        CustomArray = string.Join(" ", _initial);
        InputError = null;
        ResetRun();
    }

    /// <summary>Массив из поля «Свой массив».</summary>
    [RelayCommand]
    private void ApplyCustom()
    {
        var parts = CustomArray.Split([' ', ',', ';', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var values = new List<int>(parts.Length);
        foreach (string part in parts)
        {
            if (!int.TryParse(part, out int value) || value < 0 || value > MaxValue)
            {
                InputError = $"«{part}» не подходит: нужны целые числа от 0 до {MaxValue}.";
                return;
            }
            values.Add(value);
        }

        if (values.Count < MinLength || values.Count > MaxLength)
        {
            InputError = $"Нужно от {MinLength} до {MaxLength} чисел, а введено {values.Count}.";
            return;
        }

        _initial = values.ToArray();
        ArraySize = values.Count;
        InputError = null;
        ResetRun();
    }

    private bool CanAdvance() => !IsRunning && !IsFinished;

    /// <summary>Запускает автоматический показ с задержкой <see cref="DelayMs"/> между шагами.</summary>
    [RelayCommand(CanExecute = nameof(CanAdvance))]
    private void Start()
    {
        var cts = new CancellationTokenSource();
        _runCts = cts;
        IsRunning = true;
        _ = RunLoopAsync(cts);
    }

    private bool CanPause() => IsRunning;

    [RelayCommand(CanExecute = nameof(CanPause))]
    private void Pause() => StopRunning();

    /// <summary>Выполняет ровно один шаг.</summary>
    [RelayCommand(CanExecute = nameof(CanAdvance))]
    private void Step() => Advance();

    /// <summary>Возвращает исходный массив и начинает сортировку заново.</summary>
    [RelayCommand]
    private void Reset() => ResetRun();

    private async Task RunLoopAsync(CancellationTokenSource cts)
    {
        try
        {
            // Задержка читается на каждом шаге, поэтому ползунок действует сразу.
            while (!cts.IsCancellationRequested && Advance())
                await Task.Delay(TimeSpan.FromMilliseconds(DelayMs), cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Нажали «Пауза» или сбросили сортировку.
        }
        finally
        {
            // Если за это время запустили новый показ, его состояние не трогаем.
            if (ReferenceEquals(_runCts, cts))
            {
                _runCts = null;
                IsRunning = false;
            }
            cts.Dispose();
        }
    }

    private void StopRunning()
    {
        _runCts?.Cancel();
        _runCts = null;
        IsRunning = false;
    }

    /// <summary>Берёт у алгоритма следующий шаг и показывает его.</summary>
    /// <returns><c>false</c>, если сортировка закончилась.</returns>
    private bool Advance()
    {
        _steps ??= SelectedAlgorithm.Sort(_work).GetEnumerator();
        if (!_steps.MoveNext())
        {
            IsFinished = true;
            return false;
        }

        var step = _steps.Current;
        Apply(step);
        if (step.Kind == StepKind.Done)
        {
            IsFinished = true;
            return false;
        }
        return true;
    }

    private void Apply(SortStep step)
    {
        switch (step.Kind)
        {
            case StepKind.Compare:
                Comparisons++;
                break;
            case StepKind.Swap or StepKind.Move:
                Moves++;
                break;
        }

        foreach (int index in step.NewlySorted)
            _sorted.Add(index);

        if (step.Log is not null)
        {
            // Сравнения и перестановки нумеруются, перед служебными строками ставится знак ».
            bool counted = step.Kind is StepKind.Compare or StepKind.Swap or StepKind.Move;
            Log.Add(counted ? $"{++_logNumber}. {step.Log}" : $"» {step.Log}");
        }

        Explanation = step.Kind == StepKind.Done
            ? $"{step.Explanation} Понадобилось сравнений: {Comparisons}, перестановок: {Moves}."
            : step.Explanation;
        (string title, IBrush brush) look = step.Kind switch
        {
            StepKind.Compare => ("Сравнение", BarChart.CompareBrush),
            StepKind.Swap => ("Обмен", BarChart.MoveBrush),
            StepKind.Move => ("Перенос", BarChart.MoveBrush),
            StepKind.Done => ("Готово", BarChart.SortedBrush),
            _ => ("Пояснение", BarChart.NormalBrush),
        };
        StepTitle = look.title;
        StepBrush = look.brush;

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
    }

    private void ResetRun()
    {
        StopRunning();
        _steps?.Dispose();
        _steps = null;
        _work = (int[])_initial.Clone();
        _sorted.Clear();
        Log.Clear();
        _logNumber = 0;
        Comparisons = 0;
        Moves = 0;
        IsFinished = false;
        Frame = ChartFrame.Initial((int[])_work.Clone());
        Explanation = StartHint;
        StepTitle = StartTitle;
        StepBrush = BarChart.NormalBrush;
    }
}
