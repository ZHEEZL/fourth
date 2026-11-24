namespace SortingLab.Core;

/// <summary>Выбор формы слова после числа: 1 слово, 2 слова, 5 слов, 11 слов, 21 слово.</summary>
public static class RussianPlural
{
    /// <param name="n">Число.</param>
    /// <param name="one">Форма для 1, 21, 101…</param>
    /// <param name="few">Форма для 2–4, 22–24…</param>
    /// <param name="many">Форма для 0, 5–20, 25–30…</param>
    public static string Choose(long n, string one, string few, string many)
    {
        long lastTwo = Math.Abs(n) % 100;
        if (lastTwo is >= 11 and <= 14)
            return many;

        return (lastTwo % 10) switch
        {
            1 => one,
            >= 2 and <= 4 => few,
            _ => many,
        };
    }
}
