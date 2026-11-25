namespace SortingLab.Core.Words;

/// <summary>
/// Поразрядная сортировка строк от старшего разряда (MSD Radix Sort).
/// На каждом уровне распределяет слова по корзинам по коду символа в позиции <c>d</c>,
/// а короткие подмассивы досортировывает простыми вставками.
/// Порядок совпадает с <see cref="string.CompareOrdinal(string, string)"/>.
/// </summary>
public sealed class RadixWordSorter : IWordSorter
{
    private const int InsertionCutoff = 16;

    public string Name => "Поразрядная сортировка (Radix)";

    public void Sort(string[] words)
    {
        ArgumentNullException.ThrowIfNull(words);
        if (words.Length <= 1)
            return;

        string[] aux = new string[words.Length];
        SortRange(words, aux, 0, words.Length - 1, 0);
    }

    private static void SortRange(string[] a, string[] aux, int lo, int hi, int d)
    {
        while (hi - lo + 1 > InsertionCutoff)
        {
            int count = hi - lo + 1;
            int finished = 0;
            int minChar = char.MaxValue;
            int maxChar = char.MinValue;

            for (int i = lo; i <= hi; i++)
            {
                string s = a[i];
                if (s.Length == d)
                {
                    finished++;
                }
                else
                {
                    int c = s[d];
                    if (c < minChar) minChar = c;
                    if (c > maxChar) maxChar = c;
                }
            }

            // Все слова в этом диапазоне закончились на позиции d — они одинаковы.
            if (finished == count)
                return;

            // У всех слов на позиции d один и тот же символ — переходим к следующему разряду без корзин.
            if (finished == 0 && minChar == maxChar)
            {
                d++;
                continue;
            }

            // Часть слов закончилась, а у остальных одинаковый символ на позиции d.
            if (finished > 0 && minChar == maxChar)
            {
                int fPos = 0;
                int rPos = finished;
                for (int i = lo; i <= hi; i++)
                {
                    if (a[i].Length == d)
                        aux[fPos++] = a[i];
                    else
                        aux[rPos++] = a[i];
                }

                Array.Copy(aux, 0, a, lo, count);
                lo += finished;
                d++;
                continue;
            }

            int range = maxChar - minChar + 1;
            if (range <= 256)
            {
                // Основной быстрый путь (латиница, кириллица и другие одноалфавитные тексты):
                // корзина 0 — слова длины d; корзины 1..range — символы minChar..maxChar.
                int bucketCount = range + 1;
                int[] counts = new int[bucketCount + 2];

                for (int i = lo; i <= hi; i++)
                {
                    int b = a[i].Length == d ? 0 : (a[i][d] - minChar) + 1;
                    counts[b + 2]++;
                }

                for (int r = 0; r < bucketCount; r++)
                    counts[r + 2] += counts[r + 1];

                for (int i = lo; i <= hi; i++)
                {
                    int b = a[i].Length == d ? 0 : (a[i][d] - minChar) + 1;
                    aux[counts[b + 1]++] = a[i];
                }

                Array.Copy(aux, 0, a, lo, count);

                // Корзину 0 (закончившиеся слова) рекурсивно сортировать не нужно.
                for (int b = 1; b < bucketCount; b++)
                {
                    int subLo = lo + counts[b];
                    int subHi = lo + counts[b + 1] - 1;
                    if (subLo < subHi)
                        SortRange(a, aux, subLo, subHi, d + 1);
                }

                return;
            }
            else
            {
                // Смешанные алфавиты (например, латиница + кириллица):
                // сначала делим по старшему байту символа (c >> 8), затем в каждой корзине range <= 256.
                const int bucketCount = 257;
                int[] counts = new int[bucketCount + 2];

                for (int i = lo; i <= hi; i++)
                {
                    int b = a[i].Length == d ? 0 : (a[i][d] >> 8) + 1;
                    counts[b + 2]++;
                }

                for (int r = 0; r < bucketCount; r++)
                    counts[r + 2] += counts[r + 1];

                for (int i = lo; i <= hi; i++)
                {
                    int b = a[i].Length == d ? 0 : (a[i][d] >> 8) + 1;
                    aux[counts[b + 1]++] = a[i];
                }

                Array.Copy(aux, 0, a, lo, count);

                for (int b = 1; b < bucketCount; b++)
                {
                    int subLo = lo + counts[b];
                    int subHi = lo + counts[b + 1] - 1;
                    if (subLo < subHi)
                        SortRange(a, aux, subLo, subHi, d);
                }

                return;
            }
        }

        InsertionSort(a, lo, hi);
    }

    private static void InsertionSort(string[] a, int lo, int hi)
    {
        for (int i = lo + 1; i <= hi; i++)
        {
            string key = a[i];
            int j = i - 1;
            while (j >= lo && string.CompareOrdinal(a[j], key) > 0)
            {
                a[j + 1] = a[j];
                j--;
            }

            a[j + 1] = key;
        }
    }
}