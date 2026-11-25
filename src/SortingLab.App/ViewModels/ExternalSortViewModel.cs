using Avalonia.Media;
using Avalonia.Media.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SortingLab.App.Controls;
using SortingLab.Core;
using SortingLab.Core.External;
using SortingLab.Core.Visual;

namespace SortingLab.App.ViewModels;

/// <summary>Карточка одной записи внутри файла для отображения в интерфейсе.</summary>
public sealed record RecordChipViewItem(
    string KeyText,
    string Subtitle,
    bool HasSubtitle,
    string Tooltip,
    bool ShowRunSeparator,
    IBrush Background,
    IBrush BorderBrush,
    IBrush Foreground,
    double Opacity);

/// <summary>Блок одного файла (основного или вспомогательного) с его текущими записями.</summary>
public sealed record TapeViewItem(
    string Name,
    string Role,
    string CountText,
    bool IsEmpty,
    IReadOnlyList<RecordChipViewItem> Items);

/// <summary>Модель вкладки «Внешняя сортировка» (Задание 2).</summary>
public partial class ExternalSortViewModel : StepPlaybackViewModelBase
{
    private const string NoFilterOption = "Без фильтра";

    private static readonly IImmutableBrush NormalBg = new ImmutableSolidColorBrush(Color.Parse("#F4F4F5"));
    private static readonly IImmutableBrush NormalBorder = new ImmutableSolidColorBrush(Color.Parse("#D4D4D8"));
    private static readonly IImmutableBrush NormalText = new ImmutableSolidColorBrush(Color.Parse("#171717"));

    private static readonly IImmutableBrush ConsumedBg = new ImmutableSolidColorBrush(Color.Parse("#FAFAFA"));
    private static readonly IImmutableBrush ConsumedBorder = new ImmutableSolidColorBrush(Color.Parse("#E4E4E7"));
    private static readonly IImmutableBrush ConsumedText = new ImmutableSolidColorBrush(Color.Parse("#737373"));

    private static readonly IImmutableBrush CompareBg = new ImmutableSolidColorBrush(Color.Parse("#DBEAFE"));
    private static readonly IImmutableBrush CompareText = new ImmutableSolidColorBrush(Color.Parse("#1E3A8A"));

    private static readonly IImmutableBrush MoveBg = new ImmutableSolidColorBrush(Color.Parse("#FEE2E2"));
    private static readonly IImmutableBrush MoveText = new ImmutableSolidColorBrush(Color.Parse("#991B1B"));

    private static readonly IImmutableBrush SortedBg = new ImmutableSolidColorBrush(Color.Parse("#DCFCE7"));
    private static readonly IImmutableBrush SortedText = new ImmutableSolidColorBrush(Color.Parse("#166534"));

    private CsvTable _table = SampleTables.Countries();
    private IReadOnlyList<TableRecord> _filteredRecords = [];
    private IReadOnlyList<TableRecord> _sortedResult = [];
    private IEnumerator<ExternalSortStep>? _steps;
    private bool _suppressSelectionCallbacks;
    private string? _autoOutputPath;

    [ObservableProperty]
    private IExternalSorter _selectedAlgorithm;

    [ObservableProperty]
    private IReadOnlyList<string> _columns = [];

    [ObservableProperty]
    private int _selectedKeyColumnIndex;

    [ObservableProperty]
    private IReadOnlyList<string> _filterColumns = [];

    [ObservableProperty]
    private int _selectedFilterColumnIndex;

    [ObservableProperty]
    private IReadOnlyList<string> _filterValues = [];

    [ObservableProperty]
    private string? _selectedFilterValue;

    [ObservableProperty]
    private bool _isFilterValueEnabled;

    [ObservableProperty]
    private string _tableStatus = "";

    [ObservableProperty]
    private string _outputStatus = "";

    [ObservableProperty]
    private bool _hasSortedResult;

    [ObservableProperty]
    private IReadOnlyList<TapeViewItem> _tapes = [];

    public ExternalSortViewModel()
    {
        _selectedAlgorithm = Algorithms[0];
        UseCountries();
    }

    public IReadOnlyList<IExternalSorter> Algorithms { get; } = ExternalSorters.All;

    protected override ISorterInfo CurrentAlgorithmInfo => SelectedAlgorithm;
    public override string MovesLabel => "Записей в файлы";
    protected override string MovesSummaryWord => "записей в файлы";
    protected override string MoveStepTitle => "Запись в файл";

    public string SuggestedResultFileName => $"{_table.Name}_sorted.csv";

    partial void OnSelectedAlgorithmChanged(IExternalSorter value)
    {
        NotifyAlgorithmChanged();
        ResetPlayback();
    }

