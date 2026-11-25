using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SortingLab.App.ViewModels;

namespace SortingLab.App.Views;

public partial class ExternalSortView : UserControl
{
    public ExternalSortView() => InitializeComponent();

    private ExternalSortViewModel? ViewModel => DataContext as ExternalSortViewModel;

    private async void OnOpenCsvClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } vm || TopLevel.GetTopLevel(this) is not { } topLevel)
            return;

        try
        {
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Открыть таблицу CSV",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Таблицы CSV") { Patterns = ["*.csv", "*.txt"] },
                    FilePickerFileTypes.All,
                ],
            });
            if (files.Count == 0)
                return;

            await using var stream = await files[0].OpenReadAsync();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            string content = await reader.ReadToEndAsync();
            vm.LoadFromCsvContent(content, Path.GetFileNameWithoutExtension(files[0].Name));
        }
        catch (Exception ex)
        {
            vm.ErrorMessage = $"Не удалось открыть файл: {ex.Message}";
        }
    }

    private async void OnSaveResultClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { HasSortedResult: true } vm || TopLevel.GetTopLevel(this) is not { } topLevel)
            return;

        try
        {
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Сохранить отсортированную таблицу",
                SuggestedFileName = vm.SuggestedResultFileName,
                DefaultExtension = "csv",
                FileTypeChoices = [new FilePickerFileType("Таблица CSV") { Patterns = ["*.csv"] }],
            });
            if (file is null)
                return;

            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            await writer.WriteAsync(vm.BuildResultCsv());
            vm.OutputStatus = $"Результат сохранён: {file.Name}";
        }
        catch (Exception ex)
        {
            vm.ErrorMessage = $"Не удалось сохранить файл: {ex.Message}";
        }
    }
}