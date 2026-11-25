using System.Collections.Specialized;
using Avalonia.Controls;
using SortingLab.App.ViewModels;

namespace SortingLab.App.Controls;

public partial class LogSidebar : UserControl
{
    private StepPlaybackViewModelBase? _viewModel;

    public LogSidebar()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => AttachViewModel(DataContext as StepPlaybackViewModelBase);
    }

    private void AttachViewModel(StepPlaybackViewModelBase? viewModel)
    {
        if (_viewModel is not null)
            _viewModel.Log.CollectionChanged -= OnLogChanged;

        _viewModel = viewModel;

        if (_viewModel is not null)
            _viewModel.Log.CollectionChanged += OnLogChanged;
    }

    private void OnLogChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && _viewModel is { Log.Count: > 0 } vm)
            LogList.ScrollIntoView(vm.Log.Count - 1);
    }
}