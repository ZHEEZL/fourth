using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SortingLab.App.Controls;
using SortingLab.Core;
using SortingLab.Core.Visual;

namespace SortingLab.App.ViewModels;

/// <summary>
/// Общая основа для пошагового показа сортировки (задания 1 и 2):
/// кнопки «Старт / Пауза / Шаг / Сначала», ползунок задержки, счётчики, единый журнал и пояснения.
/// </summary>
public abstract partial class StepPlaybackViewModelBase : ObservableObject
{
    protected const string StartTitle = "Начало";

    protected const string DefaultStartHint =
        "Нажмите «Старт», и сортировка пойдёт сама, с задержкой между шагами. " +
        "Кнопка «Шаг» выполняет одно действие и останавливается.";

    private CancellationTokenSource? _runCts;
    private int _logNumber;

    [ObservableProperty]
    private double _delayMs = 600;

    [ObservableProperty]
    private string _explanation = DefaultStartHint;

    [ObservableProperty]
    private string _stepTitle = StartTitle;

    [ObservableProperty]
    private IBrush _stepBrush = BarChart.NormalBrush;

    [ObservableProperty]
    private int _comparisons;

    [ObservableProperty]
    private int _moves;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(PauseCommand), nameof(StepCommand))]
    private bool _isRunning;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(StepCommand))]
    private bool _isFinished;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(StepCommand))]
    private bool _canRun = true;

    /// <summary>Единый журнал сравнений и перестановок/переносов.</summary>
    public ObservableCollection<string> Log { get; } = [];

    protected abstract ISorterInfo CurrentAlgorithmInfo { get; }

    public string AlgorithmName => CurrentAlgorithmInfo.Name;
    public string AlgorithmGroup => CurrentAlgorithmInfo.Group;
    public string AlgorithmIdea => CurrentAlgorithmInfo.Idea;
    public string AlgorithmComplexity => CurrentAlgorithmInfo.Complexity;

    /// <summary>Подпись второго счётчика («Перестановок» или «Переносов»).</summary>
    public virtual string MovesLabel => "Перестановок";

    /// <summary>Подпись счётчика в итоговом предложении.</summary>
    protected virtual string MovesSummaryWord => "перестановок";

    /// <summary>Подпись шага <see cref="StepKind.Move"/> в карточке пояснения.</summary>
    protected virtual string MoveStepTitle => "Перенос";

    private bool CanAdvance() => CanRun && !IsRunning && !IsFinished;

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

    [RelayCommand(CanExecute = nameof(CanAdvance))]
    private void Step() => AdvanceStep();

    [RelayCommand]
    private void Reset() => ResetPlayback();

    protected void NotifyAlgorithmChanged()
    {
        OnPropertyChanged(nameof(AlgorithmName));
        OnPropertyChanged(nameof(AlgorithmGroup));
        OnPropertyChanged(nameof(AlgorithmIdea));
        OnPropertyChanged(nameof(AlgorithmComplexity));
    }

    protected void StopRunning()
    {
        _runCts?.Cancel();
        _runCts = null;
        IsRunning = false;
    }

    /// <summary>Сбрасывает общие счётчики, журнал и состояние воспроизведения.</summary>
    protected void ResetBaseState(string? startHint = null)
    {
        StopRunning();
        Log.Clear();
        _logNumber = 0;
        Comparisons = 0;
        Moves = 0;
        IsFinished = false;
        Explanation = startHint ?? DefaultStartHint;
        StepTitle = StartTitle;
        StepBrush = BarChart.NormalBrush;
    }

    /// <summary>Фиксирует шаг в общем формате лога, обновляет счётчики и карточку пояснения.</summary>
    protected void RecordStep(StepKind kind, string? log, string explanation)
    {
        switch (kind)
        {
            case StepKind.Compare:
                Comparisons++;
                break;
            case StepKind.Swap or StepKind.Move:
                Moves++;
                break;
        }

        if (log is not null)
        {
            bool counted = kind is StepKind.Compare or StepKind.Swap or StepKind.Move;
            Log.Add(counted ? $"{++_logNumber}. {log}" : $"» {log}");
        }

        Explanation = kind == StepKind.Done
            ? $"{explanation} Понадобилось сравнений: {Comparisons}, {MovesSummaryWord}: {Moves}."
            : explanation;

        (string title, IBrush brush) look = kind switch
        {
            StepKind.Compare => ("Сравнение", BarChart.CompareBrush),
            StepKind.Swap => ("Обмен", BarChart.MoveBrush),
            StepKind.Move => (MoveStepTitle, BarChart.MoveBrush),
            StepKind.Done => ("Готово", BarChart.SortedBrush),
            _ => ("Пояснение", BarChart.NormalBrush),
        };
        StepTitle = look.title;
        StepBrush = look.brush;
    }

    private async Task RunLoopAsync(CancellationTokenSource cts)
    {
        try
        {
            while (!cts.IsCancellationRequested && AdvanceStep())
                await Task.Delay(TimeSpan.FromMilliseconds(DelayMs), cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Нажали «Пауза» или сбросили сортировку.
        }
        finally
        {
            if (ReferenceEquals(_runCts, cts))
            {
                _runCts = null;
                IsRunning = false;
            }
            cts.Dispose();
        }
    }

    protected abstract bool AdvanceStep();
    protected abstract void ResetPlayback();
}