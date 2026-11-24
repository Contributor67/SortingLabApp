using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SortingLabApp.Models;

namespace SortingLabApp.Algorithms.Strings
{
    public static class TextGenerator
    {
        private static readonly string[] CommonWords = new[]
        {
            "the", "be", "to", "of", "and", "a", "in", "that", "have", "i",
            "it", "for", "not", "on", "with", "he", "as", "you", "do", "at",
            "this", "but", "his", "by", "from", "they", "we", "say", "her", "she",
            "or", "an", "will", "my", "one", "all", "would", "there", "their", "what",
            "so", "up", "out", "if", "about", "who", "get", "which", "go", "me",
            "algorithm", "sorting", "computer", "science", "quick", "radix", "array", "element",
            "merge", "binary", "tree", "memory", "function", "variable", "structure", "program",
            "performance", "complexity", "time", "space", "iteration", "recursion", "index",
            "analysis", "key", "comparison", "exchange", "pointer", "table", "data", "system"
        };

        private static readonly string[] Vocabulary = new[]
        {
            "algorithm", "efficiency", "structure", "computational", "asymptotic",
            "logarithmic", "exponential", "polynomial", "quadratic", "optimization",
            "quicksort", "radixsort", "heapsort", "mergesort", "insertionsort", "shellsort",
            "lexicographical", "frequency", "permutation", "distribution", "partition",
            "recursion", "iteration", "sequential", "processor", "architecture", "benchmark",
            "execution", "millisecond", "microsecond", "collection", "alphabetical", "string",
            "character", "buffer", "cache", "memory", "storage", "register", "hardware",
            "software", "language", "compiler", "runtime", "garbage", "collector", "thread",
            "concurrency", "parallelism", "asynchronous", "synchronous", "deterministic"
        };

        public static string GenerateText(int wordCount)
        {
            var rng = new Random();
            var words = new List<string>(wordCount);

            for (int i = 0; i < wordCount; i++)
            {
                // 70% выбор из частотных слов (как в реальных текстах закон Ципфа), 30% из словаря терминов
                if (rng.NextDouble() < 0.7)
                {
                    words.Add(CommonWords[rng.Next(CommonWords.Length)]);
                }
                else
                {
                    words.Add(Vocabulary[rng.Next(Vocabulary.Length)]);
                }
            }

            return string.Join(" ", words);
        }

        public static string[] Tokenize(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();

            // Извлекаем только слова, состоящие из букв, и переводим в нижний регистр
            var matches = Regex.Matches(text.ToLowerInvariant(), @"\b[a-zA-Zа-яА-ЯёЁ]+\b");
            var result = new string[matches.Count];
            for (int i = 0; i < matches.Count; i++)
            {
                result[i] = matches[i].Value;
            }
            return result;
        }

        public static List<WordFrequency> CountFrequenciesFromSorted(string[] sortedWords)
        {
            var list = new List<WordFrequency>();
            if (sortedWords == null || sortedWords.Length == 0) return list;

            int total = sortedWords.Length;
            int currentCount = 1;
            string currentWord = sortedWords[0];

            for (int i = 1; i < total; i++)
            {
                if (sortedWords[i] == currentWord)
                {
                    currentCount++;
                }
                else
                {
                    list.Add(new WordFrequency
                    {
                        Word = currentWord,
                        Count = currentCount,
                        FrequencyPercent = (double)currentCount / total * 100.0
                    });

                    currentWord = sortedWords[i];
                    currentCount = 1;
                }
            }

            // Добавляем последнее слово
            list.Add(new WordFrequency
            {
                Word = currentWord,
                Count = currentCount,
                FrequencyPercent = (double)currentCount / total * 100.0
            });

            // Сортируем список частот по убыванию количества
            list.Sort((a, b) => b.Count != a.Count ? b.Count.CompareTo(a.Count) : string.CompareOrdinal(a.Word, b.Word));

            // Проставляем ранги
            for (int i = 0; i < list.Count; i++)
            {
                list[i].Rank = i + 1;
            }

            return list;
        }
    }
}
