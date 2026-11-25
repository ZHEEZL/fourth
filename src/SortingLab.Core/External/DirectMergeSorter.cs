using SortingLab.Core.Visual;

namespace SortingLab.Core.External;

/// <summary>Внешняя сортировка прямым двухпутевым слиянием (файлы A, B и C).</summary>
public sealed class DirectMergeSorter : IExternalSorter
{
    public string Name => "Прямое слияние";

    public string Group => "Внешняя сортировка (2 вспомогательных файла)";

    public string Idea =>
        "Представим, что карточки не помещаются в памяти, и мы можем перекладывать их только между файлами. " +
        "Сначала делим основной файл A на кусочки (серии) длиной 1 и раскладываем по очереди в файлы B и C. " +
        "Потом сливаем пары кусочков обратно в A, каждый раз выбирая меньшую из двух верхних карточек. " +
        "На следующем проходе кусочки становятся вдвое длиннее (2, 4, 8…), пока весь файл не соберётся в один упорядоченный ряд.";

    public string Complexity =>
        "Проходов: ⌈log₂ n⌉. Сравнений и записей: O(n log n) в любом случае. Вспомогательных файлов: 2 (B и C).";

    public IEnumerable<ExternalSortStep> Sort(
        CsvTable table,
        IReadOnlyList<TableRecord> records,
        RecordKeyComparer comparer,
        string? outputPath = null)
    {
        string workDir = TapeFile.CreateWorkDirectory();
        try
        {
            var fileA = new TapeFile("A", "Основной файл", isAuxiliary: false, workDir, table);
            var fileB = new TapeFile("B", "Вспомогательный файл 1", isAuxiliary: true, workDir, table);
            var fileC = new TapeFile("C", "Вспомогательный файл 2", isAuxiliary: true, workDir, table);
            fileA.Load(records);

            if (records.Count <= 1)
            {
                if (!string.IsNullOrEmpty(outputPath))
                    table.SaveToFile(outputPath, records);

                yield return new ExternalSortStep
                {
                    Kind = StepKind.Done,
                    Log = "Сортировка закончена",
                    Explanation = records.Count == 0
                        ? "Записей для сортировки нет."
                        : "В файле всего одна запись — она уже стоит на своём месте.",
                    Tapes = [fileA.ToSnapshot(comparer, allSorted: true), fileB.ToSnapshot(comparer), fileC.ToSnapshot(comparer)],
                    Result = records.ToArray(),
                };
                yield break;
            }

            int n = records.Count;
            int runSize = 1;
            int pass = 0;

            while (runSize < n)
            {
                pass++;

                // Фаза 1: разделение A → B, C сериями длиной runSize.
                fileA.ReadPosition = 0;
                fileB.Clear();
                fileC.Clear();

                yield return new ExternalSortStep
                {
                    Kind = StepKind.Info,
                    Log = $"Проход {pass}, фаза 1: делим A на серии по {runSize} в B и C",
                    Explanation = $"Проход {pass}, разделение. Берём записи из основного файла A кусочками по {runSize} " +
                                  "и раскладываем их по очереди во вспомогательные файлы B и C.",
                    Tapes = [fileA.ToSnapshot(comparer), fileB.ToSnapshot(comparer), fileC.ToSnapshot(comparer)],
                };

                bool toB = true;
                int runB = 0, runC = 0;
                while (fileA.HasMore)
                {
                    TapeFile target = toB ? fileB : fileC;
                    int targetRun = toB ? ++runB : ++runC;

                    for (int k = 0; k < runSize && fileA.HasMore; k++)
                    {
                        var rec = fileA.Consume().Record;
                        target.Append(rec, targetRun);
                        string desc = table.DescribeRecord(rec, comparer.KeyColumnIndex);

                        yield return new ExternalSortStep
                        {
                            Kind = StepKind.Move,
                            Log = $"Перенос A → {target.Name}: {desc}",
                            Explanation = $"Перекладываем {desc} из файла A в файл {target.Name} (серия {targetRun}, размер до {runSize}).",
                            Tapes =
                            [
                                fileA.ToSnapshot(comparer),
                                fileB.ToSnapshot(comparer, movedIndex: toB ? fileB.Count - 1 : null),
                                fileC.ToSnapshot(comparer, movedIndex: !toB ? fileC.Count - 1 : null),
                            ],
                        };
                    }

                    toB = !toB;
                }

                // Фаза 2: слияние серий длиной runSize из B и C обратно в A.
                fileA.Clear();
                fileB.ReadPosition = 0;
                fileC.ReadPosition = 0;

                yield return new ExternalSortStep
                {
                    Kind = StepKind.Info,
                    Log = $"Проход {pass}, фаза 2: сливаем серии длиной {runSize} из B и C в A",
                    Explanation = $"Проход {pass}, слияние. Берём по одной серии из B и C и собираем их обратно в файл A по порядку.",
                    Tapes = [fileA.ToSnapshot(comparer), fileB.ToSnapshot(comparer), fileC.ToSnapshot(comparer)],
                };

                int outRun = 0;
                while (fileB.HasMore || fileC.HasMore)
                {
                    outRun++;
                    int leftRem = Math.Min(runSize, fileB.Remaining);
                    int rightRem = Math.Min(runSize, fileC.Remaining);

                    while (leftRem > 0 && rightRem > 0)
                    {
                        var recB = fileB.Peek().Record;
                        var recC = fileC.Peek().Record;
                        int cmp = comparer.Compare(recB, recC);
                        bool takeB = cmp <= 0;

                        string keyB = comparer.FormatKey(recB);
                        string keyC = comparer.FormatKey(recC);
                        string descB = table.DescribeRecord(recB, comparer.KeyColumnIndex);
                        string descC = table.DescribeRecord(recC, comparer.KeyColumnIndex);

                        yield return new ExternalSortStep
                        {
                            Kind = StepKind.Compare,
                            Log = $"Сравнение B[{fileB.ReadPosition}]={keyB} и C[{fileC.ReadPosition}]={keyC}: {keyB} {(takeB ? "≤" : ">")} {keyC}",
                            Explanation = takeB
                                ? $"Сравниваем первые записи текущих серий: {descB} в B и {descC} в C. У записи из B ключ не больше, поэтому берём её."
                                : $"Сравниваем первые записи текущих серий: {descB} в B и {descC} в C. У записи из C ключ меньше, поэтому берём её.",
                            Tapes =
                            [
                                fileA.ToSnapshot(comparer),
                                fileB.ToSnapshot(comparer, comparingIndex: fileB.ReadPosition),
                                fileC.ToSnapshot(comparer, comparingIndex: fileC.ReadPosition),
                            ],
                        };

                        if (takeB)
                        {
                            fileB.Consume();
                            leftRem--;
                            fileA.Append(recB, outRun);

                            yield return new ExternalSortStep
                            {
                                Kind = StepKind.Move,
                                Log = $"Перенос B → A: {descB}",
                                Explanation = $"Записываем {descB} из файла B в основной файл A.",
                                Tapes =
                                [
                                    fileA.ToSnapshot(comparer, movedIndex: fileA.Count - 1),
                                    fileB.ToSnapshot(comparer),
                                    fileC.ToSnapshot(comparer),
                                ],
                            };
                        }
                        else
                        {
                            fileC.Consume();
                            rightRem--;
                            fileA.Append(recC, outRun);

                            yield return new ExternalSortStep
                            {
                                Kind = StepKind.Move,
                                Log = $"Перенос C → A: {descC}",
                                Explanation = $"Записываем {descC} из файла C в основной файл A.",
                                Tapes =
                                [
                                    fileA.ToSnapshot(comparer, movedIndex: fileA.Count - 1),
                                    fileB.ToSnapshot(comparer),
                                    fileC.ToSnapshot(comparer),
                                ],
                            };
                        }
                    }

                    while (leftRem > 0)
                    {
                        var recB = fileB.Consume().Record;
                        leftRem--;
                        fileA.Append(recB, outRun);
                        string descB = table.DescribeRecord(recB, comparer.KeyColumnIndex);

                        yield return new ExternalSortStep
                        {
                            Kind = StepKind.Move,
                            Log = $"Перенос B → A: {descB}",
                            Explanation = $"В текущей серии файла C записи кончились, поэтому просто дописываем остаток серии из B: {descB}.",
                            Tapes =
                            [
                                fileA.ToSnapshot(comparer, movedIndex: fileA.Count - 1),
                                fileB.ToSnapshot(comparer),
                                fileC.ToSnapshot(comparer),
                            ],
                        };
                    }

                    while (rightRem > 0)
                    {
                        var recC = fileC.Consume().Record;
                        rightRem--;
                        fileA.Append(recC, outRun);
                        string descC = table.DescribeRecord(recC, comparer.KeyColumnIndex);

                        yield return new ExternalSortStep
                        {
                            Kind = StepKind.Move,
                            Log = $"Перенос C → A: {descC}",
                            Explanation = $"В текущей серии файла B записи кончились, поэтому просто дописываем остаток серии из C: {descC}.",
                            Tapes =
                            [
                                fileA.ToSnapshot(comparer, movedIndex: fileA.Count - 1),
                                fileB.ToSnapshot(comparer),
                                fileC.ToSnapshot(comparer),
                            ],
                        };
                    }
                }

                runSize *= 2;
            }

            var result = fileA.GetRecords();
            if (!string.IsNullOrEmpty(outputPath))
                table.SaveToFile(outputPath, result);

            fileA.ReadPosition = 0;
            fileB.Clear();
            fileC.Clear();

            yield return new ExternalSortStep
            {
                Kind = StepKind.Done,
                Log = "Сортировка закончена",
                Explanation = $"Все записи упорядочены по полю «{comparer.KeyColumnName}» и готовы в основном файле A.",
                Tapes = [fileA.ToSnapshot(comparer, allSorted: true), fileB.ToSnapshot(comparer), fileC.ToSnapshot(comparer)],
                Result = result,
            };
        }
        finally
        {
            TapeFile.CleanupWorkDirectory(workDir);
        }
    }
}