using System;
using System.IO;
using System.Linq;
using SortingLabApp.Algorithms.External;
using SortingLabApp.Algorithms.Internal;
using SortingLabApp.Algorithms.Strings;
using SortingLabApp.Data;

namespace SortingLabApp
{
    public static class VerificationTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("  ЗАПУСК АВТОМАТИЧЕСКИХ ТЕСТОВ ПРАКТИЧЕСКОГО ТЗ 4");
            Console.WriteLine("==================================================");

            TestInternalSorting();
            TestExternalSorting();
            TestStringSortingAndFrequencies();

            Console.WriteLine("\n[УСПЕХ] Все алгоритмы и тесты прошли проверку на 100%!");
        }

        private static void TestInternalSorting()
        {
            Console.WriteLine("\n--- ТЕСТИРОВАНИЕ ВНУТРЕННИХ СОРТИРОВОК (Задание 1) ---");

            var testCases = new int[][]
            {
                new int[] { 45, 12, 89, 3, 27, 65, 18, 92, 5, 33 },
                new int[] { 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 }, // обратный порядок
                new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, // уже отсортирован
                new int[] { 5, 5, 2, 2, 8, 8, 1, 1, 5, 2 },   // дубликаты
                new int[] { 42 },                             // 1 элемент
                new int[] { 99, 1 }                           // 2 элемента
            };

            foreach (var test in testCases)
            {
                var expected = (int[])test.Clone();
                Array.Sort(expected);

                // 1. CombSort
                var combSteps = CombSort.GenerateSteps(test);
                var combFinal = combSteps.Last().ArrayState;
                AssertEqual(expected, combFinal, "CombSort");
                AssertTrue(combSteps.All(s => !string.IsNullOrEmpty(s.Explanation)), "CombSort: пояснения присутствуют");

                // 2. ShellSort
                var shellSteps = ShellSort.GenerateSteps(test);
                var shellFinal = shellSteps.Last().ArrayState;
                AssertEqual(expected, shellFinal, "ShellSort");
                AssertTrue(shellSteps.All(s => !string.IsNullOrEmpty(s.Explanation)), "ShellSort: пояснения присутствуют");

                // 3. QuickSort
                var quickSteps = QuickSort.GenerateSteps(test);
                var quickFinal = quickSteps.Last().ArrayState;
                AssertEqual(expected, quickFinal, "QuickSort");
                AssertTrue(quickSteps.All(s => !string.IsNullOrEmpty(s.Explanation)), "QuickSort: пояснения присутствуют");

                // 4. TimSort
                var timSteps = TimSort.GenerateSteps(test);
                var timFinal = timSteps.Last().ArrayState;
                AssertEqual(expected, timFinal, "TimSort");
                AssertTrue(timSteps.All(s => !string.IsNullOrEmpty(s.Explanation)), "TimSort: пояснения присутствуют");
            }

            Console.WriteLine("[OK] CombSort, ShellSort, QuickSort, TimSort работают корректно на всех тестах.");
        }

        private static void TestExternalSorting()
        {
            Console.WriteLine("\n--- ТЕСТИРОВАНИЕ ВНЕШНЕЙ СОРТИРОВКИ (Задание 2) ---");

            var countries = InitialCountries.GetDefaultCountries();
            string testDir = Path.Combine(Path.GetTempPath(), "SortingLabApp_Tests_" + Guid.NewGuid());

            try
            {
                // Тест 1: Прямое слияние по населению (по возрастанию)
                var directSteps = ExternalSortService.ExecuteExternalSort(
                    countries, "Все континенты", "Население", true, ExternalSortMethod.DirectMerge, testDir);
                var directFinal = directSteps.Last().File0Records;
                AssertTrue(directFinal.Count == countries.Count, "Прямое слияние: сохранение количества записей");
                for (int i = 0; i < directFinal.Count - 1; i++)
                {
                    AssertTrue(directFinal[i].Population <= directFinal[i + 1].Population, 
                        $"Прямое слияние: порядок населения ({directFinal[i].Population} <= {directFinal[i + 1].Population})");
                }

                // Тест 2: Естественное слияние по площади (по убыванию) с фильтром по Европе
                var natSteps = ExternalSortService.ExecuteExternalSort(
                    countries, "Европа", "Площадь", false, ExternalSortMethod.NaturalMerge, testDir);
                var natFinal = natSteps.Last().File0Records;
                var europeCount = countries.Count(c => c.Continent == "Европа");
                AssertTrue(natFinal.Count == europeCount, "Естественное слияние: фильтрация по континенту корректна");
                for (int i = 0; i < natFinal.Count - 1; i++)
                {
                    AssertTrue(natFinal[i].Area >= natFinal[i + 1].Area, 
                        $"Естественное слияние: порядок площади по убыванию ({natFinal[i].Area} >= {natFinal[i + 1].Area})");
                }

                Console.WriteLine("[OK] Внешняя сортировка (прямое и естественное слияние файлов) работает безупречно.");
            }
            finally
            {
                if (Directory.Exists(testDir))
                {
                    try { Directory.Delete(testDir, true); } catch { }
                }
            }
        }

        private static void TestStringSortingAndFrequencies()
        {
            Console.WriteLine("\n--- ТЕСТИРОВАНИЕ СОРТИРОВКИ СЛОВ И БЕНЧМАРКА (Задание 3) ---");

            string sample = "Alice was beginning to get very tired of sitting by her sister on the bank and of having nothing to do alice alice sister";
            string[] tokens = TextGenerator.Tokenize(sample);

            var expected = (string[])tokens.Clone();
            Array.Sort(expected, StringComparer.Ordinal);

            // 1. StringQuickSort
            var quickArr = (string[])tokens.Clone();
            StringQuickSort.Sort(quickArr);
            AssertEqual(expected, quickArr, "StringQuickSort");

            // 2. MsdRadixSort
            var radixArr = (string[])tokens.Clone();
            MsdRadixSort.Sort(radixArr);
            AssertEqual(expected, radixArr, "MsdRadixSort");

            // 3. Частотный анализ
            var freqs = TextGenerator.CountFrequenciesFromSorted(radixArr);
            AssertTrue(freqs.Count > 0, "Частотный список не пустой");
            var aliceEntry = freqs.FirstOrDefault(f => f.Word == "alice");
            AssertTrue(aliceEntry != null && aliceEntry.Count == 3, $"Слово 'alice' встретилось 3 раза (факт: {aliceEntry?.Count})");

            Console.WriteLine("[OK] StringQuickSort, MsdRadixSort и частотный анализ работают с высокой точностью.");
        }

        private static void AssertEqual<T>(T[] expected, T[] actual, string name)
        {
            if (expected.Length != actual.Length)
            {
                throw new Exception($"[{name}] Несовпадение длины: ожидалось {expected.Length}, получено {actual.Length}");
            }
            for (int i = 0; i < expected.Length; i++)
            {
                if (!Equals(expected[i], actual[i]))
                {
                    throw new Exception($"[{name}] Ошибка на позиции [{i}]: ожидалось '{expected[i]}', получено '{actual[i]}'");
                }
            }
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"[ПРОВАЛ АССЕРТА]: {message}");
            }
        }
    }
}
