using SortingLab.Core.Visual;

namespace SortingLab.Core.External;

/// <summary>Внешняя сортировка многопутевым (3-путевым) сбалансированным слиянием.</summary>
public sealed class MultiwayMergeSorter : IExternalSorter
{
    private const int Ways = 3;

    public string Name => "Многопутевое слияние (3 пути)";

    public string Group => "Внешняя сортировка (3 вспомогательных файла)";

    public string Idea =>
        "Ускоряем естественное слияние: вместо двух вспомогательных файлов используем сразу три — F1, F2 и F3. " +
        "Готовые упорядоченные серии раскладываем по кругу в три файла, а при слиянии смотрим сразу на три верхние карточки " +
        "и перекладываем в A самую маленькую. Число серий уменьшается втрое за каждый проход, поэтому проходов нужно меньше.";

    public string Complexity =>
        "Проходов: до ⌈log₃ n⌉. Время: O(n log n), O(n) на уже упорядоченном файле. Вспомогательных файлов: 3 (F1, F2, F3).";

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
            TapeFile[] aux =
            [
                new TapeFile("F1", "Вспомогательный файл 1", isAuxiliary: true, workDir, table),
                new TapeFile("F2", "Вспомогательный файл 2", isAuxiliary: true, workDir, table),
                new TapeFile("F3", "Вспомогательный файл 3", isAuxiliary: true, workDir, table),
            ];
            fileA.Load(records);

