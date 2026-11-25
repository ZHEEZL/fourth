using SortingLab.Core.External;
using SortingLab.Core.Visual;

namespace SortingLab.Tests;

public class ExternalSorterTests
{
    public static IEnumerable<object[]> SorterNames => ExternalSorters.All.Select(s => new object[] { s.Name });

    [Theory]
    [MemberData(nameof(SorterNames))]
    public void SortsEuropeanCountriesByPopulation(string name)
    {
        var sorter = Find(name);
        var table = SampleTables.Countries();
        var filtered = table.Filter(filterColumnIndex: 1, "Европа");
        var comparer = table.CreateComparer(keyColumnIndex: 4, filtered);

        var steps = sorter.Sort(table, filtered, comparer).ToList();
        var result = steps[^1].Result!;

        Assert.Equal(StepKind.Done, steps[^1].Kind);
        Assert.Equal(["Норвегия", "Швеция", "Польша", "Испания", "Италия", "Франция", "Германия"],
            result.Select(r => r.GetValue(0)).ToArray());
    }

    [Theory]
    [MemberData(nameof(SorterNames))]
    public void SortsAcidsByMolecularMass(string name)
    {
        var sorter = Find(name);
        var table = SampleTables.Substances();
        var filtered = table.Filter(filterColumnIndex: 0, "Кислота");
        var comparer = table.CreateComparer(keyColumnIndex: 2, filtered);

        var steps = sorter.Sort(table, filtered, comparer).ToList();
        var result = steps[^1].Result!;

        Assert.Equal(
            ["Плавиковая кислота", "Соляная кислота", "Уксусная кислота", "Угольная кислота", "Азотная кислота", "Фосфорная кислота", "Серная кислота"],
            result.Select(r => r.GetValue(1)).ToArray());
    }

    [Theory]
    [MemberData(nameof(SorterNames))]
    public void SortsRussianWordsAlphabeticallyAndWritesResultFile(string name)
    {
        var sorter = Find(name);
        var table = SampleTables.RussianWords();
        var comparer = table.CreateComparer(keyColumnIndex: 0);
        string outPath = Path.Combine(Path.GetTempPath(), $"sortinglab_test_{Guid.NewGuid():N}.csv");

        try
        {
            var steps = sorter.Sort(table, table.Records, comparer, outPath).ToList();
            var result = steps[^1].Result!;

            Assert.True(File.Exists(outPath));
            var reloaded = CsvTable.LoadFromFile(outPath);
            Assert.Equal(result.Select(r => r.GetValue(0)), reloaded.Records.Select(r => r.GetValue(0)));

            for (int i = 1; i < result.Count; i++)
                Assert.True(comparer.Compare(result[i - 1], result[i]) <= 0);
        }
        finally
        {
            if (File.Exists(outPath))
                File.Delete(outPath);
        }
    }

    [Theory]
    [MemberData(nameof(SorterNames))]
    public void HandlesEdgeCasesAndExplainsEveryStep(string name)
    {
        var sorter = Find(name);
        var table = CsvTable.Parse("K\n5\n1\n5\n2\n1\n3");
        var comparer = table.CreateComparer(0);

        // Пустой и одноэлементный случаи.
        Assert.Empty(sorter.Sort(table, [], comparer).Last().Result!);
        Assert.Single(sorter.Sort(table, [table.Records[0]], comparer).Last().Result!);

        // Повторяющиеся значения и проверка логов/пояснений.
        var steps = sorter.Sort(table, table.Records, comparer).ToList();
        Assert.All(steps, s =>
        {
            Assert.False(string.IsNullOrWhiteSpace(s.Explanation));
            Assert.NotEmpty(s.Tapes);
            if (s.Kind is StepKind.Compare or StepKind.Move)
                Assert.False(string.IsNullOrWhiteSpace(s.Log));
        });
        Assert.Equal(["1", "1", "2", "3", "5", "5"], steps[^1].Result!.Select(r => r.GetValue(0)).ToArray());
    }

    [Fact]
    public void CsvParserSupportsSemicolonAndQuotes()
    {
        var table = CsvTable.Parse("Имя;Описание;Вес\n\"Иван;Петров\";\"Текст \"\"в кавычках\"\"\";12,5");
        Assert.Equal(';', table.Delimiter);
        Assert.Equal("Иван;Петров", table.Records[0].GetValue(0));
        Assert.Equal("Текст \"в кавычках\"", table.Records[0].GetValue(1));
        Assert.True(table.CreateComparer(2).IsNumeric);
    }

    private static IExternalSorter Find(string name) => ExternalSorters.All.Single(s => s.Name == name);
}