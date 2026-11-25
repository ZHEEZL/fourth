namespace SortingLab.App.ViewModels;

/// <summary>Модель главного окна: по одной модели на вкладку.</summary>
public sealed class MainWindowViewModel
{
    public InternalSortViewModel InternalSort { get; } = new();

    public ExternalSortViewModel ExternalSort { get; } = new();

    public WordSortViewModel WordSort { get; } = new();
}