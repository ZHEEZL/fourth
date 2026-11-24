using SortingLab.Core.Words;

namespace SortingLab.Tests;

public class WordTests
{
    [Fact]
    public void TokenizerSplitsLowercasesAndKeepsApostrophes()
    {
        string[] words = WordTokenizer.Split("Hello, world! Don’t STOP: it's 5 o'clock now...");

        Assert.Equal(["hello", "world", "don't", "stop", "it's", "o'clock", "now"], words);
    }

    [Fact]
    public void TokenizerReturnsEmptyArrayForTextWithoutLetters()
    {
        Assert.Empty(WordTokenizer.Split("  123, 456 -- !!! "));
    }
    // test text for push
    [Fact]
    public void QuickSortMatchesOrdinalSortOnSampleText()
    {
        string[] words = WordTokenizer.Split(SampleText.English);
        string[] expected = words.Order(StringComparer.Ordinal).ToArray();

        new QuickWordSorter().Sort(words);

        Assert.Equal(expected, words);
    }

    [Fact]
    public void QuickSortHandlesRandomAndDegenerateInput()
    {
        var sorter = new QuickWordSorter();
        var random = new Random(7);
        string[] vocabulary = ["a", "ab", "abc", "b", "ba", "zebra", "apple", "apple", "z", "don't"];

        for (int n = 0; n <= 300; n += 7)
        {
            string[] words = Enumerable.Range(0, n).Select(_ => vocabulary[random.Next(vocabulary.Length)]).ToArray();
            string[] expected = words.Order(StringComparer.Ordinal).ToArray();
            sorter.Sort(words);
            Assert.Equal(expected, words);
        }

        string[] same = Enumerable.Repeat("same", 1000).ToArray();
        sorter.Sort(same);
        Assert.All(same, w => Assert.Equal("same", w));
    }

    [Fact]
    public void EqualWordsEndUpNextToEachOther()
    {
        string[] words = WordTokenizer.Split("the cat and the dog and the bird");
        new QuickWordSorter().Sort(words);

        Assert.Equal(["and", "and", "bird", "cat", "dog", "the", "the", "the"], words);
    }

    [Fact]
    public void TakeCyclicRepeatsWordsWhenTextIsShort()
    {
        Assert.Equal(["a", "b", "c", "a", "b"], WordSource.TakeCyclic(["a", "b", "c"], 5));
        Assert.Equal(["a", "b"], WordSource.TakeCyclic(["a", "b", "c"], 2));
    }

    [Fact]
    public void RandomSampleIsRepeatableForSameSeed()
    {
        string[] source = WordTokenizer.Split(SampleText.English);

        Assert.Equal(WordSource.RandomSample(source, 200, 1), WordSource.RandomSample(source, 200, 1));
        Assert.All(WordSource.RandomSample(source, 200, 1), w => Assert.Contains(w, source));
    }

    [Fact]
    public void ExperimentRunnerReturnsRowPerSize()
    {
        string[] source = WordTokenizer.Split(SampleText.English);
        int[] sizes = [100, 500, 1000];

        var rows = ExperimentRunner.Run(WordSorters.All, sizes, n => WordSource.TakeCyclic(source, n), repeats: 2);

        Assert.Equal(sizes, rows.Select(r => r.WordCount));
        Assert.All(rows, r =>
        {
            Assert.Equal(WordSorters.All.Count, r.AverageMs.Count);
            Assert.All(r.AverageMs, ms => Assert.True(ms >= 0));
        });
    }
}