            TapeSnapshot[] SnapshotAll(
                int? aCompare = null,
                int? aMoved = null,
                bool aSorted = false,
                int? auxCompare1 = null,
                int? auxCompare2 = null,
                int? auxMoved = null)
            {
                var list = new TapeSnapshot[Ways + 1];
                list[0] = fileA.ToSnapshot(comparer, comparingIndex: aCompare, movedIndex: aMoved, allSorted: aSorted);
                for (int t = 0; t < Ways; t++)
                {
                    int? cmpIdx = (t == auxCompare1 || t == auxCompare2) ? aux[t].ReadPosition : null;
                    int? mvIdx = t == auxMoved ? aux[t].Count - 1 : null;
                    list[t + 1] = aux[t].ToSnapshot(comparer, comparingIndex: cmpIdx, movedIndex: mvIdx);
                }
                return list;
            }

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
                    Tapes = SnapshotAll(aSorted: true),
                    Result = records.ToArray(),
                };
                yield break;
            }

            int pass = 0;
            while (true)
            {
                pass++;
                fileA.ReadPosition = 0;
                foreach (var tape in aux)
                    tape.Clear();

                yield return new ExternalSortStep
                {
                    Kind = StepKind.Info,
                    Log = $"Проход {pass}, фаза 1: раскладываем серии из A по кругу в F1, F2 и F3",
                    Explanation = $"Проход {pass}, разделение на 3 файла. Ищем готовые упорядоченные серии в A " +
                                  "и раскладываем их по кругу: в F1, затем в F2, затем в F3.",
                    Tapes = SnapshotAll(),
                };

                int targetIdx = 0;
                int totalRuns = 0;
                int[] runCounts = new int[Ways];
                TableRecord? prev = null;

                while (fileA.HasMore)
                {
                    var current = fileA.Peek().Record;
                    if (prev is null)
                    {
                        totalRuns = 1;
                        runCounts[0] = 1;
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
                                ? $"Проверяем порядок в A: {keyPrev} ≤ {keyCur}. Серия продолжается, пишем в тот же файл {aux[targetIdx].Name}."
                                : $"Проверяем порядок в A: {keyPrev} > {keyCur}. Серия оборвалась — переходим к следующему файлу {aux[(targetIdx + 1) % Ways].Name}.",
                            Tapes = SnapshotAll(aCompare: fileA.ReadPosition),
                        };

                        if (!continues)
                        {
                            targetIdx = (targetIdx + 1) % Ways;
                            totalRuns++;
                            runCounts[targetIdx]++;
                        }
                    }

                    fileA.Consume();
                    aux[targetIdx].Append(current, runCounts[targetIdx]);
                    prev = current;

                    string desc = table.DescribeRecord(current, comparer.KeyColumnIndex);
                    yield return new ExternalSortStep
                    {
                        Kind = StepKind.Move,
                        Log = $"Перенос A → {aux[targetIdx].Name}: {desc}",
                        Explanation = $"Записываем {desc} в файл {aux[targetIdx].Name} (серия {runCounts[targetIdx]}).",
                        Tapes = SnapshotAll(auxMoved: targetIdx),
                    };
                }

                if (totalRuns <= 1)
                {
                    fileA.ReadPosition = 0;
                    foreach (var tape in aux)
                        tape.Clear();

                    yield return new ExternalSortStep
                    {
                        Kind = StepKind.Info,
                        Log = "Найдена ровно одна серия — файл уже упорядочен",
                        Explanation = "Все записи в A уже идут по порядку одной серией — можно завершать сортировку!",
                        Tapes = SnapshotAll(aSorted: true),
                    };
                    break;
                }

                // Фаза 2: одновременное слияние серий из F1, F2, F3 в A.
                fileA.Clear();
                foreach (var tape in aux)
                    tape.ReadPosition = 0;

                yield return new ExternalSortStep
                {
                    Kind = StepKind.Info,
                    Log = $"Проход {pass}, фаза 2: сливаем серии ({totalRuns} шт.) из F1, F2 и F3 в A",
                    Explanation = $"Проход {pass}, 3-путевое слияние. Берём по одной серии из F1, F2 и F3 " +
                                  "и каждый раз выбираем среди них запись с самым маленьким ключом.",
                    Tapes = SnapshotAll(),
                };

                int outRun = 0;
                while (aux.Any(t => t.HasMore))
                {
                    outRun++;
                    int[] activeRun = new int[Ways];
                    for (int t = 0; t < Ways; t++)
                        activeRun[t] = aux[t].HasMore ? aux[t].Peek().RunIndex : -1;

                    while (true)
                    {
                        var activeTapes = new List<int>(Ways);
                        for (int t = 0; t < Ways; t++)
                        {
                            if (activeRun[t] != -1 && aux[t].HasMore && aux[t].Peek().RunIndex == activeRun[t])
                                activeTapes.Add(t);
                        }

                        if (activeTapes.Count == 0)
                            break;

                        int best = activeTapes[0];
                        if (activeTapes.Count > 1)
                        {
                            for (int idx = 1; idx < activeTapes.Count; idx++)
                            {
                                int cand = activeTapes[idx];
                                var bestRec = aux[best].Peek().Record;
                                var candRec = aux[cand].Peek().Record;
                                bool candSmaller = comparer.Compare(candRec, bestRec) < 0;

                                string keyBest = comparer.FormatKey(bestRec);
                                string keyCand = comparer.FormatKey(candRec);
                                string descBest = table.DescribeRecord(bestRec, comparer.KeyColumnIndex);
                                string descCand = table.DescribeRecord(candRec, comparer.KeyColumnIndex);

                                yield return new ExternalSortStep
                                {
                                    Kind = StepKind.Compare,
                                    Log = $"Сравнение {aux[best].Name}[{aux[best].ReadPosition}]={keyBest} и {aux[cand].Name}[{aux[cand].ReadPosition}]={keyCand}: {keyBest} {(candSmaller ? ">" : "≤")} {keyCand}",
                                    Explanation = candSmaller
                                        ? $"Ищем наименьшую запись среди открытых файлов: {descCand} из {aux[cand].Name} меньше, чем {descBest} из {aux[best].Name}."
                                        : $"Ищем наименьшую запись среди открытых файлов: {descBest} из {aux[best].Name} не больше, чем {descCand} из {aux[cand].Name}.",
                                    Tapes = SnapshotAll(auxCompare1: best, auxCompare2: cand),
                                };

                                if (candSmaller)
                                    best = cand;
                            }
                        }

                        var chosen = aux[best].Consume().Record;
                        fileA.Append(chosen, outRun);
                        string chosenDesc = table.DescribeRecord(chosen, comparer.KeyColumnIndex);

                        yield return new ExternalSortStep
                        {
                            Kind = StepKind.Move,
                            Log = $"Перенос {aux[best].Name} → A: {chosenDesc}",
                            Explanation = activeTapes.Count == 1
                                ? $"В остальных файлах текущие серии уже кончились, поэтому дописываем {chosenDesc} из {aux[best].Name} в A."
                                : $"Самая маленькая из текущих записей — {chosenDesc} из {aux[best].Name}. Переносим её в файл A.",
                            Tapes = SnapshotAll(aMoved: fileA.Count - 1),
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
                Explanation = $"Все записи упорядочены по полю «{comparer.KeyColumnName}» в основном файле A.",
                Tapes = SnapshotAll(aSorted: true),
                Result = result,
            };
        }
        finally
        {
            TapeFile.CleanupWorkDirectory(workDir);
        }
    }
}