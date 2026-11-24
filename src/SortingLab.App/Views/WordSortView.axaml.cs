using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SortingLab.App.ViewModels;

namespace SortingLab.App.Views;

public partial class WordSortView : UserControl
{
    public WordSortView() => InitializeComponent();

    private WordSortViewModel? ViewModel => DataContext as WordSortViewModel;

    /// <summary>Загружает текст из файла в поле слева.</summary>
    private async void OnOpenFileClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } viewModel || TopLevel.GetTopLevel(this) is not { } topLevel)
            return;

        try
        {
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Открыть текст",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Текстовые файлы") { Patterns = ["*.txt"] },
                    FilePickerFileTypes.All,
                ],
            });
            if (files.Count == 0)
                return;

            await using var stream = await files[0].OpenReadAsync();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            viewModel.InputText = await reader.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            viewModel.SortSummary = $"Не удалось открыть файл: {ex.Message}";
        }
    }

    /// <summary>Сохраняет таблицу замеров в CSV.</summary>
    private async void OnSaveCsvClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { HasTable: true } viewModel || TopLevel.GetTopLevel(this) is not { } topLevel)
            return;

        try
        {
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Сохранить таблицу замеров",
                SuggestedFileName = "experiments.csv",
                DefaultExtension = "csv",
                FileTypeChoices = [new FilePickerFileType("Таблица CSV") { Patterns = ["*.csv"] }],
            });
            if (file is null)
                return;

            // BOM нужен, чтобы Excel сразу распознал кодировку UTF-8.
            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            await writer.WriteAsync(viewModel.BuildCsv());
            viewModel.ExperimentStatus = $"Таблица сохранена: {file.Name}";
        }
        catch (Exception ex)
        {
            viewModel.ExperimentStatus = $"Не удалось сохранить таблицу: {ex.Message}";
        }
    }
}
