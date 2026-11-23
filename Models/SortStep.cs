using System;
using System.Collections.Generic;

namespace SortingLabApp.Models
{
    public enum StepActionType
    {
        None,
        Compare,
        Swap,
        Assign,
        Pivot,
        Gap,
        Run,
        Sorted,
        Completed
    }

    /// <summary>
    /// Снимок состояния массива для пошаговой визуализации с историей (шаг назад / шаг вперед).
    /// Включает уникальные идентификаторы элементов (ElementIds) для плавной физической анимации перемещения.
    /// </summary>
    public class SortStep
    {
        public int StepNumber { get; set; }
        public int[] ArrayState { get; set; } = Array.Empty<int>();
        
        // Уникальный ID каждого элемента в текущих ячейках (0..N-1) для отслеживания траектории движения
        public int[] ElementIds { get; set; } = Array.Empty<int>();

        // Индексы ключевых элементов на текущем шаге
        public int Index1 { get; set; } = -1;
        public int Index2 { get; set; } = -1;
        public int PivotIndex { get; set; } = -1;
        
        // Индексы уже гарантированно отсортированных элементов
        public HashSet<int> SortedIndices { get; set; } = new();

        public StepActionType ActionType { get; set; } = StepActionType.None;

        // Пояснение простыми словами (для 6-классника)
        public string Explanation { get; set; } = string.Empty;

        // Технический лог операции
        public string TechnicalLog { get; set; } = string.Empty;

        // Текущие счетчики
        public long ComparisonsCount { get; set; }
        public long SwapsCount { get; set; }

        public static SortStep CreateSnapshot(
            int stepNum,
            int[] array,
            int[]? elementIds,
            int idx1,
            int idx2,
            int pivot,
            StepActionType action,
            string explanation,
            string technicalLog,
            long comps,
            long swaps,
            IEnumerable<int>? sorted = null)
        {
            var step = new SortStep
            {
                StepNumber = stepNum,
                ArrayState = (int[])array.Clone(),
                ElementIds = elementIds != null ? (int[])elementIds.Clone() : Array.Empty<int>(),
                Index1 = idx1,
                Index2 = idx2,
                PivotIndex = pivot,
                ActionType = action,
                Explanation = explanation,
                TechnicalLog = technicalLog,
                ComparisonsCount = comps,
                SwapsCount = swaps
            };

            if (sorted != null)
            {
                foreach (var s in sorted)
                    step.SortedIndices.Add(s);
            }

            return step;
        }
    }
}
