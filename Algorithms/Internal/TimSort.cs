using System;
using System.Collections.Generic;
using System.Linq;
using SortingLabApp.Models;

namespace SortingLabApp.Algorithms.Internal
{
    /// <summary>
    /// Сортировка Тимсорт (Tim Sort).
    /// </summary>
    public static class TimSort
    {
        private static int CalculateMinRun(int n)
        {
            if (n <= 16) return Math.Max(3, n / 2);
            return Math.Clamp(n / 4, 4, 8);
        }

        public static List<SortStep> GenerateSteps(int[] sourceArray)
        {
            var steps = new List<SortStep>();
            var arr = (int[])sourceArray.Clone();
            int n = arr.Length;
            int[] ids = Enumerable.Range(0, n).ToArray();

            long comps = 0;
            long swaps = 0;
            int stepNum = 1;
            var sortedIndices = new HashSet<int>();

            int minRun = CalculateMinRun(n);

            steps.Add(SortStep.CreateSnapshot(
                stepNum++, arr, ids, -1, -1, -1, StepActionType.None,
                $"Начинаем Тимсорт! Рассчитали базовый размер серии minrun = {minRun}. " +
                $"Алгоритм сначала разобьет массив на удобные блоки по {minRun} элементов и отсортирует их вставками, " +
                $"а затем аккуратно сольет блоки вместе.",
                $"Инициализация Тимсорт: n = {n}, minrun = {minRun}", comps, swaps));

            // Фаза 1: Сортировка каждого блока minRun простыми вставками
            for (int start = 0; start < n; start += minRun)
            {
                int end = Math.Min(start + minRun - 1, n - 1);

                steps.Add(SortStep.CreateSnapshot(
                    stepNum++, arr, ids, start, end, -1, StepActionType.Run,
                    $"Берем блок элементов от позиции [{start}] до [{end}] и сортируем его быстрыми вставками.",
                    $"Сортировка блока [{start}..{end}] вставками", comps, swaps, sortedIndices));

                for (int i = start + 1; i <= end; i++)
                {
                    int key = arr[i];
                    int keyId = ids[i];
                    int j = i - 1;

                    steps.Add(SortStep.CreateSnapshot(
                        stepNum++, arr, ids, i, -1, -1, StepActionType.Assign,
                        $"Выбираем карточку с числом {key} на позиции [{i}] и ищем для нее место слева в текущем блоке.",
                        $"Вставка: берем ключ arr[{i}] = {key}", comps, swaps, sortedIndices));

                    while (j >= start)
                    {
                        comps++;
                        bool needShift = arr[j] > key;

                        string cmpMsg = $"Сравниваем ключ {key} с числом {arr[j]} на позиции [{j}].";
                        if (needShift)
                        {
                            cmpMsg += $" Так как {arr[j]} > {key}, сдвигаем {arr[j]} вправо на позицию [{j + 1}].";
                        }
                        else
                        {
                            cmpMsg += $" Число {arr[j]} <= {key}. Место для {key} найдено на позиции [{j + 1}].";
                        }

                        steps.Add(SortStep.CreateSnapshot(
                            stepNum++, arr, ids, j, j + 1, -1, StepActionType.Compare,
                            cmpMsg,
                            $"Сравнение: arr[{j}] = {arr[j]} с key = {key}", comps, swaps, sortedIndices));

                        if (!needShift) break;

                        swaps++;
                        arr[j + 1] = arr[j];
                        ids[j + 1] = ids[j];

                        steps.Add(SortStep.CreateSnapshot(
                            stepNum++, arr, ids, j, j + 1, -1, StepActionType.Assign,
                            $"Сдвинули {arr[j]} на позицию [{j + 1}].",
                            $"Сдвиг: arr[{j + 1}] = {arr[j]}", comps, swaps, sortedIndices));

                        j--;
                    }

                    if (j + 1 != i)
                    {
                        swaps++;
                        arr[j + 1] = key;
                        ids[j + 1] = keyId;

                        steps.Add(SortStep.CreateSnapshot(
                            stepNum++, arr, ids, j + 1, -1, -1, StepActionType.Assign,
                            $"Вставили ключ {key} на освободившуюся позицию [{j + 1}].",
                            $"Установка ключа: arr[{j + 1}] = {key}", comps, swaps, sortedIndices));
                    }
                }
            }

            // Фаза 2: Слияние отсортированных блоков
            for (int size = minRun; size < n; size *= 2)
            {
                steps.Add(SortStep.CreateSnapshot(
                    stepNum++, arr, ids, -1, -1, -1, StepActionType.Gap,
                    $"Фаза слияния: объединяем соседние отсортированные блоки размером {size} в блоки размером {size * 2}.",
                    $"Слияние блоков размером {size}", comps, swaps, sortedIndices));

                for (int left = 0; left < n; left += 2 * size)
                {
                    int mid = left + size - 1;
                    int right = Math.Min(left + 2 * size - 1, n - 1);

                    if (mid >= right) continue;

                    steps.Add(SortStep.CreateSnapshot(
                        stepNum++, arr, ids, left, right, mid, StepActionType.Run,
                        $"Сливаем два соседних упорядоченных блока: [{left}..{mid}] и [{mid + 1}..{right}].",
                        $"Слияние диапазонов [{left}..{mid}] и [{mid + 1}..{right}]", comps, swaps, sortedIndices));

                    int len1 = mid - left + 1;
                    int len2 = right - mid;

                    int[] leftArr = new int[len1];
                    int[] leftIds = new int[len1];
                    int[] rightArr = new int[len2];
                    int[] rightIds = new int[len2];

                    Array.Copy(arr, left, leftArr, 0, len1);
                    Array.Copy(ids, left, leftIds, 0, len1);
                    Array.Copy(arr, mid + 1, rightArr, 0, len2);
                    Array.Copy(ids, mid + 1, rightIds, 0, len2);

                    int iL = 0, iR = 0, k = left;

                    while (iL < len1 && iR < len2)
                    {
                        comps++;
                        int valL = leftArr[iL];
                        int idL = leftIds[iL];
                        int valR = rightArr[iR];
                        int idR = rightIds[iR];

                        string cmpMerge = $"Сравниваем головы двух блоков: {valL} и {valR}.";

                        if (valL <= valR)
                        {
                            cmpMerge += $" Меньше или равно число {valL}, записываем его в позицию [{k}].";
                            swaps++;
                            arr[k] = valL;
                            ids[k] = idL;

                            steps.Add(SortStep.CreateSnapshot(
                                stepNum++, arr, ids, k, -1, -1, StepActionType.Assign,
                                cmpMerge,
                                $"Слияние: запись arr[{k}] = {valL}", comps, swaps, sortedIndices));
                            iL++;
                        }
                        else
                        {
                            cmpMerge += $" Меньше число {valR}, записываем его в позицию [{k}].";
                            swaps++;
                            arr[k] = valR;
                            ids[k] = idR;

                            steps.Add(SortStep.CreateSnapshot(
                                stepNum++, arr, ids, k, -1, -1, StepActionType.Assign,
                                cmpMerge,
                                $"Слияние: запись arr[{k}] = {valR}", comps, swaps, sortedIndices));
                            iR++;
                        }
                        k++;
                    }

                    while (iL < len1)
                    {
                        swaps++;
                        arr[k] = leftArr[iL];
                        ids[k] = leftIds[iL];

                        steps.Add(SortStep.CreateSnapshot(
                            stepNum++, arr, ids, k, -1, -1, StepActionType.Assign,
                            $"Дописываем оставшийся элемент {leftArr[iL]} первого блока на позицию [{k}].",
                            $"Слияние (остаток L): arr[{k}] = {leftArr[iL]}", comps, swaps, sortedIndices));
                        iL++;
                        k++;
                    }

                    while (iR < len2)
                    {
                        swaps++;
                        arr[k] = rightArr[iR];
                        ids[k] = rightIds[iR];

                        steps.Add(SortStep.CreateSnapshot(
                            stepNum++, arr, ids, k, -1, -1, StepActionType.Assign,
                            $"Дописываем оставшийся элемент {rightArr[iR]} второго блока на позицию [{k}].",
                            $"Слияние (остаток R): arr[{k}] = {rightArr[iR]}", comps, swaps, sortedIndices));
                        iR++;
                        k++;
                    }
                }
            }

            for (int k = 0; k < n; k++) sortedIndices.Add(k);

            steps.Add(SortStep.CreateSnapshot(
                stepNum++, arr, ids, -1, -1, -1, StepActionType.Completed,
                "Успех! Тимсорт завершил работу. Все блоки слились в единый полностью упорядоченный массив!",
                $"Сортировка завершена. Всего сравнений: {comps}, перестановок/записей: {swaps}", comps, swaps, sortedIndices));

            return steps;
        }
    }
}