    partial void OnSelectedKeyColumnIndexChanged(int value)
    {
        if (!_suppressSelectionCallbacks && value >= 0 && value < _table.Headers.Count)
            ResetPlayback();
    }

    partial void OnSelectedFilterColumnIndexChanged(int value)
    {
        if (_suppressSelectionCallbacks)
            return;

        UpdateFilterValues(defaultValue: null);
        ResetPlayback();
    }

    partial void OnSelectedFilterValueChanged(string? value)
    {
        if (!_suppressSelectionCallbacks)
            ResetPlayback();
    }

    [RelayCommand]
    private void UseCountries() =>
        LoadTable(SampleTables.Countries(), defaultKeyColumn: "Население", defaultFilterColumn: "Континент", defaultFilterValue: "Европа");

    [RelayCommand]
    private void UseSubstances() =>
        LoadTable(SampleTables.Substances(), defaultKeyColumn: "Молекулярная масса", defaultFilterColumn: "Класс", defaultFilterValue: "Кислота");

    [RelayCommand]
    private void UseRussianWords() =>
        LoadTable(SampleTables.RussianWords(), defaultKeyColumn: "Слово", defaultFilterColumn: null, defaultFilterValue: null);

    /// <summary>Загружает произвольную таблицу из открытого пользователем CSV-файла.</summary>
    public void LoadFromCsvContent(string content, string tableName)
    {
        try
        {
            var parsed = CsvTable.Parse(content, tableName);
            ErrorMessage = null;
            LoadTable(parsed, defaultKeyColumn: null, defaultFilterColumn: null, defaultFilterValue: null);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка чтения CSV: {ex.Message}";
        }
    }

    /// <summary>Собирает отсортированные записи в строку CSV для сохранения на диск.</summary>
    public string BuildResultCsv() => _table.ToCsv(_sortedResult);

    private void LoadTable(CsvTable table, string? defaultKeyColumn, string? defaultFilterColumn, string? defaultFilterValue)
    {
        _suppressSelectionCallbacks = true;
        try
        {
            _table = table;
            Columns = table.Headers;

            int keyIdx = defaultKeyColumn is not null
                ? FindColumnIndex(table.Headers, defaultKeyColumn)
                : 0;
            SelectedKeyColumnIndex = keyIdx >= 0 ? keyIdx : 0;

            FilterColumns = table.Headers.Prepend(NoFilterOption).ToArray();
            int filterIdx = defaultFilterColumn is not null
                ? FindColumnIndex(table.Headers, defaultFilterColumn) + 1
                : 0;
            SelectedFilterColumnIndex = filterIdx > 0 ? filterIdx : 0;

            UpdateFilterValues(defaultFilterValue);
        }
        finally
        {
            _suppressSelectionCallbacks = false;
        }

        ResetPlayback();
    }

    private void UpdateFilterValues(string? defaultValue)
    {
        if (SelectedFilterColumnIndex <= 0 || SelectedFilterColumnIndex - 1 >= _table.Headers.Count)
        {
            IsFilterValueEnabled = false;
            FilterValues = [];
            SelectedFilterValue = null;
            return;
        }

        int col = SelectedFilterColumnIndex - 1;
        var distinct = _table.GetDistinctValues(col);
        FilterValues = distinct;
        IsFilterValueEnabled = distinct.Count > 0;
        SelectedFilterValue = defaultValue is not null && distinct.Contains(defaultValue, StringComparer.OrdinalIgnoreCase)
            ? distinct.First(v => string.Equals(v, defaultValue, StringComparison.OrdinalIgnoreCase))
            : distinct.FirstOrDefault();
    }

    protected override bool AdvanceStep()
    {
        if (_steps is null)
        {
            var comparer = _table.CreateComparer(SelectedKeyColumnIndex, _filteredRecords);
            _autoOutputPath = Path.Combine(AppContext.BaseDirectory, "output", SuggestedResultFileName);
            _steps = SelectedAlgorithm.Sort(_table, _filteredRecords, comparer, _autoOutputPath).GetEnumerator();
        }

        if (!_steps.MoveNext())
        {
            IsFinished = true;
            return false;
        }

        var step = _steps.Current;
        RecordStep(step.Kind, step.Log, step.Explanation);
        Tapes = BuildTapeViewItems(step.Tapes);

        if (step.Kind == StepKind.Done)
        {
            _sortedResult = step.Result ?? [];
            HasSortedResult = _sortedResult.Count > 0;
            if (!string.IsNullOrEmpty(_autoOutputPath) && HasSortedResult)
                OutputStatus = $"Результат записан в файл: {_autoOutputPath}";
            IsFinished = true;
            return false;
        }

        return true;
    }

