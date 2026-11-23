using System;

namespace SortingLabApp.Algorithms.Strings
{
    /// <summary>
    /// Поразрядная сортировка строк по старшим разрядам (MSD Radix Sort / Most Significant Digit).
    /// Специально разработана для эффективной лексикографической сортировки строк переменной длины.
    /// Сложность O(N * W), где W - средняя длина строк.
    /// </summary>
    public static class MsdRadixSort
    {
        private const int R = 256; // Алфавит (Extended ASCII)
        private const int Cutoff = 15; // Порог переключения на сортировку вставками для маленьких подмассивов

        public static void Sort(string[] array)
        {
            if (array == null || array.Length <= 1) return;
            string[] aux = new string[array.Length];
            SortInternal(array, 0, array.Length - 1, 0, aux);
        }

        private static int CharAt(string s, int d)
        {
            if (d < s.Length)
            {
                char c = s[d];
                return c < R ? c : R - 1;
            }
            return -1; // Конец строки
        }

        private static void SortInternal(string[] a, int low, int high, int d, string[] aux)
        {
            // Для маленьких подмассивов быстрее сортировка вставками
            if (high <= low + Cutoff)
            {
                InsertionSort(a, low, high, d);
                return;
            }

            // Вычисление частот символов (R + 2: 0 для -1, 1..R для символов 0..R-1, R+1 для смещения)
            int[] count = new int[R + 2];

            for (int i = low; i <= high; i++)
            {
                int c = CharAt(a[i], d);
                count[c + 2]++;
            }

            // Преобразование счетчиков в индексы начала корзин
            for (int r = 0; r < R + 1; r++)
            {
                count[r + 1] += count[r];
            }

            // Распределение элементов по корзинам во вспомогательный массив
            for (int i = low; i <= high; i++)
            {
                int c = CharAt(a[i], d);
                aux[count[c + 1]++] = a[i];
            }

            // Копирование обратно в исходный массив
            for (int i = low; i <= high; i++)
            {
                a[i] = aux[i - low];
            }

            // Рекурсивная сортировка для каждой непустой корзины следующего разряда (d + 1)
            for (int r = 0; r < R; r++)
            {
                int bucketLow = low + count[r];
                int bucketHigh = low + count[r + 1] - 1;
                if (bucketLow < bucketHigh)
                {
                    SortInternal(a, bucketLow, bucketHigh, d + 1, aux);
                }
            }
        }

        private static void InsertionSort(string[] a, int low, int high, int d)
        {
            for (int i = low + 1; i <= high; i++)
            {
                for (int j = i; j > low && Less(a[j], a[j - 1], d); j--)
                {
                    (a[j], a[j - 1]) = (a[j - 1], a[j]);
                }
            }
        }

        private static bool Less(string v, string w, int d)
        {
            for (int i = d; i < Math.Min(v.Length, w.Length); i++)
            {
                if (v[i] < w[i]) return true;
                if (v[i] > w[i]) return false;
            }
            return v.Length < w.Length;
        }
    }
}
