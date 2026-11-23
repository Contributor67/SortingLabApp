using System;
using System.Collections.Generic;
using System.Linq;
using SortingLabApp.Models;

namespace SortingLabApp.Algorithms.Internal
{
    /// <summary>
    /// Сортировка методом Шелла (Shell Sort).
    /// </summary>
    public static class ShellSort
    {
        public static List<SortStep> GenerateSteps(int[] sourceArray)
        {
            var steps = new List<SortStep>();
            var arr = (int[])sourceArray.Clone();
            int n = arr.Length;
            int[] ids = Enumerable.Range(0, n).ToArray();

            long comps = 0;
            long swaps = 0;
            int stepNum = 1;

            steps.Add(SortStep.CreateSnapshot(
                stepNum++, arr, ids, -1, -1, -1, StepActionType.None,
                "Начинаем сортировку Шелла! Алгоритм будет наводить порядок сначала крупными шагами, а потом мелкими.",
                "Инициализация массива", comps, swaps));

            for (int gap = n / 2; gap > 0; gap /= 2)
            {
                steps.Add(SortStep.CreateSnapshot(
                    stepNum++, arr, ids, -1, -1, -1, StepActionType.Gap,
                    gap > 1
                        ? $"Выбираем шаг группировки gap = {gap}. Сортируем подпоследовательности элементов, перешагивая через {gap} позиций."
                        : "Шаг уменьшился до gap = 1! Теперь выполняется финальная сортировка вставками на почти упорядоченном массиве.",
                    $"Установка интервала gap = {gap}", comps, swaps));

                for (int i = gap; i < n; i++)
                {
                    int temp = arr[i];
                    int tempId = ids[i];
                    int j = i;

                    steps.Add(SortStep.CreateSnapshot(
                        stepNum++, arr, ids, i, -1, -1, StepActionType.Assign,
                        $"Берем элемент на позиции [{i}] со значением {temp}. Будем искать его место в цепочке с шагом {gap}.",
                        $"Выбор элемента для вставки: arr[{i}] = {temp}", comps, swaps));

                    while (j >= gap)
                    {
                        comps++;
                        int compIdx = j - gap;
                        bool needShift = arr[compIdx] > temp;

                        string cmpMsg = $"Сравниваем взятый элемент {temp} с числом {arr[compIdx]} на позиции [{compIdx}].";
                        if (needShift)
                        {
                            cmpMsg += $" Так как {arr[compIdx]} > {temp}, сдвигаем {arr[compIdx]} вправо на шаг {gap} (на позицию [{j}]).";
                        }
                        else
                        {
                            cmpMsg += $" Так как {arr[compIdx]} <= {temp}, место для числа {temp} найдено (позиция [{j}]).";
                        }

                        steps.Add(SortStep.CreateSnapshot(
                            stepNum++, arr, ids, compIdx, j, -1, StepActionType.Compare,
                            cmpMsg,
                            $"Сравнение: arr[{compIdx}] = {arr[compIdx]} с temp = {temp}", comps, swaps));

                        if (!needShift)
                        {
                            break;
                        }

                        swaps++;
                        arr[j] = arr[compIdx];
                        ids[j] = ids[compIdx];

                        steps.Add(SortStep.CreateSnapshot(
                            stepNum++, arr, ids, compIdx, j, -1, StepActionType.Assign,
                            $"Сдвинули {arr[j]} на позицию [{j}], освобождая ячейку для числа {temp}.",
                            $"Сдвиг: arr[{j}] = arr[{compIdx}] ({arr[j]})", comps, swaps));

                        j -= gap;
                    }

                    if (j != i)
                    {
                        swaps++;
                        arr[j] = temp;
                        ids[j] = tempId;

                        steps.Add(SortStep.CreateSnapshot(
                            stepNum++, arr, ids, j, -1, -1, StepActionType.Assign,
                            $"Поместили исходное число {temp} на освободившуюся позицию [{j}].",
                            $"Вставка: arr[{j}] = {temp}", comps, swaps));
                    }
                }
            }

            var allSorted = new HashSet<int>();
            for (int i = 0; i < n; i++) allSorted.Add(i);

            steps.Add(SortStep.CreateSnapshot(
                stepNum++, arr, ids, -1, -1, -1, StepActionType.Completed,
                "Отлично! Сортировка методом Шелла завершена. Все элементы расположены строго по возрастанию!",
                $"Сортировка завершена. Всего сравнений: {comps}, перестановок/записей: {swaps}", comps, swaps, allSorted));

            return steps;
        }
    }
}