    protected override void ResetPlayback()
    {
        ResetBaseState("Нажмите «Старт» для автоматического показа или «Шаг», чтобы увидеть каждый перенос между файлами.");
        _steps?.Dispose();
        _steps = null;
        _sortedResult = [];
        HasSortedResult = false;
        OutputStatus = "";

        int? filterCol = SelectedFilterColumnIndex > 0 ? SelectedFilterColumnIndex - 1 : null;
        _filteredRecords = _table.Filter(filterCol, SelectedFilterValue);
        CanRun = _filteredRecords.Count > 0;

        int keyIdx = Math.Clamp(SelectedKeyColumnIndex, 0, Math.Max(0, _table.Headers.Count - 1));
        var comparer = _table.CreateComparer(keyIdx, _filteredRecords);
        string sortType = comparer.IsNumeric ? "как числа" : "по алфавиту";

        TableStatus = filterCol is null
            ? $"Таблица «{_table.Name}»: {_filteredRecords.Count} {RussianPlural.Choose(_filteredRecords.Count, "запись", "записи", "записей")}. Ключ: «{comparer.KeyColumnName}» ({sortType})."
            : $"Таблица «{_table.Name}»: отобрано {_filteredRecords.Count} из {_table.Records.Count} ({_table.Headers[filterCol.Value]} = {SelectedFilterValue}). Ключ: «{comparer.KeyColumnName}» ({sortType}).";

        // Начальный снимок: все отобранные записи лежат в основном файле A, вспомогательные пусты.
        var initialItems = _filteredRecords.Select(r => new FileRecordSnapshot(
            KeyText: comparer.FormatKey(r),
            Subtitle: _table.GetSubtitle(r, keyIdx),
            Tooltip: _table.FormatTooltip(r),
            RunIndex: 1,
            State: RecordVisualState.Normal)).ToArray();

        var initialTapes = new List<TapeSnapshot>
        {
            new("A", "Основной файл", IsAuxiliary: false, initialItems),
        };

        if (SelectedAlgorithm is MultiwayMergeSorter)
        {
            initialTapes.Add(new("F1", "Вспомогательный файл 1", IsAuxiliary: true, []));
            initialTapes.Add(new("F2", "Вспомогательный файл 2", IsAuxiliary: true, []));
            initialTapes.Add(new("F3", "Вспомогательный файл 3", IsAuxiliary: true, []));
        }
        else
        {
            initialTapes.Add(new("B", "Вспомогательный файл 1", IsAuxiliary: true, []));
            initialTapes.Add(new("C", "Вспомогательный файл 2", IsAuxiliary: true, []));
        }

        Tapes = BuildTapeViewItems(initialTapes);
    }

    private static IReadOnlyList<TapeViewItem> BuildTapeViewItems(IReadOnlyList<TapeSnapshot> snapshots)
    {
        var result = new TapeViewItem[snapshots.Count];
        for (int t = 0; t < snapshots.Count; t++)
        {
            var snap = snapshots[t];
            var chips = new RecordChipViewItem[snap.Items.Count];
            for (int i = 0; i < snap.Items.Count; i++)
            {
                var item = snap.Items[i];
                bool showSep = i > 0 && snap.Items[i - 1].RunIndex != item.RunIndex;
                var (bg, border, fg, opacity) = item.State switch
                {
                    RecordVisualState.Comparing => (CompareBg, BarChart.CompareBrush, CompareText, 1.0),
                    RecordVisualState.Moved => (MoveBg, BarChart.MoveBrush, MoveText, 1.0),
                    RecordVisualState.Sorted => (SortedBg, BarChart.SortedBrush, SortedText, 1.0),
                    RecordVisualState.Consumed => (ConsumedBg, ConsumedBorder, ConsumedText, 0.45),
                    _ => (NormalBg, NormalBorder, NormalText, 1.0),
                };

                chips[i] = new RecordChipViewItem(
                    KeyText: item.KeyText,
                    Subtitle: item.Subtitle,
                    HasSubtitle: !string.IsNullOrEmpty(item.Subtitle),
                    Tooltip: item.Tooltip,
                    ShowRunSeparator: showSep,
                    Background: bg,
                    BorderBrush: border,
                    Foreground: fg,
                    Opacity: opacity);
            }

            string countText = snap.Items.Count == 0
                ? "пусто"
                : $"{snap.Items.Count} {RussianPlural.Choose(snap.Items.Count, "запись", "записи", "записей")}";

            result[t] = new TapeViewItem(
                Name: $"Файл {snap.Name}",
                Role: snap.Role,
                CountText: countText,
                IsEmpty: snap.Items.Count == 0,
                Items: chips);
        }

        return result;
    }

    private static int FindColumnIndex(IReadOnlyList<string> headers, string name)
    {
        for (int i = 0; i < headers.Count; i++)
        {
            if (string.Equals(headers[i], name, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }
}