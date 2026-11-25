using SortingLab.Core.Visual;

namespace SortingLab.Core.External;

/// <summary>Внешняя сортировка естественным слиянием (файлы A, B и C).</summary>
public sealed class NaturalMergeSorter : IExternalSorter
{
    public string Name => "Естественное слияние";

    public string Group => "Внешняя сортировка (2 вспомогательных файла)";

    public string Idea =>
        "Вместо того чтобы резать файл на равные кусочки, ищем уже готовые упорядоченные цепочки — естественные серии. " +
        "Пока следующая запись не меньше предыдущей, складываем её в тот же файл (B или C). " +
        "Как только порядок нарушился — переключаемся на другой файл. Затем сливаем серии обратно в A. " +
        "Если записи уже стоят по порядку, алгоритм заметит одну большую серию и закончит за один проход.";

    public string Complexity =>
        "Время: O(n log n) в среднем и худшем случае, O(n) на уже упорядоченном файле. Вспомогательных файлов: 2 (B и C).";

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

            int pass = 0;
            while (true)
            {
                pass++;
                fileA.ReadPosition = 0;
                fileB.Clear();
                fileC.Clear();

                yield return new ExternalSortStep
                {
                    Kind = StepKind.Info,
                    Log = $"Проход {pass}, фаза 1: находим естественные серии в A и раскладываем в B и C",
                    Explanation = $"Проход {pass}, разделение. Идём по файлу A и выделяем уже упорядоченные цепочки записей, " +
                                  "раскладывая их по очереди в файлы B и C.",
                    Tapes = [fileA.ToSnapshot(comparer), fileB.ToSnapshot(comparer), fileC.ToSnapshot(comparer)],
                };

                bool toB = true;
                int runB = 0, runC = 0, totalRuns = 0;
                TableRecord? prev = null;

                while (fileA.HasMore)
                {
                    var current = fileA.Peek().Record;
                    if (prev is null)
                    {
                        totalRuns = 1;
                        runB = 1;
                    }
                    else
                    {
                        bool continues = comparer.Compare(prev, current) <= 0;
                        string keyPrev = comparer.FormatKey(prev);
                        string keyCur = comparer.FormatKey(current);

                        yield return new ExternalSortStep
                        {
                            Kind = StepKind.Compare,
                            Log = $"Сравнение A[{fileA.ReadPosition - 1}]={keyPrev} и A[{fileA.ReadPosition}]={keyCur}: {keyPrev} {(continues ? "≤" : ">")} {keyCur}",
                            Explanation = continues
                                ? $"Проверяем порядок в A: {keyPrev} ≤ {keyCur}. Серия продолжается, поэтому кладём запись в тот же файл {(toB ? "B" : "C")}."
                                : $"Проверяем порядок в A: {keyPrev} > {keyCur}. Серия оборвалась! Переключаемся на файл {(!toB ? "B" : "C")}.",
                            Tapes =
                            [
                                fileA.ToSnapshot(comparer, comparingIndex: fileA.ReadPosition),
                                fileB.ToSnapshot(comparer),
                                fileC.ToSnapshot(comparer),
                            ],
                        };

                        if (!continues)
                        {
                            toB = !toB;
                            totalRuns++;
                            if (toB) runB++; else runC++;
                        }
                    }

                    fileA.Consume();
                    TapeFile target = toB ? fileB : fileC;
                    int targetRun = toB ? runB : runC;
                    target.Append(current, targetRun);
                    prev = current;

                    string desc = table.DescribeRecord(current, comparer.KeyColumnIndex);
                    yield return new ExternalSortStep
                    {
                        Kind = StepKind.Move,
                        Log = $"Перенос A → {target.Name}: {desc}",
                        Explanation = $"Записываем {desc} в файл {target.Name} (серия {targetRun}).",
                        Tapes =
                        [
                            fileA.ToSnapshot(comparer),
                            fileB.ToSnapshot(comparer, movedIndex: toB ? fileB.Count - 1 : null),
                            fileC.ToSnapshot(comparer, movedIndex: !toB ? fileC.Count - 1 : null),
                        ],
                    };
                }

                if (totalRuns <= 1)
                {
                    fileA.ReadPosition = 0;
                    fileB.Clear();
                    fileC.Clear();

                    yield return new ExternalSortStep
                    {
                        Kind = StepKind.Info,
                        Log = "Найдена ровно одна серия — файл уже упорядочен",
                        Explanation = "Все записи в файле A образовали одну целую серию без обрывов. Значит, порядок уже идеальный!",
                        Tapes = [fileA.ToSnapshot(comparer, allSorted: true), fileB.ToSnapshot(comparer), fileC.ToSnapshot(comparer)],
                    };
                    break;
                }

                // Фаза 2: слияние естественных серий из B и C обратно в A.
                fileA.Clear();
                fileB.ReadPosition = 0;
                fileC.ReadPosition = 0;

                yield return new ExternalSortStep
                {
                    Kind = StepKind.Info,
                    Log = $"Проход {pass}, фаза 2: сливаем серии ({totalRuns} шт.) из B и C в A",
                    Explanation = $"Проход {pass}, слияние. Найдено серий: {totalRuns}. Сливаем по одной серии из B и C в файл A.",
                    Tapes = [fileA.ToSnapshot(comparer), fileB.ToSnapshot(comparer), fileC.ToSnapshot(comparer)],
                };

                int outRun = 0;
                while (fileB.HasMore || fileC.HasMore)
                {
                    outRun++;
                    int bRunId = fileB.HasMore ? fileB.Peek().RunIndex : -1;
                    int cRunId = fileC.HasMore ? fileC.Peek().RunIndex : -1;

                    while (fileB.HasMore && fileB.Peek().RunIndex == bRunId &&
                           fileC.HasMore && fileC.Peek().RunIndex == cRunId)
                    {
                        var recB = fileB.Peek().Record;
                        var recC = fileC.Peek().Record;
                        bool takeB = comparer.Compare(recB, recC) <= 0;

                        string keyB = comparer.FormatKey(recB);
                        string keyC = comparer.FormatKey(recC);
                        string descB = table.DescribeRecord(recB, comparer.KeyColumnIndex);
                        string descC = table.DescribeRecord(recC, comparer.KeyColumnIndex);

                        yield return new ExternalSortStep
                        {
                            Kind = StepKind.Compare,
                            Log = $"Сравнение B[{fileB.ReadPosition}]={keyB} и C[{fileC.ReadPosition}]={keyC}: {keyB} {(takeB ? "≤" : ">")} {keyC}",
                            Explanation = takeB
                                ? $"Сравниваем {descB} из B и {descC} из C. Запись из B не больше — отправляем её в A первой."
                                : $"Сравниваем {descB} из B и {descC} из C. Запись из C меньше — отправляем её в A первой.",
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
                            fileA.Append(recB, outRun);
                            yield return new ExternalSortStep
                            {
                                Kind = StepKind.Move,
                                Log = $"Перенос B → A: {descB}",
                                Explanation = $"Записываем {descB} из B в основной файл A.",
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
                            fileA.Append(recC, outRun);
                            yield return new ExternalSortStep
                            {
                                Kind = StepKind.Move,
                                Log = $"Перенос C → A: {descC}",
                                Explanation = $"Записываем {descC} из C в основной файл A.",
                                Tapes =
                                [
                                    fileA.ToSnapshot(comparer, movedIndex: fileA.Count - 1),
                                    fileB.ToSnapshot(comparer),
                                    fileC.ToSnapshot(comparer),
                                ],
                            };
                        }
                    }

                    while (fileB.HasMore && fileB.Peek().RunIndex == bRunId)
                    {
                        var recB = fileB.Consume().Record;
                        fileA.Append(recB, outRun);
                        string descB = table.DescribeRecord(recB, comparer.KeyColumnIndex);
                        yield return new ExternalSortStep
                        {
                            Kind = StepKind.Move,
                            Log = $"Перенос B → A: {descB}",
                            Explanation = $"Текущая серия в C закончилась, дописываем остаток серии из B: {descB}.",
                            Tapes =
                            [
                                fileA.ToSnapshot(comparer, movedIndex: fileA.Count - 1),
                                fileB.ToSnapshot(comparer),
                                fileC.ToSnapshot(comparer),
                            ],
                        };
                    }

                    while (fileC.HasMore && fileC.Peek().RunIndex == cRunId)
                    {
                        var recC = fileC.Consume().Record;
                        fileA.Append(recC, outRun);
                        string descC = table.DescribeRecord(recC, comparer.KeyColumnIndex);
                        yield return new ExternalSortStep
                        {
                            Kind = StepKind.Move,
                            Log = $"Перенос C → A: {descC}",
                            Explanation = $"Текущая серия в B закончилась, дописываем остаток серии из C: {descC}.",
                            Tapes =
                            [
                                fileA.ToSnapshot(comparer, movedIndex: fileA.Count - 1),
                                fileB.ToSnapshot(comparer),
                                fileC.ToSnapshot(comparer),
                            ],
                        };
                    }
                }
            }

            var result = fileA.GetRecords();
            if (!string.IsNullOrEmpty(outputPath))
                table.SaveToFile(outputPath, result);

            yield return new ExternalSortStep
            {
                Kind = StepKind.Done,
                Log = "Сортировка закончена",
                Explanation = $"Все записи упорядочены по полю «{comparer.KeyColumnName}» и сохранены в основном файле A.",
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