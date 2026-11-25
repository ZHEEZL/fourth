using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SortingLab.Core;
using SortingLab.Core.Words;

namespace SortingLab.App.ViewModels;

/// <summary>Строка таблицы замеров, уже отформатированная для показа.</summary>
public sealed record TableRow(string Words, IReadOnlyList<string> Times);

/// <summary>Модель вкладки с сортировкой слов, подсчётом частот и замерами времени.</summary>
public partial class WordSortViewModel : ObservableObject
{
    private const int MaxSize = 5_000_000;

    [ObservableProperty]
    private IWordSorter _selectedSorter;

    [ObservableProperty]
    private string _inputText = SampleText.English;

    [ObservableProperty]
    private IReadOnlyList<string> _sortedWords = [];

    [ObservableProperty]
    private IReadOnlyList<WordFrequency> _wordFrequencies = [];

    [ObservableProperty]
    private bool _showFrequencies = true;

    [ObservableProperty]
    private bool _showAllWords;

    [ObservableProperty]
    private string _sortSummary = "Нажмите «Разбить и отсортировать».";

    [ObservableProperty]
    private string _sizesText = "100, 500, 1000, 2000, 5000, 10000, 20000";

    [ObservableProperty]
    private decimal _repeats = 5;

    [ObservableProperty]
    private bool _takeFirstWords = true;

    [ObservableProperty]
    private bool _takeRandomWords;

    [ObservableProperty]
    private string _experimentStatus = "";

    [ObservableProperty]
    private IReadOnlyList<string> _timeHeaders = [];

    private IReadOnlyList<ExperimentRow> _lastRows = [];

    [ObservableProperty]
    private bool _hasTable;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunExperimentsCommand))]
    private bool _isBusy;

    public WordSortViewModel()
    {
        _selectedSorter = Sorters[0];
        _timeHeaders = Sorters.Select(s => $"{s.Name}, мс").ToArray();
    }

    public IReadOnlyList<IWordSorter> Sorters { get; } = WordSorters.All;

    public ObservableCollection<TableRow> TableRows { get; } = [];

    [RelayCommand]
    private void UseSample() => InputText = SampleText.English;

    /// <summary>Разбивает текст на слова, сортирует их и подсчитывает частоту каждого слова.</summary>
    [RelayCommand]
    private void SortText()
    {
        string[] words = WordTokenizer.Split(InputText);
        long start = Stopwatch.GetTimestamp();
        SelectedSorter.Sort(words);
        double ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

        var frequencies = WordFrequencyCounter.CountSorted(words);
        SortedWords = words;
        WordFrequencies = frequencies;

        SortSummary = words.Length == 0
            ? "В тексте не нашлось ни одного слова."
            : $"Слов: {words.Length}, уникальных: {frequencies.Count}. Сортировка заняла {ms:0.###} мс.";
    }

    private bool CanRunExperiments() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanRunExperiments))]
    private async Task RunExperimentsAsync()
    {
        if (!TryParseSizes(SizesText, out var sizes, out string? error))
        {
            ExperimentStatus = error;
            return;
        }

        string[] source = WordTokenizer.Split(InputText);
        if (source.Length == 0)
        {
            ExperimentStatus = "В тексте слева нет слов, брать их для замеров неоткуда.";
            return;
        }

        int repeats = Math.Max(1, (int)Repeats);
        bool takeFirst = TakeFirstWords;
        var sorters = Sorters;
        Func<int, string[]> makeWords = takeFirst
            ? n => WordSource.TakeCyclic(source, n)
            : n => WordSource.RandomSample(source, n, seed: n);

        IsBusy = true;
        ExperimentStatus = "Идут замеры…";
        try
        {
            var progress = new Progress<string>(message => ExperimentStatus = message);
            var rows = await Task.Run(() => ExperimentRunner.Run(sorters, sizes, makeWords, repeats, progress));

            _lastRows = rows;
            TableRows.Clear();
            foreach (var row in rows)
            {
                TableRows.Add(new TableRow(
                    row.WordCount.ToString("N0", CultureInfo.CurrentCulture),
                    row.AverageMs.Select(ms => ms.ToString("0.000", CultureInfo.CurrentCulture)).ToArray()));
            }
            HasTable = TableRows.Count > 0;
            ExperimentStatus =
                $"Готово. В исходном тексте {source.Length} {RussianPlural.Choose(source.Length, "слово", "слова", "слов")}. " +
                $"В таблице среднее время по {repeats} {RussianPlural.Choose(repeats, "запуску", "запускам", "запускам")}.";
        }
        catch (Exception ex)
        {
            ExperimentStatus = $"Замеры прерваны: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public string BuildCsv()
    {
        var culture = CultureInfo.CurrentCulture;
        var csv = new StringBuilder();
        csv.AppendLine(string.Join(';', TimeHeaders.Prepend("Слов")));
        foreach (var row in _lastRows)
        {
            var times = row.AverageMs.Select(ms => ms.ToString("0.000", culture));
            csv.AppendLine(string.Join(';', times.Prepend(row.WordCount.ToString(culture))));
        }
        return csv.ToString();
    }

    private static bool TryParseSizes(string text, out List<int> sizes, [NotNullWhen(false)] out string? error)
    {
        sizes = [];
        error = null;
        foreach (string part in text.Split([' ', ',', ';', '\t'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (!int.TryParse(part, out int size) || size < 1 || size > MaxSize)
            {
                error = $"«{part}» не подходит: размер текста должен быть целым числом от 1 до {MaxSize}.";
                return false;
            }
            sizes.Add(size);
        }

        if (sizes.Count == 0)
        {
            error = "Укажите хотя бы один размер текста.";
            return false;
        }
        return true;
    }
}