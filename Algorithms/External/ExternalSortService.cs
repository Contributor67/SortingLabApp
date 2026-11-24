using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SortingLabApp.Data;
using SortingLabApp.Models;

namespace SortingLabApp.Algorithms.External
{
    public enum ExternalSortMethod
    {
        DirectMerge,
        NaturalMerge
    }

    /// <summary>
    /// Сервис внешней сортировки табличных данных на диске.
    /// Реализует прямое простое слияние и естественное слияние файлов с подробным протоколом,
    /// разметкой цветных серий и траекториями полета карточек.
    /// </summary>
    public static class ExternalSortService
    {
        public static List<ExternalSortStep> ExecuteExternalSort(
            List<Country> sourceList,
            string? continentFilter,
            string sortAttribute,
            bool ascending,
            ExternalSortMethod method,
            string workDirectory)
        {
            var steps = new List<ExternalSortStep>();
            Directory.CreateDirectory(workDirectory);

            string file0Path = Path.Combine(workDirectory, "file_F0.csv");
            string file1Path = Path.Combine(workDirectory, "file_F1.csv");
            string file2Path = Path.Combine(workDirectory, "file_F2.csv");

            // Применяем фильтр по континенту/категории, если задан
            bool noFilter = string.IsNullOrWhiteSpace(continentFilter)
                            || continentFilter == "Все континенты"
                            || continentFilter == "Все записи";

            var records = noFilter
                ? new List<Country>(sourceList)
                : sourceList.Where(c => c.Continent.Equals(continentFilter, StringComparison.OrdinalIgnoreCase)).ToList();

            // Сохраняем исходный файл F0
            InitialCountries.SaveToFile(file0Path, records);

            long comps = 0;
            long fileOps = records.Count;
            int stepNum = 1;

            string filterDesc = noFilter
                ? "без фильтрации"
                : $"категория «{continentFilter}»";

            var f0Initial = records.Select((c, i) => CountryRunRecord.Create(c, 0, i == 0)).ToList();

            steps.Add(ExternalSortStep.CreateSnapshot(
                stepNum++, 0, "Инициализация",
                f0Initial, Enumerable.Empty<CountryRunRecord>(), Enumerable.Empty<CountryRunRecord>(),
                null, null, null, "—", "", "", null, "F0", comps, fileOps,
                $"Подготовили исходный файл F0 ({records.Count} записей {filterDesc}). " +
                $"Сортируем по полю «{sortAttribute}» ({(ascending ? "по возрастанию" : "по убыванию")}).",
                $"Записан файл F0: {records.Count} строк"));

            if (records.Count <= 1)
            {
                steps.Add(ExternalSortStep.CreateSnapshot(
                    stepNum++, 0, "Завершено",
                    f0Initial, Enumerable.Empty<CountryRunRecord>(), Enumerable.Empty<CountryRunRecord>(),
                    null, null, null, "OK", "", "", null, "F0", comps, fileOps,
                    "В файле всего одна или ноль записей — сортировка не требуется!",
                    "Сортировка завершена (тривиальный случай)"));
                return steps;
            }

            if (method == ExternalSortMethod.DirectMerge)
            {
                ExecuteDirectMerge(file0Path, file1Path, file2Path, sortAttribute, ascending, steps, ref stepNum, ref comps, ref fileOps);
            }
            else
            {
                ExecuteNaturalMerge(file0Path, file1Path, file2Path, sortAttribute, ascending, steps, ref stepNum, ref comps, ref fileOps);
            }

            return steps;
        }

        #region Прямое простое слияние (Direct Merge)

