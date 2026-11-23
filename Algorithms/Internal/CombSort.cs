using System;
using System.Collections.Generic;
using System.Linq;
using SortingLabApp.Models;

namespace SortingLabApp.Algorithms.Internal
{
    /// <summary>
    /// Сортировка расчёской (Comb Sort).
    /// </summary>
    public static class CombSort
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

            double shrinkFactor = 1.3;
            int gap = n;
            bool swapped = true;

            steps.Add(SortStep.CreateSnapshot(
                stepNum++, arr, ids, -1, -1, -1, StepActionType.None,
                "Начинаем сортировку расчёской! Исходный массив готов к обработке.",
                "Инициализация массива", comps, swaps));

            while (gap > 1 || swapped)
            {
                gap = (int)(gap / shrinkFactor);
                if (gap < 1) gap = 1;

                steps.Add(SortStep.CreateSnapshot(
                    stepNum++, arr, ids, -1, -1, -1, StepActionType.Gap,
                    gap > 1 
                        ? $"Устанавливаем шаг расчески (интервал) gap = {gap}. Будем сравнивать элементы, стоящие через {gap} позиций друг от друга."
                        : "Шаг расчески стал равен 1! Теперь алгоритм работает как пузырек, но массив уже почти отсортирован.",
                    $"Установка интервала gap = {gap}", comps, swaps));

                swapped = false;

                for (int i = 0; i + gap < n; i++)
                {
                    int j = i + gap;
                    comps++;

                    bool needSwap = arr[i] > arr[j];
                    string cmpExp = $"Сравниваем элементы на позициях [{i}] (число {arr[i]}) и [{j}] (число {arr[j]}).";
                    if (needSwap)
                    {
                        cmpExp += $" Так как {arr[i]} > {arr[j]}, их нужно поменять местами!";
                    }
                    else
                    {
                        cmpExp += $" Порядок верный ({arr[i]} <= {arr[j]}), перестановка не требуется.";
                    }

                    steps.Add(SortStep.CreateSnapshot(
                        stepNum++, arr, ids, i, j, -1, StepActionType.Compare,
                        cmpExp,
                        $"Сравнение: arr[{i}] = {arr[i]} с arr[{j}] = {arr[j]}", comps, swaps));

                    if (needSwap)
                    {
                        swaps++;
                        (arr[i], arr[j]) = (arr[j], arr[i]);
                        (ids[i], ids[j]) = (ids[j], ids[i]);
                        swapped = true;

                        steps.Add(SortStep.CreateSnapshot(
                            stepNum++, arr, ids, i, j, -1, StepActionType.Swap,
                            $"Поменяли местами: {arr[j]} переместилось вправо на позицию [{j}], а {arr[i]} подвинулось влево на позицию [{i}].",
                            $"Перестановка: arr[{i}] <-> arr[{j}]", comps, swaps));
                    }
                }
            }

            var allSorted = new HashSet<int>();
            for (int i = 0; i < n; i++) allSorted.Add(i);

            steps.Add(SortStep.CreateSnapshot(
                stepNum++, arr, ids, -1, -1, -1, StepActionType.Completed,
                "Ура! Сортировка расчёской успешно завершена. Все числа выстроились по возрастанию!",
                $"Сортировка завершена. Всего сравнений: {comps}, перестановок: {swaps}", comps, swaps, allSorted));

            return steps;
        }
    }
}
