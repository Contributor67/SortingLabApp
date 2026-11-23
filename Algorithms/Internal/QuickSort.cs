using System;
using System.Collections.Generic;
using System.Linq;
using SortingLabApp.Models;

namespace SortingLabApp.Algorithms.Internal
{
    /// <summary>
    /// Быстрая сортировка Хоара (Quick Sort).
    /// </summary>
    public static class QuickSort
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
            var sortedIndices = new HashSet<int>();

            steps.Add(SortStep.CreateSnapshot(
                stepNum++, arr, ids, -1, -1, -1, StepActionType.None,
                "Начинаем быструю сортировку Хоара! Алгоритм делит массив на две команды вокруг опорного элемента (лидера).",
                "Инициализация массива", comps, swaps));

            void QuickSortRecursive(int low, int high)
            {
                if (low >= high)
                {
                    if (low == high) sortedIndices.Add(low);
                    return;
                }

                int pivotIndex = low + (high - low) / 2;
                int pivotValue = arr[pivotIndex];

                steps.Add(SortStep.CreateSnapshot(
                    stepNum++, arr, ids, -1, -1, pivotIndex, StepActionType.Pivot,
                    $"В подмассиве от [{low}] до [{high}] выбираем опорный элемент (лидера): {pivotValue} на позиции [{pivotIndex}]. " +
                    $"Все числа меньше {pivotValue} пойдут влево, а большие — вправо.",
                    $"Выбор pivot: arr[{pivotIndex}] = {pivotValue} (диапазон [{low}..{high}])", comps, swaps, sortedIndices));

                int i = low;
                int j = high;

                while (i <= j)
                {
                    while (arr[i] < pivotValue)
                    {
                        comps++;
                        steps.Add(SortStep.CreateSnapshot(
                            stepNum++, arr, ids, i, -1, pivotIndex, StepActionType.Compare,
                            $"Число {arr[i]} на позиции [{i}] меньше лидера ({pivotValue}). Оно уже в правильной (левой) команде, шагаем вправо.",
                            $"Сравнение: arr[{i}] = {arr[i]} < pivot ({pivotValue})", comps, swaps, sortedIndices));
                        i++;
                    }
                    comps++;

                    while (arr[j] > pivotValue)
                    {
                        comps++;
                        steps.Add(SortStep.CreateSnapshot(
                            stepNum++, arr, ids, -1, j, pivotIndex, StepActionType.Compare,
                            $"Число {arr[j]} на позиции [{j}] больше лидера ({pivotValue}). Оно уже в правильной (правой) команде, шагаем влево.",
                            $"Сравнение: arr[{j}] = {arr[j]} > pivot ({pivotValue})", comps, swaps, sortedIndices));
                        j--;
                    }
                    comps++;

                    if (i <= j)
                    {
                        if (i != j)
                        {
                            swaps++;
                            (arr[i], arr[j]) = (arr[j], arr[i]);
                            (ids[i], ids[j]) = (ids[j], ids[i]);

                            if (i == pivotIndex) pivotIndex = j;
                            else if (j == pivotIndex) pivotIndex = i;

                            steps.Add(SortStep.CreateSnapshot(
                                stepNum++, arr, ids, i, j, pivotIndex, StepActionType.Swap,
                                $"Числа {arr[j]} (было слева) и {arr[i]} (было справа) оказались не в своих командах. Меняем их местами!",
                                $"Перестановка: arr[{i}] <-> arr[{j}]", comps, swaps, sortedIndices));
                        }

                        i++;
                        j--;
                    }
                }

                if (low < j)
                {
                    QuickSortRecursive(low, j);
                }
                else if (low <= j)
                {
                    sortedIndices.Add(low);
                }

                if (i < high)
                {
                    QuickSortRecursive(i, high);
                }
                else if (i <= high)
                {
                    sortedIndices.Add(high);
                }
            }

            QuickSortRecursive(0, n - 1);

            for (int k = 0; k < n; k++) sortedIndices.Add(k);

            steps.Add(SortStep.CreateSnapshot(
                stepNum++, arr, ids, -1, -1, -1, StepActionType.Completed,
                "Превосходно! Быстрая сортировка завершена. Все подмассивы объединены и идеально упорядочены!",
                $"Сортировка завершена. Всего сравнений: {comps}, перестановок: {swaps}", comps, swaps, sortedIndices));

            return steps;
        }
    }
}