        private static void ExecuteDirectMerge(
            string f0, string f1, string f2,
            string sortAttr, bool asc,
            List<ExternalSortStep> steps,
            ref int stepNum, ref long comps, ref long fileOps)
        {
            int pass = 1;
            int runSize = 1;
            int totalRecords = InitialCountries.LoadFromFile(f0).Count;

            while (runSize < totalRecords)
            {
                // ФАЗА 1: Распределение из F0 в F1 и F2 блоками по runSize с цветными сериями
                var f0List = InitialCountries.LoadFromFile(f0);
                var f1RunItems = new List<CountryRunRecord>();
                var f2RunItems = new List<CountryRunRecord>();

                steps.Add(ExternalSortStep.CreateSnapshot(
                    stepNum++, pass, "Разбиение на серии",
                    f0List.Select((c, i) => CountryRunRecord.Create(c, i / runSize, i % runSize == 0)),
                    f1RunItems, f2RunItems, null, null, null, "Разбиение", "", "", null, "F1/F2", comps, fileOps,
                    $"[Проход {pass}] Начинаем разбиение файла F0 на серии длиной {runSize}. " +
                    $"Поочередно перекладываем блоки по {runSize} карточек в файлы F1 и F2.",
                    $"Фаза разбиения: серия={runSize}"));

                bool toFile1 = true;
                int currentInRun = 0;
                int runCounter = 0;

                for (int i = 0; i < f0List.Count; i++)
                {
                    var record = f0List[i];
                    fileOps++;

                    bool isHeader = currentInRun == 0;
                    var runItem = CountryRunRecord.Create(record, runCounter, isHeader);

                    string targetName = toFile1 ? "F1" : "F2";
                    if (toFile1) f1RunItems.Add(runItem);
                    else f2RunItems.Add(runItem);

                    // Шаг полета карточки из F0 в F1 или F2
                    var remainingF0 = f0List.Skip(i + 1).Select((c, idx) => CountryRunRecord.Create(c, (i + 1 + idx) / runSize, (i + 1 + idx) % runSize == 0));

                    steps.Add(ExternalSortStep.CreateSnapshot(
                        stepNum++, pass, $"Разбиение в {targetName}",
                        remainingF0, f1RunItems, f2RunItems,
                        record, null, null, "➔", "F0", targetName, record, targetName, comps, fileOps,
                        $"Карточка «{record.Name}» летит из F0 во вспомогательный файл {targetName} (в серию #{runCounter + 1}).",
                        $"Запись в {targetName}: {record.Name}"));

                    currentInRun++;
                    if (currentInRun == runSize)
                    {
                        toFile1 = !toFile1;
                        currentInRun = 0;
                        runCounter++;
                    }
                }

                // Записываем во временные файлы на диске
                InitialCountries.SaveToFile(f1, f1RunItems.ConvertAll(i => i.Country));
                InitialCountries.SaveToFile(f2, f2RunItems.ConvertAll(i => i.Country));

                // ФАЗА 2: Слияние серий из F1 и F2 обратно в F0
                var f1List = f1RunItems;
                var f2List = f2RunItems;
                var newF0Items = new List<CountryRunRecord>();
                int i1 = 0, i2 = 0;
                int mergedRunId = 0;

                while (i1 < f1List.Count || i2 < f2List.Count)
                {
                    int run1Count = 0;
                    int run2Count = 0;
                    bool isMergedRunHeader = true;

                    while (run1Count < runSize && i1 < f1List.Count &&
                           run2Count < runSize && i2 < f2List.Count)
                    {
                        comps++;
                        var item1 = f1List[i1];
                        var item2 = f2List[i2];
                        var c1 = item1.Country;
                        var c2 = item2.Country;
                        int cmp = c1.CompareTo(c2, sortAttr, asc);

                        string compSymbol = cmp <= 0 ? "≤" : ">";
                        var winner = cmp <= 0 ? c1 : c2;
                        string winnerFile = cmp <= 0 ? "F1" : "F2";

                        var winnerRunItem = CountryRunRecord.Create(winner, mergedRunId, isMergedRunHeader);
                        isMergedRunHeader = false;
                        newF0Items.Add(winnerRunItem);
                        fileOps++;

                        if (cmp <= 0) { i1++; run1Count++; }
                        else { i2++; run2Count++; }

                        string cmpMsg = $"Сравнение на арене: «{c1.Name}» ({c1.GetAttributeValue(sortAttr):N0}) из F1 " +
                                        $"против «{c2.Name}» ({c2.GetAttributeValue(sortAttr):N0}) из F2. " +
                                        $"Побеждает «{winner.Name}»! Карточка летит в файл F0.";

                        steps.Add(ExternalSortStep.CreateSnapshot(
                            stepNum++, pass, "Поединок слияния",
                            newF0Items, f1List.Skip(i1), f2List.Skip(i2),
                            c1, c2, winner, compSymbol, winnerFile, "F0", winner, "F0", comps, fileOps,
                            cmpMsg, $"Победа {winner.Name}: запись в F0"));
                    }

                    // Дописываем остаток серии из F1
                    while (run1Count < runSize && i1 < f1List.Count)
                    {
                        var rem1Item = f1List[i1++];
                        run1Count++;
                        var added = CountryRunRecord.Create(rem1Item.Country, mergedRunId, isMergedRunHeader);
                        isMergedRunHeader = false;
                        newF0Items.Add(added);
                        fileOps++;

                        steps.Add(ExternalSortStep.CreateSnapshot(
                            stepNum++, pass, "Слияние (хвост F1)",
                            newF0Items, f1List.Skip(i1), f2List.Skip(i2),
                            rem1Item.Country, null, rem1Item.Country, "Остаток", "F1", "F0", rem1Item.Country, "F0", comps, fileOps,
                            $"Серия из F2 исчерпана! Оставшаяся карточка «{rem1Item.Country.Name}» из F1 отправляется в F0.",
                            $"Запись остатка F1: {rem1Item.Country.Name}"));
                    }

                    // Дописываем остаток серии из F2
                    while (run2Count < runSize && i2 < f2List.Count)
                    {
                        var rem2Item = f2List[i2++];
                        run2Count++;
                        var added = CountryRunRecord.Create(rem2Item.Country, mergedRunId, isMergedRunHeader);
                        isMergedRunHeader = false;
                        newF0Items.Add(added);
                        fileOps++;

                        steps.Add(ExternalSortStep.CreateSnapshot(
                            stepNum++, pass, "Слияние (хвост F2)",
                            newF0Items, f1List.Skip(i1), f2List.Skip(i2),
                            null, rem2Item.Country, rem2Item.Country, "Остаток", "F2", "F0", rem2Item.Country, "F0", comps, fileOps,
                            $"Серия из F1 исчерпана! Оставшаяся карточка «{rem2Item.Country.Name}» из F2 отправляется в F0.",
                            $"Запись остатка F2: {rem2Item.Country.Name}"));
                    }

                    mergedRunId++;
                }

                // Перезаписываем F0
                InitialCountries.SaveToFile(f0, newF0Items.ConvertAll(i => i.Country));
                runSize *= 2;
                pass++;
            }

            var finalSorted = InitialCountries.LoadFromFile(f0);
            var finalRunItems = finalSorted.Select((c, i) => CountryRunRecord.Create(c, 0, i == 0)).ToList();

            steps.Add(ExternalSortStep.CreateSnapshot(
                stepNum++, pass - 1, "Завершено",
                finalRunItems, Enumerable.Empty<CountryRunRecord>(), Enumerable.Empty<CountryRunRecord>(),
                null, null, null, "Успех", "", "", null, "F0", comps, fileOps,
                $"Внешняя сортировка прямым слиянием успешно завершена! Итоговый файл F0 содержит {finalSorted.Count} отсортированных записей.",
                $"Сортировка завершена. Всего сравнений: {comps}, дисковых операций: {fileOps}"));
        }

