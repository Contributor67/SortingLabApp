using System;

namespace SortingLabApp.Algorithms.Strings
{
    /// <summary>
    /// Быстрая сортировка массива строк в лексикографическом порядке (Hoare QuickSort).
    /// </summary>
    public static class StringQuickSort
    {
        public static void Sort(string[] array)
        {
            if (array == null || array.Length <= 1) return;
            SortInternal(array, 0, array.Length - 1);
        }

        private static void SortInternal(string[] array, int low, int high)
        {
            if (low >= high) return;

            string pivot = array[low + (high - low) / 2];
            int i = low;
            int j = high;

            while (i <= j)
            {
                while (string.CompareOrdinal(array[i], pivot) < 0)
                {
                    i++;
                }

                while (string.CompareOrdinal(array[j], pivot) > 0)
                {
                    j--;
                }

                if (i <= j)
                {
                    (array[i], array[j]) = (array[j], array[i]);
                    i++;
                    j--;
                }
            }

            if (low < j)
            {
                SortInternal(array, low, j);
            }

            if (i < high)
            {
                SortInternal(array, i, high);
            }
        }
    }
}
