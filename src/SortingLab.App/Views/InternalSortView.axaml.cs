using System.Collections.Specialized;
using Avalonia.Controls;
using SortingLab.App.ViewModels;

namespace SortingLab.App.Views;

public partial class InternalSortView : UserControl
{
    private InternalSortViewModel? _viewModel;

    public InternalSortView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => AttachViewModel(DataContext as InternalSortViewModel);
    }

    private void AttachViewModel(InternalSortViewModel? viewModel)
    {
        if (_viewModel is not null)
            _viewModel.Log.CollectionChanged -= OnLogChanged;

        _viewModel = viewModel;

        if (_viewModel is not null)
            _viewModel.Log.CollectionChanged += OnLogChanged;
    }

    // Журнал всегда прокручен к последней записи.
    private void OnLogChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && _viewModel is { Log.Count: > 0 } viewModel)
            LogList.ScrollIntoView(viewModel.Log.Count - 1);
    }
}