        #endregion

        #region Естественное слияние (Natural Merge)

        private static void ExecuteNaturalMerge(
            string f0, string f1, string f2,
            string sortAttr, bool asc,
            List<ExternalSortStep> steps,
            ref int stepNum, ref long comps, ref long fileOps)
        {
            int pass = 1;
            bool isSorted = false;

            while (!isSorted)
            {
                var f0List = InitialCountries.LoadFromFile(f0);
                var f1RunItems = new List<CountryRunRecord>();
                var f2RunItems = new List<CountryRunRecord>();

                steps.Add(ExternalSortStep.CreateSnapshot(
                    stepNum++, pass, "Поиск естественных серий",
                    f0List.Select((c, i) => CountryRunRecord.Create(c, 0, i == 0)),
                    f1RunItems, f2RunItems, null, null, null, "Поиск серий", "", "", null, "F1/F2", comps, fileOps,
                    $"[Проход {pass}] Ищем в файле F0 естественные цепочки стран, которые уже идут в нужном порядке. " +
                    $"Каждую найденную серию отправляем по очереди в F1 или F2.",
                    $"Поиск естественных серий (проход {pass})"));

                bool toFile1 = true;
                int runsCount = 0;

                if (f0List.Count > 0)
                {
                    runsCount = 1;
                    var firstItem = CountryRunRecord.Create(f0List[0], runsCount - 1, true);
                    if (toFile1) f1RunItems.Add(firstItem);
                    else f2RunItems.Add(firstItem);
                    fileOps++;

                    string target1 = toFile1 ? "F1" : "F2";
                    steps.Add(ExternalSortStep.CreateSnapshot(
                        stepNum++, pass, $"Серия #{runsCount} ➔ {target1}",
                        f0List.Skip(1).Select((c, idx) => CountryRunRecord.Create(c, 0, false)),
                        f1RunItems, f2RunItems,
                        f0List[0], null, null, "➔", "F0", target1, f0List[0], target1, comps, fileOps,
                        $"Начало серии #{runsCount}: страна «{f0List[0].Name}» перелетает в {target1}.",
                        $"Запись серии #{runsCount} в {target1}"));

                    for (int i = 1; i < f0List.Count; i++)
                    {
                        comps++;
                        var prev = f0List[i - 1];
                        var curr = f0List[i];
                        int cmp = prev.CompareTo(curr, sortAttr, asc);

                        bool newRunStarted = cmp > 0;
                        if (newRunStarted)
                        {
                            runsCount++;
                            toFile1 = !toFile1;
                        }

                        var item = CountryRunRecord.Create(curr, runsCount - 1, newRunStarted);
                        string target = toFile1 ? "F1" : "F2";

                        if (toFile1) f1RunItems.Add(item);
                        else f2RunItems.Add(item);
                        fileOps++;

                        var remF0 = f0List.Skip(i + 1).Select((c, idx) => CountryRunRecord.Create(c, 0, false));

                        steps.Add(ExternalSortStep.CreateSnapshot(
                            stepNum++, pass, newRunStarted ? $"Новая серия #{runsCount} ➔ {target}" : $"Серия #{runsCount} ➔ {target}",
                            remF0, f1RunItems, f2RunItems,
                            curr, null, null, "➔", "F0", target, curr, target, comps, fileOps,
                            newRunStarted 
                                ? $"Порядок нарушился («{prev.Name}» > «{curr.Name}»). Начинается серия #{runsCount}! Карточка «{curr.Name}» летит в {target}."
                                : $"Порядок сохраняется («{prev.Name}» <= «{curr.Name}»). «{curr.Name}» продолжает серию #{runsCount} в {target}.",
                            $"Запись в {target}: {curr.Name}"));
                    }
                }

                InitialCountries.SaveToFile(f1, f1RunItems.ConvertAll(i => i.Country));
                InitialCountries.SaveToFile(f2, f2RunItems.ConvertAll(i => i.Country));

                if (runsCount <= 1)
                {
                    isSorted = true;
                    break;
                }

                // Слияние естественных серий
                var newF0Items = new List<CountryRunRecord>();
                int i1 = 0, i2 = 0;
                int mergedRunId = 0;

                while (i1 < f1RunItems.Count || i2 < f2RunItems.Count)
                {
                    bool isMergedRunHeader = true;

                    while (i1 < f1RunItems.Count && i2 < f2RunItems.Count)
                    {
                        comps++;
                        var item1 = f1RunItems[i1];
                        var item2 = f2RunItems[i2];
                        var c1 = item1.Country;
                        var c2 = item2.Country;
                        int cmp = c1.CompareTo(c2, sortAttr, asc);

                        var winner = cmp <= 0 ? c1 : c2;
                        string winnerFile = cmp <= 0 ? "F1" : "F2";
                        string compSym = cmp <= 0 ? "≤" : ">";

                        var winnerItem = CountryRunRecord.Create(winner, mergedRunId, isMergedRunHeader);
                        isMergedRunHeader = false;
                        newF0Items.Add(winnerItem);
                        fileOps++;

                        if (cmp <= 0) i1++;
                        else i2++;

                        steps.Add(ExternalSortStep.CreateSnapshot(
                            stepNum++, pass, "Поединок естественных серий",
                            newF0Items, f1RunItems.Skip(i1), f2RunItems.Skip(i2),
                            c1, c2, winner, compSym, winnerFile, "F0", winner, "F0", comps, fileOps,
                            $"Сравнение: «{c1.Name}» против «{c2.Name}». Побеждает «{winner.Name}» и отправляется в файл F0!",
                            $"Слияние в F0: {winner.Name}"));

                        // Проверяем конец серии
                        if (cmp <= 0)
                        {
                            if (i1 < f1RunItems.Count && c1.CompareTo(f1RunItems[i1].Country, sortAttr, asc) > 0)
                            {
                                while (i2 < f2RunItems.Count)
                                {
                                    var next2 = f2RunItems[i2++];
                                    var tailItem = CountryRunRecord.Create(next2.Country, mergedRunId, isMergedRunHeader);
                                    isMergedRunHeader = false;
                                    newF0Items.Add(tailItem);
                                    fileOps++;

                                    steps.Add(ExternalSortStep.CreateSnapshot(
                                        stepNum++, pass, "Слияние (хвост F2)",
                                        newF0Items, f1RunItems.Skip(i1), f2RunItems.Skip(i2),
                                        null, next2.Country, next2.Country, "Хвост", "F2", "F0", next2.Country, "F0", comps, fileOps,
                                        $"Серия F1 завершилась. Дописываем «{next2.Country.Name}» из F2 в F0.",
                                        $"Слияние в F0: {next2.Country.Name}"));

                                    if (i2 < f2RunItems.Count && next2.Country.CompareTo(f2RunItems[i2].Country, sortAttr, asc) > 0)
                                        break;
                                }
                                break;
                            }
                        }
                        else
                        {
                            if (i2 < f2RunItems.Count && c2.CompareTo(f2RunItems[i2].Country, sortAttr, asc) > 0)
                            {
                                while (i1 < f1RunItems.Count)
                                {
                                    var next1 = f1RunItems[i1++];
                                    var tailItem = CountryRunRecord.Create(next1.Country, mergedRunId, isMergedRunHeader);
                                    isMergedRunHeader = false;
                                    newF0Items.Add(tailItem);
                                    fileOps++;

                                    steps.Add(ExternalSortStep.CreateSnapshot(
                                        stepNum++, pass, "Слияние (хвост F1)",
                                        newF0Items, f1RunItems.Skip(i1), f2RunItems.Skip(i2),
                                        next1.Country, null, next1.Country, "Хвост", "F1", "F0", next1.Country, "F0", comps, fileOps,
                                        $"Серия F2 завершилась. Дописываем «{next1.Country.Name}» из F1 в F0.",
                                        $"Слияние в F0: {next1.Country.Name}"));

                                    if (i1 < f1RunItems.Count && next1.Country.CompareTo(f1RunItems[i1].Country, sortAttr, asc) > 0)
                                        break;
                                }
                                break;
                            }
                        }
                    }

                    // Дописываем остатки
                    while (i1 < f1RunItems.Count)
                    {
                        var rem1 = f1RunItems[i1++];
                        newF0Items.Add(CountryRunRecord.Create(rem1.Country, mergedRunId, isMergedRunHeader));
                        isMergedRunHeader = false;
                        fileOps++;

                        steps.Add(ExternalSortStep.CreateSnapshot(
                            stepNum++, pass, "Остаток F1",
                            newF0Items, f1RunItems.Skip(i1), f2RunItems.Skip(i2),
                            rem1.Country, null, rem1.Country, "Остаток", "F1", "F0", rem1.Country, "F0", comps, fileOps,
                            $"Дописываем оставшуюся запись «{rem1.Country.Name}» из F1 в F0.",
                            $"Остаток в F0: {rem1.Country.Name}"));
                    }

                    while (i2 < f2RunItems.Count)
                    {
                        var rem2 = f2RunItems[i2++];
                        newF0Items.Add(CountryRunRecord.Create(rem2.Country, mergedRunId, isMergedRunHeader));
                        isMergedRunHeader = false;
                        fileOps++;

                        steps.Add(ExternalSortStep.CreateSnapshot(
                            stepNum++, pass, "Остаток F2",
                            newF0Items, f1RunItems.Skip(i1), f2RunItems.Skip(i2),
                            null, rem2.Country, rem2.Country, "Остаток", "F2", "F0", rem2.Country, "F0", comps, fileOps,
                            $"Дописываем оставшуюся запись «{rem2.Country.Name}» из F2 в F0.",
                            $"Остаток в F0: {rem2.Country.Name}"));
                    }

                    mergedRunId++;
                }

                InitialCountries.SaveToFile(f0, newF0Items.ConvertAll(i => i.Country));
                pass++;
            }

            var finalResult = InitialCountries.LoadFromFile(f0);
            var finalRunItems = finalResult.Select((c, i) => CountryRunRecord.Create(c, 0, i == 0)).ToList();

            steps.Add(ExternalSortStep.CreateSnapshot(
                stepNum++, pass - 1, "Завершено",
                finalRunItems, Enumerable.Empty<CountryRunRecord>(), Enumerable.Empty<CountryRunRecord>(),
                null, null, null, "Успех", "", "", null, "F0", comps, fileOps,
                $"Внешняя сортировка естественным слиянием успешно завершена! Файл F0 отсортирован ({finalResult.Count} записей).",
                $"Сортировка завершена. Сравнений: {comps}, дисковых операций: {fileOps}"));
        }

        #endregion
    }
}
